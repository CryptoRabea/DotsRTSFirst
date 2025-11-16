using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DotsRTS.Config
{
    /// <summary>
    /// Global game configuration - stored as singleton component
    /// </summary>
    public struct GameConfig : IComponentData
    {
        // Grid & World Settings
        public float GridCellSize;
        public int MapSize;
        public float3 MapCenter;

        // Performance Settings
        public int MaxEntities;
        public int MaxEnemiesPerWave;
        public bool EnableBurstCompilation;

        // Gameplay Settings
        public float DayDurationSeconds;
        public float NightDurationSeconds;
        public int StartingWood;
        public int StartingStone;
        public int StartingFood;
        public int StartingGold;

        // Combat Settings
        public float ProjectileSpeed;
        public float MeleeRange;
        public float ArcherRange;
        public float TowerRange;

        // Movement Settings
        public float WorkerSpeed;
        public float MeleeSpeed;
        public float RangedSpeed;
        public float EnemySpeed;

        public static GameConfig CreateDefault()
        {
            return new GameConfig
            {
                // Grid & World
                GridCellSize = 1f,
                MapSize = 256,
                MapCenter = float3.zero,

                // Performance
                MaxEntities = 1000000,
                MaxEnemiesPerWave = 50000,
                EnableBurstCompilation = true,

                // Gameplay
                DayDurationSeconds = 300f,  // 5 minutes
                NightDurationSeconds = 180f, // 3 minutes
                StartingWood = 200,
                StartingStone = 100,
                StartingFood = 100,
                StartingGold = 0,

                // Combat
                ProjectileSpeed = 20f,
                MeleeRange = 1.5f,
                ArcherRange = 15f,
                TowerRange = 25f,

                // Movement
                WorkerSpeed = 5f,
                MeleeSpeed = 6f,
                RangedSpeed = 5.5f,
                EnemySpeed = 4f
            };
        }
    }

    /// <summary>
    /// ScriptableObject for editing game config in Unity Editor
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "DotsRTS/Game Config")]
    public class GameConfigAuthoring : ScriptableObject
    {
        [Header("Grid & World Settings")]
        public float gridCellSize = 1f;
        public int mapSize = 256;

        [Header("Performance Settings")]
        public int maxEntities = 1000000;
        public int maxEnemiesPerWave = 50000;
        public bool enableBurstCompilation = true;

        [Header("Gameplay Settings")]
        public float dayDurationSeconds = 300f;
        public float nightDurationSeconds = 180f;
        public int startingWood = 200;
        public int startingStone = 100;
        public int startingFood = 100;
        public int startingGold = 0;

        [Header("Combat Settings")]
        public float projectileSpeed = 20f;
        public float meleeRange = 1.5f;
        public float archerRange = 15f;
        public float towerRange = 25f;

        [Header("Movement Settings")]
        public float workerSpeed = 5f;
        public float meleeSpeed = 6f;
        public float rangedSpeed = 5.5f;
        public float enemySpeed = 4f;

        public GameConfig ToComponentData()
        {
            return new GameConfig
            {
                GridCellSize = gridCellSize,
                MapSize = mapSize,
                MapCenter = float3.zero,
                MaxEntities = maxEntities,
                MaxEnemiesPerWave = maxEnemiesPerWave,
                EnableBurstCompilation = enableBurstCompilation,
                DayDurationSeconds = dayDurationSeconds,
                NightDurationSeconds = nightDurationSeconds,
                StartingWood = startingWood,
                StartingStone = startingStone,
                StartingFood = startingFood,
                StartingGold = startingGold,
                ProjectileSpeed = projectileSpeed,
                MeleeRange = meleeRange,
                ArcherRange = archerRange,
                TowerRange = towerRange,
                WorkerSpeed = workerSpeed,
                MeleeSpeed = meleeSpeed,
                RangedSpeed = rangedSpeed,
                EnemySpeed = enemySpeed
            };
        }
    }
}
