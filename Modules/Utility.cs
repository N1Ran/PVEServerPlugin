using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.World;
using Torch;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Plugins;

namespace PVEServerPlugin.Modules
{
    public static class Utility
    {
        public static bool InConflict(long id, long challengingId, out ConflictPairs foundPair)
        {
            var matching = false;
            foundPair = null;
            foreach (var pair in Config.Instance.ConflictPairs)
            {
                if ((pair.Id + pair.ChallengingId) != (id + challengingId)) continue;
                matching = !pair.Pending;
                foundPair = pair;
            }

            return matching;
        }

        public static void IssueChallenge(long id, long challengingId, ulong changeRequestId = 0)
        {
            if (id == 0 || challengingId == 0 || changeRequestId == 0) return;
            if (InConflict(id, challengingId, out var foundPair))
            {
                if (foundPair.ChangeRequestId == changeRequestId) return;
                foundPair.Pending = false;
                foundPair.ChangeRequestId = changeRequestId;
                return;
            }
            
            Config.Instance.ConflictPairs.Add(new ConflictPairs{ChallengingId = challengingId, Id = id,Pending = true,ChangeRequestId = changeRequestId});
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

        public static void RequestSubmit(long id, long challengingId, ulong changeRequestId = 0)
        {
            if (id == 0 || challengingId == 0) return;
            if (changeRequestId == 0 ) return;
            if (!InConflict(id, challengingId, out var foundPair) || foundPair.ChangeRequestId == changeRequestId)
            {
                return;
            }
            if (foundPair.Submit || foundPair.Pending)
            {

                Config.Instance.ConflictPairs.Remove(foundPair);
                return;
            }

            foundPair.Submit = true;
            foundPair.ChangeRequestId = changeRequestId;
            HashSet<ulong> steamIds = new HashSet<ulong>();
            var faction = MySession.Static.Factions.TryGetFactionById(challengingId);
            string challengerName;
            if (faction != null)
            {
                foreach (var (memberId,member) in faction.Members)
                {
                    if (!member.IsFounder && !member.IsLeader) continue;
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
                ModCommunication.SendMessageTo(new NotificationMessage($"{challengerName} has issued a submission",10000,MyFontEnum.White),steamId );
            }

        }

        public static void AcceptChallenge(long id, long challengingId, ulong changeRequestId = 0)
        {
            if (changeRequestId == 0 ) return;
            if (!InConflict(id, challengingId, out var foundPair) || foundPair.ChangeRequestId == changeRequestId)
            {
                return;
            }

            if (foundPair.Submit)
            {
                return;
            }

            foundPair.Pending = false;
            foundPair.ChangeRequestId = changeRequestId;
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
                    challengerName = $"{MySession.Static.Factions.TryGetFactionById(id).Tag} and {faction.Tag}";
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
                ModCommunication.SendMessageTo(new NotificationMessage($"Conflict with {challengerName} Accepted",10000,MyFontEnum.White),steamId );
            }


        }

        public static bool AcceptChallenge(long id, ulong changeRequestId = 0)
        {
            if (changeRequestId == 0) return false;

            var foundPendingConflict = false;
            var faction = MySession.Static.Factions.GetPlayerFaction(id);
            var validFaction = faction != null && (faction.IsLeader(id) || faction.IsFounder(id));
            if (validFaction)
            {
                var player = MySession.Static.Players.TryGetPlayerBySteamId(changeRequestId);
                if (player != null)
                    validFaction = MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId) != faction;
            }
            foreach (var pair in Config.Instance.ConflictPairs.Where(x=>x.Pending))
            {
                if (pair.ChangeRequestId == changeRequestId) continue;
                if (pair.ChallengingId != id && pair.Id != id && (validFaction && !InConflict(id, faction.FactionId, out _))) continue;
                pair.Pending = false;
                foundPendingConflict = true;
                pair.ChangeRequestId = 0;
            }

            return foundPendingConflict;
        }

        public static void AcceptSubmission(long id, long challengingId, ulong changeRequestId = 0)
        {
            if (changeRequestId == 0 ) return;
            if (!InConflict(id, challengingId, out var foundPair) || foundPair.ChangeRequestId == changeRequestId)
            {
                return;
            }

            Config.Instance.ConflictPairs.Remove(foundPair);

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
                    challengerName = $"{MySession.Static.Factions.TryGetFactionById(id).Tag} and {faction.Tag}";
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
            if (Config.Instance.EnableConflict)Core.Instance.RecheckReputations();
            if (steamIds.Count == 0 || string.IsNullOrEmpty(challengerName)) return;
            foreach (var steamId in steamIds)
            {
                ModCommunication.SendMessageTo(new NotificationMessage($"Resolution with {challengerName} Approved",10000,MyFontEnum.White),steamId );
            }



        }

        public static bool AcceptSubmission(long id, ulong changeRequestId = 0)
        {
            if (changeRequestId == 0) return false;
            var foundPendingConflict = false;
            var faction = MySession.Static.Factions.GetPlayerFaction(id);
            
            var validFaction = faction != null && (faction.IsLeader(id) || faction.IsFounder(id));

            if (validFaction)
            {
                var player = MySession.Static.Players.TryGetPlayerBySteamId(changeRequestId);
                if (player != null)
                    validFaction = MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId) != faction;
            }
            HashSet<ConflictPairs> toRemove = new HashSet<ConflictPairs>();
            
            foreach (var pair in Config.Instance.ConflictPairs.Where(x=>x.Submit))
            {
                if (pair.ChangeRequestId == changeRequestId) continue;

                if (validFaction && (pair.Id == faction.FactionId || pair.ChallengingId == faction.FactionId))
                {
                    toRemove.Add(pair);
                    foundPendingConflict = true;
                    continue;
                }

                if (pair.Id != id && pair.ChallengingId != id) continue;
                toRemove.Add(pair);
                foundPendingConflict = true;

            }
            RemovePairs(toRemove);
            if (Config.Instance.EnableConflict)Core.Instance.RecheckReputations();

            return foundPendingConflict;
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


        public static void RemovePairs(HashSet<ConflictPairs> toRemove)
        {
            foreach (var pair in toRemove)
            {
                Config.Instance.ConflictPairs.Remove(pair);
            }

        }


    }
}