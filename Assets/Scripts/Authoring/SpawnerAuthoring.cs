using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using DotsRTS.Components.GameState;

namespace DotsRTS.Authoring
{
    /// <summary>
    /// Authoring component for enemy spawners
    /// </summary>
    public class SpawnerAuthoring : MonoBehaviour
    {
        [Header("Spawner Settings")]
        public SpawnerType spawnerType = SpawnerType.North;
        public float spawnRadius = 5f;
        public int maxActiveEnemies = 1000;
        public bool startActive = false;

        [Header("Enemy Prefab")]
        [Tooltip("The enemy prefab to spawn (must be in a subscene or have a baker)")]
        public GameObject enemyPrefab;
    }

    /// <summary>
    /// Baker for Spawner entities
    /// </summary>
    public class SpawnerBaker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Add spawner tag
            AddComponent<SpawnerTag>(entity);

            // Get prefab entity reference (if assigned)
            Entity prefabEntity = Entity.Null;
            if (authoring.enemyPrefab != null)
            {
                prefabEntity = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic);
            }

            // Add spawner data
            var transform = GetComponent<Transform>();
            AddComponent(entity, new Spawner
            {
                SpawnPosition = transform.position,
                SpawnRadius = authoring.spawnRadius,
                MaxActiveEnemies = authoring.maxActiveEnemies,
                CurrentActiveEnemies = 0,
                IsActive = authoring.startActive,
                Type = authoring.spawnerType,
                EnemyPrefab = prefabEntity
            });
        }
    }
}
