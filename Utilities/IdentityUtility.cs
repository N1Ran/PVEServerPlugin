using System;
using System.Linq;
using Sandbox.Game.World;

namespace PVEServerPlugin.Utilities
{
    public static class IdentityUtility
    {

        public static long GetPlayerId(string name)
        {
            if (long.TryParse(name, out var id)) return id;

            ulong.TryParse(name, out var steamId);

            foreach (var player in MySession.Static.Players.GetOnlinePlayers())
            {
                if (string.IsNullOrEmpty(player.DisplayName)) continue;

                if (steamId > 0 && player.Id.SteamId == steamId)
                {
                    id = player.Identity.IdentityId;
                    break;
                }

                if (!player.DisplayName.Equals(name,StringComparison.OrdinalIgnoreCase)) continue;
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


        public static string GetIdentityName(long id)
        {
            string name = "Unknown";

            //var factionName = MySession.Static.Factions.getpla

            return name;
        }



    }
}