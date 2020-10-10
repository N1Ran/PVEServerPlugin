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
        [Command("challenge player")]
        [Permission(MyPromoteLevel.None)]
        public void PlayerChallenge(string playerName)
        {
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

            var challengingId = Utility.GetPlayerId(playerName);
            if (challengingId == 0)
            {
                Context.Respond("Challenge failed. Player with name" + playerName + "not found");
                return;
            }

            Utility.IssueChallenge(id, challengingId);
        }

        [Command("challenge faction")]
        [Permission(MyPromoteLevel.None)]
        public void FactionChallenge(string factionNameOrTag)
        {
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
            var challengingId = Utility.GetFactionId(factionNameOrTag);
            if (challengingId == 0 || playerFaction == null ||(!playerFaction.IsLeader(playerId) && !playerFaction.IsFounder(playerId)))
            {
                Context.Respond("Faction error. This command requires you to be the founder or leader of your faction and a valid faction name or tag to issue challenge");
                return;
            }

            Utility.IssueChallenge(playerFaction.FactionId, challengingId);
        }
    }
}