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

            // Add spawner data
            var transform = GetComponent<Transform>();
            AddComponent(entity, new Spawner
            {
                SpawnPosition = transform.position,
                SpawnRadius = authoring.spawnRadius,
                MaxActiveEnemies = authoring.maxActiveEnemies,
                CurrentActiveEnemies = 0,
                IsActive = authoring.startActive,
                Type = authoring.spawnerType
            });
        }
    }
}
