using System;
using System.Collections.Generic;
using System.Linq;
using PVEServerPlugin.Modules;
using Sandbox.Game.World;
using Torch;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Plugins;

namespace PVEServerPlugin.Utility
{
    public static class Utilities
    {

        public static long GetPlayerId(string name)
        {
            long id = 0;
            foreach (var player in MySession.Static.Players.GetOnlinePlayers())
            {
                if (string.IsNullOrEmpty(player.DisplayName) || !player.DisplayName.Equals(name,StringComparison.OrdinalIgnoreCase)) continue;
                id = player.Identity.IdentityId;
                break;
            }
            return id;
        }

        public static long GetFactionId(string nameOrTag)
        {
            long id = 0;
            foreach (var (factionId, faction) in MySession.Static.Factions)
            {
                if (faction.IsEveryoneNpc() || (!faction.Name.Equals(nameOrTag,StringComparison.OrdinalIgnoreCase) && !faction.Tag.Equals(nameOrTag,StringComparison.OrdinalIgnoreCase)))continue;

                id = factionId;
                break;

            }

            return id;
        }




    }
}