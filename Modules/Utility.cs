using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.World;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;

namespace PVEServerPlugin.Modules
{
    public class Utility
    {
        public static bool InConflict(long id, long challengingId, out ConflictPairs foundPair)
        {
            var matching = false;
            foundPair = null;
            foreach (var pair in Config.Instance.ConflictPairs)
            {
                if (pair.Id == id && pair.ChallengingId == challengingId)
                {
                    matching = !pair.Pending;
                    foundPair = pair;
                    break;
                }

                if (pair.ChallengingId == id && pair.Id == challengingId)
                {
                    matching = !pair.Pending;
                    foundPair = pair;
                    break;
                }
            }

            return matching;
        }

        public static void IssueChallenge(long id, long challengingId)
        {
            if (id == 0 || challengingId == 0) return;
            if (InConflict(id, challengingId, out var foundPair))
            {
                foundPair.Pending = false;
                return;
            }
            
            Config.Instance.ConflictPairs.Add(new ConflictPairs{ChallengingId = challengingId, Id = id,Pending = true});
            HashSet<ulong> steamIds = new HashSet<ulong>();
            var faction = MySession.Static.Factions.TryGetFactionById(challengingId);
            string challengerName = "";
            if (faction != null)
            {
                foreach (var (memberId,member) in faction.Members)
                {
                    steamIds.Add(MySession.Static.Players.TryGetSteamId(memberId));
                }
                challengerName = MySession.Static.Factions.TryGetFactionById(id).Tag;

            }
            else
            {
                steamIds.Add(MySession.Static.Players.TryGetSteamId(challengingId));
                challengerName = MySession.Static.Players.TryGetIdentity(id).DisplayName ?? "";
            }
            if (steamIds.Count == 0 || string.IsNullOrEmpty(challengerName)) return;
            foreach (var steamId in steamIds)
            {
                ModCommunication.SendMessageTo(new NotificationMessage($"{challengerName} is calling you out",10000,MyFontEnum.White),steamId );
            }

        }

        public static void AcceptChallenge(long id, long challengingId)
        {
            if (InConflict(id, challengingId, out var foundPair))
            {
                foundPair.Pending = false;
                return;
            }
            Config.Instance.ConflictPairs.Add(new ConflictPairs{ChallengingId = challengingId, Id = id,Pending = false});
            HashSet<ulong> steamIds = new HashSet<ulong>();
            var faction = MySession.Static.Factions.TryGetFactionById(challengingId);
            string challengerName = "";

            if (faction != null)
            {
                foreach (var (memberId,member) in faction.Members)
                {
                    steamIds.Add(MySession.Static.Players.TryGetSteamId(memberId));
                }

                var challengerFaction = MySession.Static.Factions.TryGetFactionById(id);
                if (challengerFaction != null)
                {
                    challengerName = MySession.Static.Factions.TryGetFactionById(id).Tag;
                    foreach (var memberId in faction.Members.Keys)
                    {
                        steamIds.Add(MySession.Static.Players.TryGetSteamId(memberId));
                    }
                }

            }
            else
            {
                steamIds.Add(MySession.Static.Players.TryGetSteamId(challengingId));
                challengerName = MySession.Static.Players.TryGetIdentity(id).DisplayName ?? "";
            }
            if (steamIds.Count == 0 || string.IsNullOrEmpty(challengerName)) return;
            foreach (var steamId in steamIds)
            {
                ModCommunication.SendMessageTo(new NotificationMessage($"Conflict with {challengerName}",10000,MyFontEnum.White),steamId );
            }


        }

        public static void AcceptChallenge(long id)
        {

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