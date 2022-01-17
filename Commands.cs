using PVEServerPlugin.Modules;
using Sandbox.Game.World;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace PVEServerPlugin
{
    [Category("pve")]
    public partial class Commands:CommandModule
    {
        [Command("challenge player", "Issues a challenge to a specified player.  Use player name.")]
        [Permission(MyPromoteLevel.None)]
        public void PlayerChallenge(string playerName)
        {
            if (!Config.Instance.EnableConflict || Core.NexusDetected)
            {
                Context.Respond("Conflict is not enabled");
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                Context.Respond("Command requires the name of the player you wish to challenge");
            }

            var id = Context.Player.IdentityId;
            if (id == 0)
            {
                Context.Respond("This command can only be used ingame");
                return;
            }

            var subjectId = Utilities.IdentityUtility.GetPlayerId(playerName);
            if (subjectId == 0)
            {
                Context.Respond("Challenge failed. Player with name " + playerName + " not found or not online");
                return;
            }

            ConflictPairModule.IssueChallenge(id, subjectId,ConflictPairModule.ConflictType.Player,Context.Player.SteamUserId);
        }

        [Command("challenge faction", "Issues a challenge to a specified faction.  Use faction tag")]
        [Permission(MyPromoteLevel.None)]
        public void FactionChallenge(string factionNameOrTag)
        {

            if (!Config.Instance.EnableConflict || Core.NexusDetected)
            {
                Context.Respond("Conflict is not enabled");
                return;
            }

            if (string.IsNullOrEmpty(factionNameOrTag))
            {
                Context.Respond("Command requires the faction name or tag you wish to challenge");
            }

            var playerId = Context.Player.IdentityId;

            if (playerId == 0)
            {
                Context.Respond("This command can only be used ingame");
                return;
            }

            var playerFaction = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            var subjectId = Utilities.IdentityUtility.GetFactionId(factionNameOrTag);
            if (subjectId == 0 || playerFaction == null ||(!playerFaction.IsLeader(playerId) && !playerFaction.IsFounder(playerId)))
            {
                Context.Respond("Faction error. This command requires you to be the founder or leader of your faction and a valid faction name or tag to issue challenge");
                return;
            }

            if (MySession.Static.Factions.IsNpcFaction(subjectId))
            {
                Context.Respond("Command cannot be used with NPC faction");
                return;
            }

            ConflictPairModule.IssueChallenge(playerFaction.FactionId, subjectId, ConflictPairModule.ConflictType.Faction, Context.Player.SteamUserId);
        }

        [Command("accept challenge", "Accepts all current challenge or specified player name or faction tag at the end of command")]
        [Permission(MyPromoteLevel.None)]
        public void AcceptChallenge()
        
        {
            if (!Config.Instance.EnableConflict || Core.NexusDetected)
            {
                Context.Respond("Conflict is not enabled");
                return;
            }


            if (Context.Player == null)
            {
                Context.Respond("This command can only be used ingame");
                return;
            }

            var playerId = Context.Player.IdentityId;

            if (Context.Args?.Count == 0)
            {
                if (ConflictPairModule.AcceptChallenge(playerId,Context.Player.SteamUserId) == 0)
                {
                    Context.Respond("No pending conflict found");
                    return;
                }
            }

            if (Context.Args != null)
            {
                var factionId = Utilities.IdentityUtility.GetFactionId(Context.Args[0]);
                var challengingPlayerId = Utilities.IdentityUtility.GetPlayerId(Context.Args[0]);
                if (factionId > 0)
                {
                    var playerFaction = MySession.Static.Factions.TryGetPlayerFaction(playerId);
                    if (playerFaction == null || (!playerFaction.IsFounder(playerId) && !playerFaction.IsLeader(playerId)))
                    {
                        Context.Respond("You either do not belong to a faction or do not have permission to accept challenge");
                        return;
                    }
                    ConflictPairModule.AcceptChallenge(playerFaction.FactionId, factionId, ConflictPairModule.ConflictType.Faction, Context.Player.SteamUserId);
                    return;
                }

                if (challengingPlayerId > 0)
                {
                    ConflictPairModule.AcceptChallenge(playerId, challengingPlayerId, ConflictPairModule.ConflictType.Player, Context.Player.SteamUserId);
                    return;
                }
                Context.Respond($"{Context.Args[0]} not found");

            }


        }


        [Command("accept submission", "Accepts specified or all submissions.  Add player name or faction tag at the end of command to specify")]
        [Permission(MyPromoteLevel.None)]
        public void AcceptSubmission()
        {
            if (!Config.Instance.EnableConflict || Core.NexusDetected)
            {
                Context.Respond("Conflict is not enabled");
                return;
            }

            var playerId = Context.Player.IdentityId;

            if (playerId == 0)
            {
                Context.Respond("This command can only be used ingame");
                return;
            }

            if (Context.Args.Count < 1)
            {
                if (ConflictPairModule.AcceptSubmission(playerId) == 0)
                {
                    Context.Respond("No pending conflict found");
                    return;
                }
            }

            var factionId = Utilities.IdentityUtility.GetFactionId(Context.Args[0]);
            var challengingPlayerId = Utilities.IdentityUtility.GetPlayerId(Context.Args[0]);
            if (factionId > 0)
            {
                var playerFaction = MySession.Static.Factions.TryGetPlayerFaction(playerId);
                if (playerFaction == null || (!playerFaction.IsFounder(playerId) && !playerFaction.IsLeader(playerId)))
                {
                    Context.Respond("You either do not belong to a faction or do not have permission to accept submission");
                    return;
                }
                ConflictPairModule.AcceptSubmission(playerFaction.FactionId, factionId, ConflictPairModule.ConflictType.Faction, Context.Player.SteamUserId);
                return;
            }

            if (challengingPlayerId > 0)
            {
                ConflictPairModule.AcceptSubmission(playerId, challengingPlayerId, ConflictPairModule.ConflictType.Player, Context.Player.SteamUserId);
                return;
            }

            Context.Respond($"{Context.Args[0]} not found");
        }

        [Command("submit faction", "Initiates submission by requesting to submit to all or specified faction tag or player name")]
        [Permission(MyPromoteLevel.None)]
        public void FactionSubmit(string factionNameOrTag)
        {

            if (!Config.Instance.EnableConflict || Core.NexusDetected)
            {
                Context.Respond("Conflict is not enabled");
                return;
            }
            if (string.IsNullOrEmpty(factionNameOrTag))
            {
                Context.Respond("Command requires the faction name or tag you wish to challenge");
            }

            var playerId = Context.Player.IdentityId;

            if (playerId == 0)
            {
                Context.Respond("This command can only be used ingame");
                return;
            }

            var playerFaction = MySession.Static.Factions.GetPlayerFaction(Context.Player.IdentityId);
            var subjectId = Utilities.IdentityUtility.GetFactionId(factionNameOrTag);
            if (subjectId == 0 || playerFaction == null ||(!playerFaction.IsLeader(playerId) && !playerFaction.IsFounder(playerId)))
            {
                Context.Respond("Faction error. This command requires you to be the founder or leader of your faction and a valid faction name or tag to issue challenge");
                return;
            }

            if (MySession.Static.Factions.IsNpcFaction(subjectId))
            {
                Context.Respond("Command cannot be used with NPC faction");
                return;
            }

            ConflictPairModule.RequestSubmit(playerFaction.FactionId, subjectId, ConflictPairModule.ConflictType.Faction,Context.Player.SteamUserId);
        }

        [Command("submit player","Issues submissions to a specified player or faction")]
        [Permission(MyPromoteLevel.None)]
        public void PlayerSubmit(string playerName)
        {

            if (!Config.Instance.EnableConflict || Core.NexusDetected)
            {
                Context.Respond("Conflict is not enabled");
                return;
            }
            if (string.IsNullOrEmpty(playerName))
            {
                Context.Respond("Command requires the player name or tag you wish to challenge");
            }

            var playerId = Utilities.IdentityUtility.GetPlayerId(playerName);

            if (playerId == 0)
            {
                Context.Respond("This command can only be used ingame");
                return;
            }


            ConflictPairModule.RequestSubmit(playerId, Context.Player.SteamUserId);
        }
    }
}