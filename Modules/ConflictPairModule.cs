using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Torch;
using Newtonsoft.Json;
using Sandbox.Game.World;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;

namespace PVEServerPlugin.Modules
{
    public class ConflictPairModule
    {
        public static HashSet<ConflictPairData> ConflictPairs = new HashSet<ConflictPairData>();

        public class ConflictPairData
        {
            [JsonProperty(Order = 1)]
            public long ChallengerId { get; set; }
            [JsonProperty(Order = 2)]
            public long ChallengedId { get; set; }
            [JsonProperty(Order = 3)]
            public ulong ChangeRequestId { get; set; }
            [JsonProperty(Order = 4)]
            public bool ConflictPending { get; set; }
            [JsonProperty(Order = 5)]
            public bool ConflictSubmitted { get; set; }
        }

        private static void SaveConflictData()
        {
            File.WriteAllText(Core.Instance.conflictDataPath, JsonConvert.SerializeObject(ConflictPairs, Formatting.Indented));
        }

        public static bool InConflict(long id, long challengingId, out ConflictPairData foundPairModule)
        {
            var inConflict = false;
            foundPairModule = null;
            foreach (var pair in ConflictPairs)
            {
                if (pair.ChallengerId + pair.ChallengedId != id + challengingId) continue;
                inConflict = !pair.ConflictPending;
                foundPairModule = pair;
            }

            return inConflict;
        }

        public static void IssueChallenge(long id, long challengingId, ulong changeRequestId = 0)
        {
            if (id == 0 || challengingId == 0 || changeRequestId == 0) return;
            if (InConflict(id, challengingId, out var foundPair))
            {
                if (foundPair.ChangeRequestId == changeRequestId) return;
                foundPair.ConflictPending = false;
                foundPair.ChangeRequestId = changeRequestId;
                return;
            }
            
            ConflictPairs.Add(new ConflictPairData{ChallengerId = challengingId, ChallengedId = id,ConflictPending = true,ChangeRequestId = changeRequestId});
            SaveConflictData();
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
                ModCommunication.SendMessageTo(new NotificationMessage($"{challengerName} has issued a war challenge",10000,MyFontEnum.White),steamId );
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
            if (foundPair.ConflictSubmitted || foundPair.ConflictPending)
            {

                ConflictPairs.Remove(foundPair);
                SaveConflictData();
                return;
            }

            foundPair.ConflictSubmitted = true;
            foundPair.ChangeRequestId = changeRequestId;
            SaveConflictData();
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

            if (foundPair.ConflictSubmitted)
            {
                return;
            }

            foundPair.ConflictPending = false;
            foundPair.ChangeRequestId = changeRequestId;
            SaveConflictData();
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
            foreach (var pair in ConflictPairs.Where(x=>x.ConflictPending))
            {
                if (pair.ChangeRequestId == changeRequestId) continue;
                if (pair.ChallengedId != id && pair.ChallengerId != id && (validFaction && !InConflict(id, faction.FactionId, out _))) continue;
                pair.ConflictPending = false;
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

            ConflictPairs.Remove(foundPair);
            SaveConflictData();
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
            HashSet<ConflictPairData> toRemove = new HashSet<ConflictPairData>();
            
            foreach (var pair in ConflictPairs.Where(x=>x.ConflictSubmitted))
            {
                if (pair.ChangeRequestId == changeRequestId) continue;

                if (validFaction && (pair.ChallengedId == faction.FactionId || pair.ChallengerId == faction.FactionId))
                {
                    toRemove.Add(pair);
                    foundPendingConflict = true;
                    continue;
                }

                if (pair.ChallengerId != id && pair.ChallengedId != id) continue;
                toRemove.Add(pair);
                foundPendingConflict = true;

            }
            RemovePairs(toRemove);
            Core.Instance.RecheckReputations();

            return foundPendingConflict;
        }



        private static void RemovePairs(HashSet<ConflictPairData> toRemove)
        {
            foreach (var pair in toRemove)
            {
                ConflictPairs.Remove(pair);
            }
            SaveConflictData();
        }



    }
}