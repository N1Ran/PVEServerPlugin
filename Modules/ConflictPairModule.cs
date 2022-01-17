using System.Collections.Generic;
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
            public string ChallengerName { get; set; }
            [JsonProperty(Order = 2)]
            public long ChallengerId { get; set; }
            [JsonProperty(Order = 3)]
            public long SubjectName { get; set; }
            [JsonProperty(Order = 4)]
            public long SubjectId { get; set; }
            [JsonProperty(Order = 5)]
            public ulong ChangeRequestId { get; set; }
            [JsonProperty(Order = 6)]
            public ConflictType CurrentConflictType { get; set; }
            [JsonProperty(Order = 7)]
            public ConflictState CurrentConflictState { get; set; }
        }

        private static void SaveConflictData()
        {
            File.WriteAllText(Core.Instance.ConflictDataPath, JsonConvert.SerializeObject(ConflictPairs, Formatting.Indented));
        }

        public static bool InConflict(long challengerId, long subjectId, ConflictType type, out ConflictPairData foundPairModule)
        {
            foundPairModule = null;
            if (!Config.Instance.EnableConflict) return false;

            if (Core.NexusDetected)
            {
                return MySession.Static.Factions.AreFactionsEnemies(challengerId, subjectId) ||
                       MySession.Static.Factions.IsFactionWithPlayerEnemy(challengerId, subjectId) ||
                       MySession.Static.Factions.IsFactionWithPlayerEnemy(subjectId, challengerId);
            }
            var inConflict = false;
            foreach (var pair in ConflictPairs)
            {
                if (pair.ChallengerId + pair.SubjectId != challengerId + subjectId || pair.CurrentConflictType != type) continue;
                inConflict = pair.CurrentConflictState == ConflictState.Active;
                foundPairModule = pair;
            }

            return inConflict;
        }

        public static bool InConflict(long challengerId, long subjectId,  out ConflictPairData foundPairModule)
        {
            foundPairModule = null;
            if (!Config.Instance.EnableConflict) return false;

            if (Core.NexusDetected)
            {
                return MySession.Static.Factions.AreFactionsEnemies(challengerId, subjectId) ||
                       MySession.Static.Factions.IsFactionWithPlayerEnemy(challengerId, subjectId) ||
                       MySession.Static.Factions.IsFactionWithPlayerEnemy(subjectId, challengerId);
            }
            var inConflict = false;
            foreach (var pair in ConflictPairs)
            {
                if (pair.ChallengerId + pair.SubjectId != challengerId + subjectId) continue;
                inConflict = pair.CurrentConflictState == ConflictState.Active;
                foundPairModule = pair;
            }

            return inConflict;
        }

        public static void IssueChallenge(long challengerId, long subjectId, ConflictType type, ulong changeRequestId = 0)
        {
            if (challengerId == 0 || subjectId == 0 || changeRequestId == 0) return;
            if (InConflict(challengerId, subjectId,type, out var foundPair))
            {
                if (foundPair.ChangeRequestId == changeRequestId) return;
                foundPair.CurrentConflictState = ConflictState.Active;
                foundPair.ChangeRequestId = changeRequestId;
                return;
            }
            
            ConflictPairs.Add(new ConflictPairData{CurrentConflictType = type, ChallengerId = challengerId, SubjectId = subjectId,CurrentConflictState = ConflictState.PendingConflict, ChangeRequestId = changeRequestId});
            SaveConflictData();
            HashSet<ulong> steamIds = new HashSet<ulong>();
            var faction = MySession.Static.Factions.TryGetFactionById(subjectId);
            string challengerName = "";
            if (faction != null)
            {
                foreach (var (memberId,member) in faction.Members)
                {
                    steamIds.Add(MySession.Static.Players.TryGetSteamId(memberId));
                }
                challengerName = MySession.Static.Factions.TryGetFactionById(challengerId).Tag;

            }
            else
            {
                steamIds.Add(MySession.Static.Players.TryGetSteamId(subjectId));
                challengerName = MySession.Static.Players.TryGetIdentity(challengerId).DisplayName ?? "";
            }
            if (steamIds.Count == 0 || string.IsNullOrEmpty(challengerName)) return;
            foreach (var steamId in steamIds)
            {
                ModCommunication.SendMessageTo(new NotificationMessage($"{challengerName} has issued a war challenge",10000,MyFontEnum.White),steamId );
            }

        }

        public static void RequestSubmit(long challengerId, long subjectId, ConflictType type, ulong changeRequestId = 0)
        {
            if (challengerId == 0 || subjectId == 0) return;
            if (changeRequestId == 0) return;
            if (!InConflict(challengerId, subjectId, type, out var foundPair) || foundPair.ChangeRequestId == changeRequestId)
            {
                return;
            }
            if (foundPair.CurrentConflictState != ConflictState.Active)
            {

                ConflictPairs.Remove(foundPair);
                SaveConflictData();
                return;
            }

            foundPair.CurrentConflictState = ConflictState.PendingSubmission;
            foundPair.ChangeRequestId = changeRequestId;
            SaveConflictData();
            HashSet<ulong> steamIds = new HashSet<ulong>();
            var faction = MySession.Static.Factions.TryGetFactionById(subjectId);
            string challengerName;
            if (faction != null)
            {
                foreach (var (memberId, member) in faction.Members)
                {
                    if (!member.IsFounder && !member.IsLeader) continue;
                    steamIds.Add(MySession.Static.Players.TryGetSteamId(memberId));
                }
                challengerName = MySession.Static.Factions.TryGetFactionById(challengerId).Tag;

            }
            else
            {
                steamIds.Add(MySession.Static.Players.TryGetSteamId(subjectId));
                challengerName = MySession.Static.Players.TryGetIdentity(challengerId).DisplayName ?? "";
            }
            if (steamIds.Count == 0 || string.IsNullOrEmpty(challengerName)) return;
            foreach (var steamId in steamIds)
            {
                ModCommunication.SendMessageTo(new NotificationMessage($"{challengerName} has issued a submission", 10000, MyFontEnum.White), steamId);
            }

        }

        public static int RequestSubmit(long challengerId, ulong changeRequestId = 0)
        {
            if (changeRequestId == 0) return 0;

            var setSubmission = 0;
            var faction = MySession.Static.Factions.GetPlayerFaction(challengerId);
            var validFaction = faction != null && (faction.IsLeader(challengerId) || faction.IsFounder(challengerId));
            if (validFaction)
            {
                var player = MySession.Static.Players.TryGetPlayerBySteamId(changeRequestId);
                if (player != null)
                    validFaction = MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId) != faction;
            }

            var toRemove = new List<ConflictPairData>();
            var changeRequestIdentity = MySession.Static.Players.TryGetPlayerBySteamId(changeRequestId)?.Identity;
            foreach (var pair in ConflictPairs)
            {
                if (pair.ChangeRequestId == changeRequestId) continue;
                if (pair.SubjectId != challengerId && pair.ChallengerId != challengerId && (validFaction && !InConflict(challengerId, faction.FactionId, ConflictType.Faction, out _))) continue;
                if (pair.CurrentConflictState == ConflictState.PendingSubmission && changeRequestIdentity != null)
                {
                    if (validFaction && faction.Members.ContainsKey(changeRequestIdentity.IdentityId)) continue;
                    toRemove.Add(pair);
                    continue;
                }
                pair.CurrentConflictState = ConflictState.PendingSubmission;
                setSubmission++;
                pair.ChangeRequestId = 0;
            }

            foreach (var pair in toRemove)
            {
                ConflictPairs.Remove(pair);
            }
            return setSubmission;
        }

        public static void AcceptChallenge(long challengerId, long subjectId, ConflictType type, ulong changeRequestId = 0)
        {
            if (changeRequestId == 0 ) return;
            if (!InConflict(challengerId, subjectId, type, out var foundPair) || foundPair.ChangeRequestId == changeRequestId)
            {
                return;
            }

            if (foundPair.CurrentConflictState != ConflictState.PendingConflict)
            {
                return;
            }

            foundPair.CurrentConflictState = ConflictState.Active;
            foundPair.ChangeRequestId = changeRequestId;
            SaveConflictData();
            HashSet<ulong> steamIds = new HashSet<ulong>();
            var faction = MySession.Static.Factions.TryGetFactionById(subjectId);
            string challengerName = "";

            if (faction != null)
            {
                foreach (var (memberId,member) in faction.Members)
                {
                    steamIds.Add(MySession.Static.Players.TryGetSteamId(memberId));
                }

                var challengerFaction = MySession.Static.Factions.TryGetFactionById(challengerId);
                if (challengerFaction != null)
                {
                    challengerName = $"{MySession.Static.Factions.TryGetFactionById(challengerId).Tag} and {faction.Tag}";
                    foreach (var memberId in faction.Members.Keys)
                    {
                        steamIds.Add(MySession.Static.Players.TryGetSteamId(memberId));
                    }
                }

            }
            else
            {
                steamIds.Add(MySession.Static.Players.TryGetSteamId(subjectId));
                challengerName = MySession.Static.Players.TryGetIdentity(challengerId).DisplayName ?? "";
            }
            if (steamIds.Count == 0 || string.IsNullOrEmpty(challengerName)) return;
            foreach (var steamId in steamIds)
            {
                ModCommunication.SendMessageTo(new NotificationMessage($"Conflict with {challengerName} Accepted",10000,MyFontEnum.White),steamId );
            }


        }

        public static int AcceptChallenge(long challengerId, ulong changeRequestId = 0)
        {
            if (changeRequestId == 0) return 0;

            var setActive = 0;
            var faction = MySession.Static.Factions.GetPlayerFaction(challengerId);
            var validFaction = faction != null && (faction.IsLeader(challengerId) || faction.IsFounder(challengerId));
            if (validFaction)
            {
                var player = MySession.Static.Players.TryGetPlayerBySteamId(changeRequestId);
                if (player != null)
                    validFaction = MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId) != faction;
            }
            foreach (var pair in ConflictPairs.Where(x=>x.CurrentConflictState == ConflictState.PendingConflict))
            {
                if (pair.ChangeRequestId == changeRequestId) continue;
                if (pair.SubjectId != challengerId && pair.ChallengerId != challengerId && (validFaction && !InConflict(challengerId, faction.FactionId, ConflictType.Faction, out _))) continue;
                pair.CurrentConflictState = ConflictState.Active;
                setActive ++;
                pair.ChangeRequestId = 0;
            }

            return setActive;
        }

        public static void AcceptSubmission(long challengerId, long subjectId, ConflictType type, ulong changeRequestId = 0)
        {
            if (changeRequestId == 0 ) return;
            if (!InConflict(challengerId, subjectId, type, out var foundPair) || foundPair.ChangeRequestId == changeRequestId)
            {
                return;
            }

            ConflictPairs.Remove(foundPair);
            SaveConflictData();
            HashSet<ulong> steamIds = new HashSet<ulong>();
            string challengerName = "";

            if (type == ConflictType.Faction)
            {
                var faction = MySession.Static.Factions.TryGetFactionById(subjectId);

                if (faction != null)
                {
                    foreach (var (memberId, member) in faction.Members)
                    {
                        steamIds.Add(MySession.Static.Players.TryGetSteamId(memberId));
                    }

                    var challengerFaction = MySession.Static.Factions.TryGetFactionById(challengerId);
                    if (challengerFaction != null)
                    {
                        challengerName = $"{MySession.Static.Factions.TryGetFactionById(challengerId).Tag} and {faction.Tag}";
                        foreach (var memberId in faction.Members.Keys)
                        {
                            steamIds.Add(MySession.Static.Players.TryGetSteamId(memberId));
                        }
                    }
                }

            }
            else
            {
                steamIds.Add(MySession.Static.Players.TryGetSteamId(subjectId));
                challengerName = MySession.Static.Players.TryGetIdentity(challengerId).DisplayName ?? "";
            }
            if (Config.Instance.EnableConflict)Core.Instance.RecheckReputations();
            if (steamIds.Count == 0 || string.IsNullOrEmpty(challengerName)) return;
            foreach (var steamId in steamIds)
            {
                ModCommunication.SendMessageTo(new NotificationMessage($"Resolution with {challengerName} Approved",10000,MyFontEnum.White),steamId );
            }



        }

        public static int AcceptSubmission(long challengerId, ulong changeRequestId = 0)
        {
            if (changeRequestId == 0) return 0;
            var submissionAccepted = 0;
            var faction = MySession.Static.Factions.GetPlayerFaction(challengerId);
            
            var validFaction = faction != null && (faction.IsLeader(challengerId) || faction.IsFounder(challengerId));

            if (validFaction)
            {
                var player = MySession.Static.Players.TryGetPlayerBySteamId(changeRequestId);
                if (player != null)
                    validFaction = MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId) != faction;
            }
            HashSet<ConflictPairData> toRemove = new HashSet<ConflictPairData>();
            
            foreach (var pair in ConflictPairs.Where(x=>x.CurrentConflictState == ConflictState.PendingSubmission))
            {
                if (pair.ChangeRequestId == changeRequestId) continue;

                if (validFaction && pair.CurrentConflictType == ConflictType.Faction && (pair.SubjectId == faction.FactionId || pair.ChallengerId == faction.FactionId))
                {
                    toRemove.Add(pair);
                    submissionAccepted ++;
                    continue;
                }

                if (pair.ChallengerId != challengerId && pair.SubjectId != challengerId) continue;
                toRemove.Add(pair);
                submissionAccepted ++;

            }
            RemovePairs(toRemove);
            Core.Instance.RecheckReputations();

            return submissionAccepted;
        }

        public List<string> GetPlayerPendingConflicts(long id)
        {

            var conflicts = new List<string>();
            var pendingConflicts = new List<ConflictPairData>(ConflictPairs.Where(x => x.CurrentConflictState == ConflictState.PendingConflict));
            if (pendingConflicts.Count > 0 && id > 0)
            {
                long factionId = 0;
                var faction = MySession.Static.Factions.GetPlayerFaction(id);
                if (faction.FounderId == id || faction.IsLeader(id))
                {
                    factionId = faction.FactionId;
                }

                foreach (var conflict in pendingConflicts)
                {
                    if (conflict.SubjectId != id && conflict.ChallengerId != id &&  conflict.SubjectId != factionId && conflict.ChallengerId != factionId) continue;
                }
            }

            return conflicts;
        }


        public List<string> GetPlayerConflicts(long id)
        {
            var conflicts = new List<string>();
            var currentConflicts = new List<ConflictPairData>(ConflictPairs.Where(x => x.CurrentConflictState == ConflictState.Active));
            if (currentConflicts.Count > 0 && id > 0)
            {
                long factionId = 0;
                var faction = MySession.Static.Factions.GetPlayerFaction(id);
                if (faction.FounderId == id || faction.IsLeader(id))
                {
                    factionId = faction.FactionId;
                }

                foreach (var conflict in currentConflicts)
                {
                    if (conflict.SubjectId != id && conflict.ChallengerId != id &&  conflict.SubjectId != factionId && conflict.ChallengerId != factionId) continue;
                }
            }

            return conflicts;
        }

        public enum ConflictState
        {
            PendingSubmission,
            PendingConflict,
            Active
        }

        public enum ConflictType
        {
            Player,
            Faction
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