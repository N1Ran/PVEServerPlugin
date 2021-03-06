﻿using PVEServerPlugin.Modules;
using Sandbox.Game.World;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace PVEServerPlugin
{
    [Category("pve")]
    public partial class Commands:CommandModule
    {
        [Command("challenge player")]
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

            var challengingId = Utility.Utilities.GetPlayerId(playerName);
            if (challengingId == 0)
            {
                Context.Respond("Challenge failed. Player with name " + playerName + " not found or not online");
                return;
            }

            ConflictPairModule.IssueChallenge(id, challengingId,Context.Player.SteamUserId);
        }

        [Command("challenge faction")]
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
            var challengingId = Utility.Utilities.GetFactionId(factionNameOrTag);
            if (challengingId == 0 || playerFaction == null ||(!playerFaction.IsLeader(playerId) && !playerFaction.IsFounder(playerId)))
            {
                Context.Respond("Faction error. This command requires you to be the founder or leader of your faction and a valid faction name or tag to issue challenge");
                return;
            }

            if (MySession.Static.Factions.IsNpcFaction(challengingId))
            {
                Context.Respond("Command cannot be used with NPC faction");
                return;
            }

            ConflictPairModule.IssueChallenge(playerFaction.FactionId, challengingId,Context.Player.SteamUserId);
        }

        [Command("accept challenge")]
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
                if (!ConflictPairModule.AcceptChallenge(playerId,Context.Player.SteamUserId))
                {
                    Context.Respond("No pending conflict found");
                    return;
                }
            }

            if (Context.Args != null)
            {
                var factionId = Utility.Utilities.GetFactionId(Context.Args[0]);
                var challengingPlayerId = Utility.Utilities.GetPlayerId(Context.Args[0]);
                if (factionId > 0)
                {
                    var playerFaction = MySession.Static.Factions.TryGetPlayerFaction(playerId);
                    if (playerFaction == null || (!playerFaction.IsFounder(playerId) && !playerFaction.IsLeader(playerId)))
                    {
                        Context.Respond("You either do not belong to a faction or do not have permission to accept challenge");
                        return;
                    }
                    ConflictPairModule.AcceptChallenge(playerId, factionId,Context.Player.SteamUserId);
                    return;
                }

                if (challengingPlayerId > 0)
                {
                    ConflictPairModule.AcceptChallenge(playerId, challengingPlayerId,Context.Player.SteamUserId);
                    return;
                }
                Context.Respond($"{Context.Args[0]} not found");

            }


        }

        [Command("accept submission")]
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
                if (!ConflictPairModule.AcceptSubmission(playerId))
                {
                    Context.Respond("No pending conflict found");
                    return;
                }
            }

            var factionId = Utility.Utilities.GetFactionId(Context.Args[0]);
            var challengingPlayerId = Utility.Utilities.GetPlayerId(Context.Args[0]);
            if (factionId > 0)
            {
                var playerFaction = MySession.Static.Factions.TryGetPlayerFaction(playerId);
                if (playerFaction == null || (!playerFaction.IsFounder(playerId) && !playerFaction.IsLeader(playerId)))
                {
                    Context.Respond("You either do not belong to a faction or do not have permission to accept submission");
                    return;
                }
                ConflictPairModule.AcceptSubmission(playerId, factionId,Context.Player.SteamUserId);
                return;
            }

            if (challengingPlayerId > 0)
            {
                ConflictPairModule.AcceptSubmission(playerId, challengingPlayerId,Context.Player.SteamUserId);
                return;
            }

            Context.Respond($"{Context.Args[0]} not found");
        }

        [Command("submit faction")]
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
            var challengingId = Utility.Utilities.GetFactionId(factionNameOrTag);
            if (challengingId == 0 || playerFaction == null ||(!playerFaction.IsLeader(playerId) && !playerFaction.IsFounder(playerId)))
            {
                Context.Respond("Faction error. This command requires you to be the founder or leader of your faction and a valid faction name or tag to issue challenge");
                return;
            }

            if (MySession.Static.Factions.IsNpcFaction(challengingId))
            {
                Context.Respond("Command cannot be used with NPC faction");
                return;
            }

            ConflictPairModule.RequestSubmit(playerFaction.FactionId, challengingId,Context.Player.SteamUserId);
        }
    }
}