using Unity.Entities;
using Unity.Mathematics;

namespace DotsRTS.Components.GameState
{
    /// <summary>
    /// Day/night cycle state (singleton)
    /// </summary>
    public struct DayNightCycle : IComponentData
    {
        public bool IsNight;
        public int CurrentDay;
        public int CurrentNight;
        public float CurrentCycleTime;      // Time within current day/night
        public float DayDuration;
        public float NightDuration;
        public float TransitionProgress;    // 0-1 for smooth transitions
    }

    /// <summary>
    /// Wave director singleton
    /// </summary>
    public struct WaveDirector : IComponentData
    {
        public int CurrentWave;
        public int TotalWavesSpawned;
        public bool IsWaveActive;
        public float WaveStartTime;
        public float TimeBetweenSpawns;
        public float LastSpawnTime;
        public int EnemiesSpawnedThisWave;
        public int TotalEnemiesToSpawn;
        public int EnemiesAliveThisWave;
        public float DifficultyMultiplier;
    }

    /// <summary>
    /// Wave configuration
    /// </summary>
    public struct WaveConfig : IComponentData
    {
        public int BaseEnemyCount;
        public float EnemyCountScaling;     // Multiplier per wave
        public float HealthScaling;
        public float DamageScaling;
        public float SpeedScaling;

        public static WaveConfig CreateDefault()
        {
            return new WaveConfig
            {
                BaseEnemyCount = 50,
                EnemyCountScaling = 1.5f,
                HealthScaling = 1.2f,
                DamageScaling = 1.1f,
                SpeedScaling = 1.05f
            };
        }
    }

    /// <summary>
    /// Spawner entity
    /// </summary>
    public struct SpawnerTag : IComponentData
    {
    }

    /// <summary>
    /// Spawner data
    /// </summary>
    public struct Spawner : IComponentData
    {
        public float3 SpawnPosition;
        public float SpawnRadius;
        public int MaxActiveEnemies;
        public int CurrentActiveEnemies;
        public bool IsActive;
        public SpawnerType Type;
    }

    public enum SpawnerType : byte
    {
        North,
        South,
        East,
        West,
        Random
    }

    /// <summary>
    /// Game state (singleton)
    /// </summary>
    public struct GameState : IComponentData
    {
        public GamePhase Phase;
        public float GameTime;
        public bool IsPaused;
        public int PlayerID;
    }

    public enum GamePhase : byte
    {
        Initializing,
        Day,
        Night,
        Victory,
        Defeat
    }

    /// <summary>
    /// Victory/defeat conditions
    /// </summary>
    public struct VictoryConditions : IComponentData
    {
        public int SurviveNights;           // Win condition: survive X nights
        public bool HeadquartersDestroyed;  // Lose condition
        public bool AllEnemiesDefeated;
        public int NightsSurvived;
    }

    /// <summary>
    /// Player HQ/main building
    /// </summary>
    public struct HeadquartersTag : IComponentData
    {
    }
}
