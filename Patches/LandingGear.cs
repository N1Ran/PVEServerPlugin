using System.Collections;
using System.Linq;
using System.Reflection;
using System.Security.RightsManagement;
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
    [PatchShim]
    public static class LandingGear
    {
        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyLandingGear).GetMethod("CanAttachTo",
                    BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes
                .Add(typeof(LandingGear).GetMethod(nameof(AttachCheck), BindingFlags.NonPublic | BindingFlags.Static));
        }


        private static bool AttachCheck(ref bool __result, MyLandingGear __instance, MyEntity entity, Vector3D worldPos)
        {
            if (!Config.Instance.EnablePlugin  || Config.Instance.AllowLandingGear) return true;
            if (__instance == entity || __instance.MarkedForClose || entity.MarkedForClose) return true;

            var grid1 = __instance.CubeGrid;

            if (grid1 == entity) return true;
            
            MyCubeGrid grid2 = null;
            switch (entity)
            {
                case MyCubeGrid grid:
                    grid2 = grid;
                    break;
                case MyCubeBlock block:
                    grid2 = block.CubeGrid;
                    break;
            }

            if (grid2 == null || grid1 == grid2) return true;

            __result = CanAttach(grid1, grid2, worldPos);
            return __result;

        }


        private static bool CanAttach(MyCubeGrid grid1, MyCubeGrid grid2, Vector3D position)
        {

            if (Config.Instance.PvpZones?.Count > 0)
            {
                if (Config.Instance.PvpZones.Any(zone =>
                        zone.IsWithinZoneRadius(position))) return true;
            }

            var firstOwner = TryGetOwner(grid1);
            var secondOwner = TryGetOwner(grid2);
            
            if (secondOwner == firstOwner || secondOwner == 0) return true;
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
            if (ConflictPairModule.InConflict(firstOwner,secondOwner, out var foundPair) 
                && (foundPair == null || foundPair.CurrentConflictState == ConflictPairModule.ConflictState.Active)) return true;

            return false;
        }

        private static long TryGetOwner(MyCubeGrid grid)
        {
            long owner = 0;
            if (grid.BigOwners?.Count > 0) owner = grid.BigOwners.FirstOrDefault();

            if (owner == 0)
            {
                foreach (var block in grid.CubeBlocks)
                {
                    if (block.OwnerId + block.BuiltBy == 0) continue;
                    if (block.OwnerId > 0)
                    {
                        owner = block.OwnerId;
                    }
                    else
                    {
                        owner = block.BuiltBy;
                    }
                }
            }


            return owner;
        }

    }
}