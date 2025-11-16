using Unity.Entities;
using Unity.Collections;

namespace DotsRTS.Utilities
{
    /// <summary>
    /// Pre-defined entity archetypes for efficient entity spawning
    /// Avoids structural changes by creating entities with complete component sets
    /// </summary>
    public struct EntityArchetypes
    {
        // Note: Archetypes will be populated when components are defined in Step 2
        // This class provides the infrastructure for archetype-based entity creation

        /// <summary>
        /// Create an archetype from component types
        /// </summary>
        public static EntityArchetype CreateArchetype(EntityManager entityManager, params ComponentType[] types)
        {
            return entityManager.CreateArchetype(types);
        }

        /// <summary>
        /// Helper to create multiple entities from an archetype efficiently
        /// </summary>
        public static void CreateEntities(
            EntityManager entityManager,
            EntityArchetype archetype,
            int count,
            out NativeArray<Entity> entities,
            Allocator allocator = Allocator.Temp)
        {
            entities = new NativeArray<Entity>(count, allocator);
            entityManager.CreateEntity(archetype, entities);
        }

        /// <summary>
        /// Batch create entities with archetype - uses NativeArray for zero allocations
        /// </summary>
        public static void CreateEntitiesBatch(
            EntityManager entityManager,
            EntityArchetype archetype,
            NativeArray<Entity> outputEntities)
        {
            entityManager.CreateEntity(archetype, outputEntities);
        }
    }

    /// <summary>
    /// Manages commonly-used archetypes for the game
    /// Initialized at startup for maximum performance
    /// </summary>
    public struct ArchetypeCache : IComponentData
    {
        // Archetypes will be defined when component types are created in Step 2
        // This provides a singleton storage for frequently-used archetypes

        public bool IsInitialized;

        // Note: Add specific archetype fields in Step 2 when components are defined
        // Example:
        // public EntityArchetype WorkerArchetype;
        // public EntityArchetype EnemyArchetype;
        // public EntityArchetype ProjectileArchetype;
    }
}
