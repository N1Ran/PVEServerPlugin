using System;
using NLog;
using NLog.Fluent;
using PVEServerPlugin.Modules;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace PVEServerPlugin
{
    public static class DamageHandler
    {
        private static bool _init;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static void Init()
        {
            if (_init)
                return;

            _init = true;

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(1, ProcessDamage);
        }


        private static void ProcessDamage(object target, ref MyDamageInformation info)
        {
            if (!Config.Instance.EnablePlugin) return;
            long id;
            long attackerId = info.AttackerId;
            switch (target)
            {
                case MySlimBlock block:
                    id = block.CubeGrid.BigOwners.Count> 0 ? block.CubeGrid.BigOwners[0]:0;
                    break;
                case MyCharacter character:
                    //id = character.GetPlayerIdentityId();
                    return;
                default:
                    id = 0;
                    break;
            }

            if (MyEntities.TryGetEntityById(info.AttackerId, out var attacker, true))
            {
                if (attacker is MyVoxelBase)
                    return;

                if (attacker is MyAngleGrinder grinder)
                {
                    attackerId = grinder.OwnerIdentityId;
                }

                if (attacker is MyUserControllableGun controllableGun)
                {
                    attackerId = controllableGun.OwnerId;
                }

                if (attacker is MyShipToolBase tool)
                {
                    attackerId = tool.OwnerId;
                }
                if (attacker is MyAutomaticRifleGun characterWeapon)
                {
                    attackerId = characterWeapon.OwnerIdentityId;
                }

                if (attacker is MyCubeGrid grid)
                {
                    attackerId = grid.BigOwners.Count > 0 ? grid.BigOwners[0] : 0;
                }

                if (attacker is MyLargeTurretBase turret)
                {
                    attackerId = turret.OwnerId;
                }
            }

            var attackerSteamId = MySession.Static.Players.TryGetSteamId(attackerId);
            var targetSteamId = MySession.Static.Players.TryGetSteamId(id);

            if (Config.Instance.EnableConflict && Utility.InConflict(attackerId,id, out var foundPair) && !foundPair.Pending) return;

            if (MySession.Static.Players.IdentityIsNpc(attackerId) ||id == 0 || MySession.Static.Players.IdentityIsNpc(id) || id == info.AttackerId || attackerSteamId == targetSteamId ||MySession.Static.Factions.TryGetPlayerFaction(attackerId) == MySession.Static.Factions.TryGetPlayerFaction(id))return;
            info.Amount = 0;
        }

    }
}