using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PVEServerPlugin.Modules;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using NLog;
using PVEServerPlugin.ProcessHandlers;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using Torch.Utils;
using Torch.Views;
using VRage.Game.ModAPI;

namespace PVEServerPlugin
{
    public class Core : TorchPluginBase, IWpfPlugin
    {
        public readonly Logger Log = LogManager.GetLogger("PVEPlugin");
        private Thread _processThread;
        private HashSet<Thread> _processThreads;
        private HashSet<Base> _handlers;
        private bool _running;
        private TorchSessionManager _sessionManager;
        public static Core Instance { get; private set; }
        public string conflictDataPath = "";
        private UserControl _control;
        private UserControl Control => _control ?? (_control = new PropertyGrid{ DataContext = Config.Instance});
        public UserControl GetControl()
        {
            return Control;
        }
        private void EnableControl(bool enable = true)
        {
            _control?.Dispatcher?.Invoke(() =>
            {
                Control.IsEnabled = enable;
                Control.DataContext = Config.Instance;
            });

        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;
            Config.Instance.Load();
            _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();

            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += SessionChanged;
        }

        public override void Update()
        {
            base.Update();

            if (MyAPIGateway.Session == null)
                return;
            Utility.EntityCache.Update();
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newstate)
        {
            _running = newstate == TorchSessionState.Loaded;
            switch (newstate)
            {
                case TorchSessionState.Loading:
                   var storageDir = Path.Combine(Torch.CurrentSession.KeenSession.CurrentPath, "Storage");
                   conflictDataPath = Path.Combine(storageDir, "Conflict.json");
                    if (!File.Exists(conflictDataPath))
                    {
                        if (!Directory.Exists(storageDir)) Directory.CreateDirectory(storageDir);
                        File.Create(conflictDataPath);
                        Log.Warn($"Creating conflict data at {conflictDataPath}");
                    }

                    break;
                case TorchSessionState.Loaded:
                    Load();
                    break;
                case TorchSessionState.Unloading:
                    break;
                case TorchSessionState.Unloaded:
                    break;
            }
        }

        private void Load()
        {
            DamageHandler.Init();
           MySession.Static.Factions.FactionCreated += FactionsOnFactionCreated;
           var data = File.ReadAllText(conflictDataPath);
           if (!string.IsNullOrEmpty(data))
           {
               ConflictPairModule.ConflictPairs =
                   JsonConvert.DeserializeObject<HashSet<ConflictPairModule.ConflictPairData>>(
                       File.ReadAllText(conflictDataPath));
           }

           RecheckReputations();
           _handlers = new HashSet<Base>
           {
               new PvPZoneMessage(),
           };
           _processThreads = new HashSet<Thread>();
           _processThread = new Thread(PluginProcessing);
           _processThread.Start();

        }

        private void PluginProcessing()
        {
            try
            {
                foreach (var handler in _handlers)
                {
                    Base currentHandler = handler;
                    var thread = new Thread(() =>
                    {
                        while (_running)
                        {
                            if (currentHandler.CanProcess())
                            {
                                try
                                {
                                    currentHandler.Handle();
                                }
                                catch (Exception ex)
                                {
                                    Log.Warn("Handler Problems: {0} - {1}", currentHandler.GetUpdateResolution(),
                                        ex);
                                }

                                currentHandler.LastUpdate = DateTime.Now;
                            }

                            Thread.Sleep(100);
                        }

                    });
                    _processThreads.Add(thread);
                    thread.Start();
                }

                foreach (Thread thread in _processThreads)
                    thread.Join();

            }
            catch (ThreadAbortException ex)
            {
                Log.Trace(ex);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }


        private void FactionsOnFactionCreated(long obj)
        {
            var faction = MySession.Static.Factions[obj];
            if (faction.IsEveryoneNpc()) return;
            var playerFactions = new HashSet<MyFaction>(MySession.Static.Factions.Select(x=>x.Value).Where(x=>!x.IsEveryoneNpc()));

            foreach (var fac in playerFactions)
            {
                if (fac.FactionId == obj) continue;
                MySession.Static.Factions.SetReputationBetweenFactions(obj, fac.FactionId, MySession.Static.Factions.ClampReputation(0));
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    FactionStateChangeRequest(MyFactionStateChange.AcceptPeace, obj, fac.FactionId, 0);

                });
            }
        }



        public void RecheckReputations()
        {

            var playerFactions = new HashSet<MyFaction>(MySession.Static.Factions.Select(x=>x.Value).Where(x=>!x.IsEveryoneNpc()));

            if (playerFactions.Count == 0) return;
            
            var players = new HashSet<MyIdentity>(MySession.Static.Players.GetAllIdentities().Where(x=>!MySession.Static.Players.IdentityIsNpc(x.IdentityId)));


            foreach (var player in players)
            {
                var playerFaction = MySession.Static.Factions.GetPlayerFaction(player.IdentityId);
                foreach (var faction in playerFactions)
                {
                    if (Config.Instance.EnableConflict && playerFaction != null &&
                        ConflictPairModule.InConflict(playerFaction.FactionId, faction.FactionId, out var foundPair) &&
                        !foundPair.ConflictPending)
                    {
                        MySession.Static.Factions.SetReputationBetweenPlayerAndFaction(player.IdentityId, faction.FactionId,
                            -500);
                        continue;
                    }
                    MySession.Static.Factions.SetReputationBetweenPlayerAndFaction(player.IdentityId, faction.FactionId,
                        0);
                }
            }


        }

        [ReflectedStaticMethod(Type = typeof(MyFactionCollection), Name = "SendFactionChange", OverrideTypes = new []{typeof(MyFactionStateChange), typeof(long), typeof(long), typeof(long)})]
        private static Action <MyFactionStateChange,long,long,long> FactionStateChangeRequest;

        public static void RequestFactionChange(MyFactionStateChange action, long fromFactionId, long toFactionId,
            long playerId)
        {
            FactionStateChangeRequest(action, fromFactionId, toFactionId, playerId);
        }
    }
}
