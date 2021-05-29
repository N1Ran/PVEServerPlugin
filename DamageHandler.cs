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
                    id = block.CubeGrid.BigOwners.Count > 0 ? block.CubeGrid.BigOwners[0]:0;
                    break;
                case MyCharacter _:
                    return;
                case MyCubeGrid grid:
                    id =  grid.BigOwners.Count > 0 ? grid.BigOwners[0]:0;
                    break;
                default:
                    id = 0;
                    break;
            }

            if (id == 0 || MySession.Static.Players.IdentityIsNpc(id) || MySession.Static.Players.IdentityIsNpc(attackerId) || id == attackerId) return;

            if (MyEntities.TryGetEntityById(attackerId, out var attacker, true))
            {
                foreach (var zone in Config.Instance.PvpZones)
                {
                    if (!zone.IsWithinZoneRadius(attacker)) continue;
                    return;
                }

                if (attacker is MyVoxelBase)
                    return;

                if (attacker is MyCubeBlock block)
                {
                    attackerId = block.CubeGrid.BigOwners[0];
                }

                if (attacker is MyHandToolBase handTool)
                {
                    attackerId = handTool.OwnerIdentityId;
                }

                if (attacker is MyAngleGrinder grinder)
                {
                    attackerId = grinder.OwnerIdentityId;
                }

                if (attacker is MyUserControllableGun controllableGun)
                {
                    attackerId = controllableGun.CubeGrid.BigOwners[0];
                }

                if (attacker is MyAutomaticRifleGun gun)
                {
                    attackerId = gun.OwnerIdentityId;
                }

                if (attacker is MyShipToolBase tool)
                {
                    attackerId = tool.CubeGrid.BigOwners[0];
                }

                if (attacker is MyAutomaticRifleGun characterWeapon)
                {
                    attackerId = characterWeapon.OwnerIdentityId;
                }

                if (attacker is MyCubeGrid grid)
                {
                    attackerId = grid.BigOwners[0];
                }

                if (attacker is MyLargeTurretBase turret)
                {
                    attackerId = turret.CubeGrid.BigOwners[0];
                }
            }


            var attackerSteamId = MySession.Static.Players.TryGetSteamId(attackerId);
            var targetSteamId = MySession.Static.Players.TryGetSteamId(id);

            if (ConflictPairModule.InConflict(attackerId,id, out var foundPair) && (foundPair == null || !foundPair.ConflictPending)) return;

            if (Config.Instance.EnableFactionDamage)
            {
                var fac1 = MySession.Static.Factions.TryGetPlayerFaction(attackerId);
                var fac2 = MySession.Static.Factions.TryGetPlayerFaction(id);

                if (fac2 != null && fac1 != null && fac2 == fac1) return;
            } 

            if ((Config.Instance.EnableNoOwner && attackerId == 0) || attackerSteamId == targetSteamId)return;
            info.Amount = 0;
        }

    }
}