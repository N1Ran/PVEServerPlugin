using System;
using System.Linq;
using System.Reflection;
using NLog;
using PVEServerPlugin.Modules;
using SpaceEngineers.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch;
using Torch.Managers;
using Torch.Utils;
using VRage.Game.ModAPI;
using VRage.Network;

namespace PVEServerPlugin
{
    [PatchShim]
    public static class ReputationPatch
    {

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyFactionCollection).GetMethod("FactionStateChangeRequest",
                    BindingFlags.NonPublic | BindingFlags.Static)).Prefixes
                .Add(typeof(ReputationPatch).GetMethod(nameof(ChangeRequest), BindingFlags.NonPublic|BindingFlags.Static));

            ctx.GetPattern(typeof(MyFactionCollection).GetMethod(
                    nameof(MyFactionCollection.DamageFactionPlayerReputation),
                    BindingFlags.Public | BindingFlags.Instance))
                .Prefixes.Add(typeof(ReputationPatch).GetMethod(nameof(DamageFaction), BindingFlags.NonPublic|BindingFlags.Static));
        }

        private static bool ChangeRequest(MyFactionStateChange action, long fromFactionId, long toFactionId, long playerId)
        {
            if (action != MyFactionStateChange.DeclareWar) return true;
            if (Config.Instance.EnableConflict && Utility.InConflict(fromFactionId, toFactionId, out var foundPair) && !foundPair.Pending) return true;
            Utility.IssueChallenge(fromFactionId,toFactionId,MyEventContext.Current.Sender.Value);
            Core.RequestFactionChange(MyFactionStateChange.AcceptPeace, fromFactionId, toFactionId, playerId);
            return false;
        }

        private static bool DamageFaction(MyFactionCollection __instance, long playerIdentityId,
            long attackedIdentityId,
            MyReputationDamageType repDamageType)
        {
            return (Config.Instance.EnableConflict && Utility.InConflict(playerIdentityId,attackedIdentityId, out var foundPair) && !foundPair.Pending )||MySession.Static.Factions.GetNpcFactions().Any(x =>
                x.Members.ContainsKey(playerIdentityId) || x.Members.ContainsKey(attackedIdentityId));
        }

    }
}