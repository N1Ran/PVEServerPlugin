using System.Collections.Generic;
using PVEServerPlugin.Modules;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Weapons;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace PVEServerPlugin.ProcessHandlers
{
    public class PvPZoneMessage : Base
    {

        public override int GetUpdateResolution()
        {
            return 200;
        }


        public override void Handle()
        {
            var entities = new HashSet<MyEntity>();
            
            Utilities.EntityCache.GetEntities(entities);

            if (entities.Count == 0) return;
            var zones = Config.Instance.PvpZones;

            if (zones == null || zones.Count == 0) return;

            foreach (var zone in zones)
            {
               if (!zone.Enable) continue;

               var zoneSphere = new BoundingSphere(new Vector3(zone.X, zone.Y, zone.Z), zone.Radius);
               
               foreach (var entity in entities)
               {
                   if (entity?.Physics == null || entity.Closed || entity.MarkedForClose) continue;

                   if (entity is MyVoxelBase || entity is MyAmmoBase) continue;

                   if (zone.ContainsEntities.Contains(entity.EntityId))
                   {
                       if (zoneSphere.Contains(entity.PositionComp.WorldVolume) != ContainmentType.Disjoint) continue;
                       zone.ContainsEntities.Remove(entity.EntityId);
                       if (entity is MyCharacter character) SendMessage(zone.ExitMessage, character.ControlSteamId);

                       if (entity is MyCubeGrid grid)
                           foreach (var controller in grid.GetFatBlocks<MyShipController>())
                           {
                               if (controller.Pilot == null || !zone.ContainsEntities.Contains(controller.Pilot.EntityId)) continue;
                               zone.ContainsEntities.Remove(controller.Pilot.EntityId);
                               SendMessage(zone.ExitMessage, controller.Pilot.ControlSteamId);
                           }
                   }
                   else if (zoneSphere.Contains(entity.PositionComp.WorldVolume) != ContainmentType.Disjoint)
                   {
                       zone.ContainsEntities.Add(entity.EntityId);
                       if (entity is MyCharacter character) SendMessage(zone.EntryMessage, character.ControlSteamId);

                       if (entity is MyCubeGrid grid)
                           foreach (var controller in grid.GetFatBlocks<MyShipController>())
                           {
                               if (controller.Pilot == null || zone.ContainsEntities.Contains(controller.Pilot.EntityId)) continue;
                               zone.ContainsEntities.Add(controller.Pilot.EntityId);
                               SendMessage(zone.EntryMessage, controller.Pilot.ControlSteamId);
                           }

                   }
                   

               }
            }

            void SendMessage(string msg, ulong playerSteamIds)
            {
                if (playerSteamIds == 0 || string.IsNullOrEmpty(msg)) return;
                ModCommunication.SendMessageTo(new NotificationMessage(msg, 5000, MyFontEnum.White),playerSteamIds);
            }

        }
    }
}