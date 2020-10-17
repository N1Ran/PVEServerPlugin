using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PVEServerPlugin.Modules;
using System.Windows.Controls;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
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
        private TorchSessionManager _sessionManager;
        public static Core Instance { get; private set; }

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

        private void SessionChanged(ITorchSession session, TorchSessionState newstate)
        {
            switch (newstate)
            {
                case TorchSessionState.Loading:
                    
                    break;
                case TorchSessionState.Loaded:
                    Load();
                    RecheckReputations();
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
                        Utility.InConflict(playerFaction.FactionId, faction.FactionId, out var foundPair) &&
                        !foundPair.Pending)
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
