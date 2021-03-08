using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRage;
using VRage.Game.Entity;

namespace PVEServerPlugin.Utility
{
    public class EntityCache
    {
        private static readonly HashSet<MyEntity> _entityCache = new HashSet<MyEntity>();
        private static readonly Dictionary<long, List<long>> _bigBuilders = new Dictionary<long, List<long>>();
        private static readonly HashSet<MyCubeGrid> _dirtyEntities = new HashSet<MyCubeGrid>();
        private static int _updateCounter;
        private static readonly FastResourceLock _entityLock = new FastResourceLock();
        private static readonly FastResourceLock _builderLock = new FastResourceLock();

        static EntityCache()
        {

        }

        public static void Update()
        {
            if(Thread.CurrentThread != MySandboxGame.Static.UpdateThread)
                throw new Exception("Update called from wrong thread");

            using(_entityLock.AcquireExclusiveUsing())
            {
                var e = MyEntities.GetEntities();
                //KEEN WHAT THE FUCK ARE YOU **DOING?!?!**
                if (e.Any())
                {
                    _entityCache.Clear();
                    _entityCache.UnionWith(e);
                }
            }

        }

        public static bool TryGetEntityById(long entityId, out MyEntity entity)
        {
            using(_entityLock.AcquireSharedUsing())
            {
                entity = _entityCache.FirstOrDefault(e => e.EntityId == entityId);
                return entity != null;
            }
        }

        public static void GetEntities(HashSet<MyEntity> entities)
        {
            using(_entityLock.AcquireSharedUsing())
            {
                entities.UnionWith(_entityCache);
            }
        }

    }
}