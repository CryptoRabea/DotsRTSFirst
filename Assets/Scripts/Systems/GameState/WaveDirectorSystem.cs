using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.GameState;
using DotsRTS.Components.Units;
using DotsRTS.Components.Combat;
using DotsRTS.Components.Movement;
using DotsRTS.Bootstrap;
using DotsRTS.Utilities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DotsRTS.Systems.GameState
{
    /// <summary>
    /// Wave director system - manages enemy spawning during night cycles
    /// Scales difficulty based on night number
    /// Supports massive enemy counts (up to 1M)
    /// </summary>
    [UpdateInGroup(typeof(GameStateSystemGroup))]
    [UpdateAfter(typeof(DayNightCycleSystem))]
    public partial struct WaveDirectorSystem : ISystem
    {
        private bool m_Initialized;
        private Random m_Random;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTime>();
            m_Initialized = false;
            m_Random = new Random(1234); // Seed for deterministic behavior
        }

        public void OnUpdate(ref SystemState state)
        {
            var currentTime = SystemAPI.GetSingleton<GameTime>().ElapsedTime;

            // Initialize on first update
            if (!m_Initialized)
            {
                InitializeWaveDirector(ref state);
                m_Initialized = true;
            }

            // Check if we have a wave director
            if (!SystemAPI.TryGetSingletonRW<WaveDirector>(out var director))
                return;

            // Only spawn during active waves (night time)
            if (!director.ValueRO.IsWaveActive)
                return;

            // Check if we've spawned all enemies for this wave
            if (director.ValueRO.EnemiesSpawnedThisWave >= director.ValueRO.TotalEnemiesToSpawn)
                return;

            // Check spawn timing
            float timeSinceLastSpawn = currentTime - director.ValueRO.LastSpawnTime;

            if (timeSinceLastSpawn >= director.ValueRO.TimeBetweenSpawns)
            {
                // Spawn enemy batch
                int enemiesToSpawn = math.min(
                    100, // Batch size
                    director.ValueRO.TotalEnemiesToSpawn - director.ValueRO.EnemiesSpawnedThisWave
                );

                SpawnEnemyBatch(
                    ref state,
                    enemiesToSpawn,
                    director.ValueRO.CurrentWave,
                    director.ValueRO.DifficultyMultiplier
                );

                director.ValueRW.EnemiesSpawnedThisWave += enemiesToSpawn;
                director.ValueRW.LastSpawnTime = currentTime;

                #if UNITY_EDITOR
                Debug.Log($"[WaveDirector] Spawned {enemiesToSpawn} enemies. Total: {director.ValueRO.EnemiesSpawnedThisWave}/{director.ValueRO.TotalEnemiesToSpawn}");
                #endif
            }

            // Count alive enemies
            int aliveEnemies = 0;
            foreach (var health in SystemAPI.Query<RefRO<Health>>().WithAll<EnemyTag>())
            {
                if (!health.ValueRO.IsDead)
                    aliveEnemies++;
            }

            director.ValueRW.EnemiesAliveThisWave = aliveEnemies;
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        /// <summary>
        /// Initialize wave director singleton
        /// </summary>
        private void InitializeWaveDirector(ref SystemState state)
        {
            var directorEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(directorEntity, new WaveDirector
            {
                CurrentWave = 0,
                TotalWavesSpawned = 0,
                IsWaveActive = false,
                WaveStartTime = 0f,
                TimeBetweenSpawns = 0.5f, // Spawn every 0.5 seconds
                LastSpawnTime = 0f,
                EnemiesSpawnedThisWave = 0,
                TotalEnemiesToSpawn = 0,
                EnemiesAliveThisWave = 0,
                DifficultyMultiplier = 1f
            });
            state.EntityManager.SetName(directorEntity, "WaveDirector");

            // Create wave config
            var configEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(configEntity, WaveConfig.CreateDefault());
            state.EntityManager.SetName(configEntity, "WaveConfig");

            #if UNITY_EDITOR
            Debug.Log("[WaveDirector] Initialized");
            #endif
        }

        /// <summary>
        /// Spawn a batch of enemies
        /// </summary>
        private void SpawnEnemyBatch(
            ref SystemState state,
            int count,
            int waveNumber,
            float difficultyMultiplier)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Get all active spawners
            var spawners = new NativeList<SpawnerInfo>(Allocator.Temp);

            foreach (var (spawner, transform) in
                SystemAPI.Query<RefRO<Spawner>, RefRO<LocalTransform>>())
            {
                if (spawner.ValueRO.IsActive)
                {
                    spawners.Add(new SpawnerInfo
                    {
                        Position = spawner.ValueRO.SpawnPosition,
                        Radius = spawner.ValueRO.SpawnRadius
                    });
                }
            }

            if (spawners.Length == 0)
            {
                spawners.Dispose();
                ecb.Dispose();
                return;
            }

            // Get wave config for scaling
            var waveConfig = SystemAPI.GetSingleton<WaveConfig>();

            // Spawn enemies
            for (int i = 0; i < count; i++)
            {
                // Choose random spawner
                int spawnerIndex = m_Random.NextInt(0, spawners.Length);
                var spawnerInfo = spawners[spawnerIndex];

                // Random position within spawner radius
                float2 randomOffset = RandomHelpers.RandomInCircle(ref m_Random, spawnerInfo.Radius);
                float3 spawnPosition = spawnerInfo.Position + new float3(randomOffset.x, 0, randomOffset.y);

                // Create enemy
                CreateEnemy(
                    ref ecb,
                    spawnPosition,
                    waveNumber,
                    difficultyMultiplier,
                    waveConfig
                );
            }

            ecb.Playback(state.EntityManager);

            spawners.Dispose();
            ecb.Dispose();
        }

        /// <summary>
        /// Create an enemy entity
        /// </summary>
        private void CreateEnemy(
            ref EntityCommandBuffer ecb,
            float3 position,
            int waveNumber,
            float difficultyMultiplier,
            WaveConfig waveConfig)
        {
            var enemy = ecb.CreateEntity();

            // Determine enemy type based on wave
            EnemyType enemyType = DetermineEnemyType(waveNumber, ref m_Random);

            // Calculate scaled stats
            float baseHealth = 100f;
            float baseDamage = 10f;
            float baseSpeed = 4f;

            float healthScaling = math.pow(waveConfig.HealthScaling, waveNumber - 1);
            float damageScaling = math.pow(waveConfig.DamageScaling, waveNumber - 1);
            float speedScaling = math.pow(waveConfig.SpeedScaling, waveNumber - 1);

            float finalHealth = baseHealth * healthScaling * difficultyMultiplier;
            float finalDamage = baseDamage * damageScaling * difficultyMultiplier;
            float finalSpeed = baseSpeed * speedScaling;

            // Add components
            ecb.AddComponent(enemy, LocalTransform.FromPosition(position));
            ecb.AddComponent<EnemyTag>(enemy);
            ecb.AddComponent<BasicEnemyTag>(enemy);

            ecb.AddComponent(enemy, new EnemyData
            {
                Type = enemyType,
                MoveSpeed = finalSpeed,
                AttackDamage = finalDamage,
                AttackRange = 1.5f,
                AttackCooldown = 1.5f,
                LastAttackTime = 0f,
                WaveNumber = waveNumber,
                ThreatLevel = 1f
            });

            ecb.AddComponent(enemy, new EnemyAI
            {
                State = EnemyAIState.Spawning,
                CurrentTarget = Entity.Null,
                TargetPosition = float3.zero,
                RetargetTimer = 0f,
                RetargetInterval = 2f
            });

            ecb.AddComponent(enemy, new WaveData
            {
                WaveNumber = waveNumber,
                NightNumber = waveNumber,
                SpawnTime = SystemAPI.GetSingleton<GameTime>().ElapsedTime
            });

            ecb.AddComponent(enemy, new UnitOwnership
            {
                PlayerID = -1,
                TeamID = -1,
                IsPlayerControlled = false
            });

            ecb.AddComponent(enemy, new Health
            {
                Current = finalHealth,
                Maximum = finalHealth,
                IsDead = false,
                LastDamageTime = 0f
            });

            ecb.AddComponent(enemy, new Movement
            {
                Velocity = float3.zero,
                MoveSpeed = finalSpeed,
                RotationSpeed = 8f,
                Acceleration = 15f,
                CurrentDirection = new float3(0, 0, 1)
            });

            ecb.AddComponent(enemy, new MoveTarget
            {
                Destination = float3.zero,
                HasDestination = false,
                StoppingDistance = 1.5f,
                ReachedDestination = false
            });

            ecb.AddComponent(enemy, new Steering
            {
                DesiredVelocity = float3.zero,
                SteeringForce = float3.zero,
                MaxForce = 20f,
                MaxSpeed = finalSpeed,
                SeparationWeight = 1.5f,
                AlignmentWeight = 1f,
                CohesionWeight = 1f
            });

            ecb.AddComponent(enemy, new Avoidance
            {
                AvoidanceRadius = 1f,
                AvoidanceForce = 3f,
                AvoidUnits = true,
                AvoidBuildings = false
            });

            ecb.AddComponent(enemy, new CanAttack
            {
                AttackRange = 1.5f,
                AttackDamage = finalDamage,
                AttackCooldown = 1.5f,
                LastAttackTime = 0f,
                DamageType = DamageType.Physical,
                RequiresLineOfSight = false
            });

            ecb.AddComponent(enemy, new AttackTarget
            {
                Target = Entity.Null,
                LastKnownPosition = float3.zero,
                AcquisitionTime = 0f
            });

            ecb.AddComponent(enemy, new FlowFieldAgent
            {
                CurrentCell = int2.zero,
                TargetCell = int2.zero,
                FlowDirection = float3.zero,
                UseFlowField = true
            });
        }

        /// <summary>
        /// Determine enemy type based on wave number
        /// </summary>
        private EnemyType DetermineEnemyType(int waveNumber, ref Random random)
        {
            // Early waves: only basic enemies
            if (waveNumber <= 2)
                return EnemyType.Basic;

            // Later waves: mix of enemy types
            float roll = random.NextFloat();

            if (waveNumber >= 5 && roll < 0.1f)
                return EnemyType.Siege; // 10% siege units on wave 5+

            if (waveNumber >= 3 && roll < 0.3f)
                return EnemyType.Tank; // 20% tank units on wave 3+

            if (roll < 0.5f)
                return EnemyType.Fast; // 20% fast units

            return EnemyType.Basic; // 50% basic units
        }
    }

    /// <summary>
    /// Spawner information
    /// </summary>
    public struct SpawnerInfo
    {
        public float3 Position;
        public float Radius;
    }
}
