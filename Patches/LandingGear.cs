using System.Collections;
using System.Reflection;
using PVEServerPlugin.Modules;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using VRage.Game.Entity;
using VRageMath;

namespace PVEServerPlugin.Patches
{
   /* 
    [PatchShim]
    public class LandingGear
    {
        public void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyLandingGear).GetMethod("CanAttachTo",
                    BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes
                .Add(typeof(LandingGear).GetMethod(nameof(AttachCheck), BindingFlags.NonPublic));
        }


        public bool AttachToPatch(ref bool __result, MyLandingGear __instance, MyEntity entity, Vector3D worldPos)
        {
            if (!Config.Instance.EnablePlugin  || Config.Instance.AllowLandingGear) return true;

            var firstOwner = __instance.CubeGrid.BigOwners[0];

            if (firstOwner == 0)
            {
                __result = false;
                return false;
            }
            long secondOwner;
            switch (entity)
            {
                case MyCubeGrid grid:
                    secondOwner = grid.BigOwners[0];
                    if (Config.Instance.PvpZones?.Count > 0)
                    {
                        foreach (var zone in Config.Instance.PvpZones)
                        {
                            if (!zone.IsWithinZoneRadius(grid)) continue;
                            return  true;
                        }
                    }

                    break;
                case MyCubeBlock block:
                    secondOwner = block.CubeGrid.BigOwners[0];
                    if (Config.Instance.PvpZones?.Count > 0)
                    {
                        foreach (var zone in Config.Instance.PvpZones)
                        {
                            if (!zone.IsWithinZoneRadius(block.CubeGrid)) continue;
                            return true;
                        }
                    }

                    break;
                default:
                    return true;
            }

            if (secondOwner == firstOwner || firstOwner == 0 || secondOwner == 0) return true;
            var attackerSteamId = MySession.Static.Players.TryGetSteamId(firstOwner);
            var targetSteamId = MySession.Static.Players.TryGetSteamId(secondOwner);
            if (attackerSteamId == targetSteamId) return true;
            if (Config.Instance.EnableFactionDamage)
            {
                var firstOwnerFaction = MySession.Static.Factions.GetPlayerFaction(firstOwner);
                var secondOwnerFaction = MySession.Static.Factions.GetPlayerFaction(secondOwner);
                if (firstOwnerFaction != null && secondOwnerFaction != null &&
                    firstOwnerFaction == secondOwnerFaction) return true;
            }
            if (ConflictPairModule.InConflict(firstOwner,secondOwner, out var foundPair) && (foundPair == null || foundPair.CurrentConflictState == ConflictPairModule.ConflictState.Active)) return true;

            __result = false;
            return false;
        }

    }
    */
}