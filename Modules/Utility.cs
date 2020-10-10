using System;
using System.Linq;
using Sandbox.Game.World;

namespace PVEServerPlugin.Modules
{
    public class Utility
    {
        public static bool InConflict(long id, long challengingId)
        {
            var matching = false;
            foreach (var pair in Config.Instance.ConflictPairs)
            {
                if (pair.Id == id && pair.ChallengingId == challengingId)
                {
                    matching = true;
                    break;
                }

                if (pair.ChallengingId != id || pair.Id != challengingId) continue;
                matching = true;
                break;
            }
            return matching;
        }

        public static void IssueChallenge(long id, long challengingId)
        {
            if (InConflict(id, challengingId)) return;
        }

        public static void AcceptChallenge(long id, long challengingId)
        {
            if (InConflict(id, challengingId)) return;
            Config.Instance.ConflictPairs.Add(new ConflictPairs{ChallengingId = challengingId, Id = id });

        }

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