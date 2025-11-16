using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using DotsRTS.Components.GameState;
using DotsRTS.Bootstrap;
using DotsRTS.Config;
using UnityEngine;

namespace DotsRTS.Systems.GameState
{
    /// <summary>
    /// Day/night cycle system - manages time progression and transitions
    /// Enemies only attack during night, following "Diplomacy Is Not an Option" mechanics
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameStateSystemGroup), OrderFirst = true)]
    public partial struct DayNightCycleSystem : ISystem
    {
        private bool m_Initialized;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameConfig>();
            state.RequireForUpdate<GameTime>();
            m_Initialized = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<GameConfig>();
            var deltaTime = SystemAPI.GetSingleton<GameTime>().DeltaTime;

            // Initialize on first update
            if (!m_Initialized)
            {
                var cycleEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(cycleEntity, new DayNightCycle
                {
                    IsNight = false,
                    CurrentDay = 1,
                    CurrentNight = 0,
                    CurrentCycleTime = 0f,
                    DayDuration = config.DayDurationSeconds,
                    NightDuration = config.NightDurationSeconds,
                    TransitionProgress = 0f
                });
                state.EntityManager.SetName(cycleEntity, "DayNightCycle");

                m_Initialized = true;

                #if UNITY_EDITOR
                Debug.Log("[DayNightCycle] Initialized - Day: " + config.DayDurationSeconds + "s, Night: " + config.NightDurationSeconds + "s");
                #endif
            }

            // Update cycle
            if (SystemAPI.TryGetSingletonRW<DayNightCycle>(out var cycle))
            {
                cycle.ValueRW.CurrentCycleTime += deltaTime;

                float cycleDuration = cycle.ValueRO.IsNight ?
                    cycle.ValueRO.NightDuration :
                    cycle.ValueRO.DayDuration;

                // Update transition progress
                cycle.ValueRW.TransitionProgress = cycle.ValueRO.CurrentCycleTime / cycleDuration;

                // Check for cycle transition
                if (cycle.ValueRO.CurrentCycleTime >= cycleDuration)
                {
                    // Transition to next cycle
                    if (cycle.ValueRO.IsNight)
                    {
                        // Night → Day
                        cycle.ValueRW.IsNight = false;
                        cycle.ValueRW.CurrentDay++;
                        cycle.ValueRW.CurrentCycleTime = 0f;
                        cycle.ValueRW.TransitionProgress = 0f;

                        OnDayStart(ref state);

                        #if UNITY_EDITOR
                        Debug.Log($"[DayNightCycle] Day {cycle.ValueRO.CurrentDay} started");
                        #endif
                    }
                    else
                    {
                        // Day → Night
                        cycle.ValueRW.IsNight = true;
                        cycle.ValueRW.CurrentNight++;
                        cycle.ValueRW.CurrentCycleTime = 0f;
                        cycle.ValueRW.TransitionProgress = 0f;

                        OnNightStart(ref state);

                        #if UNITY_EDITOR
                        Debug.Log($"[DayNightCycle] Night {cycle.ValueRO.CurrentNight} started - ENEMIES INCOMING!");
                        #endif
                    }
                }

                // Update game state phase
                if (SystemAPI.TryGetSingletonRW<Components.GameState.GameState>(out var gameState))
                {
                    if (cycle.ValueRO.IsNight)
                    {
                        gameState.ValueRW.Phase = GamePhase.Night;
                    }
                    else
                    {
                        gameState.ValueRW.Phase = GamePhase.Day;
                    }
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        /// <summary>
        /// Called when day starts
        /// </summary>
        private void OnDayStart(ref SystemState state)
        {
            // Deactivate all spawners
            foreach (var spawner in SystemAPI.Query<RefRW<Spawner>>())
            {
                spawner.ValueRW.IsActive = false;
            }

            // Update wave director
            if (SystemAPI.TryGetSingletonRW<WaveDirector>(out var director))
            {
                director.ValueRW.IsWaveActive = false;
            }
        }

        /// <summary>
        /// Called when night starts
        /// </summary>
        private void OnNightStart(ref SystemState state)
        {
            // Activate all spawners
            foreach (var spawner in SystemAPI.Query<RefRW<Spawner>>())
            {
                spawner.ValueRW.IsActive = true;
                spawner.ValueRW.CurrentActiveEnemies = 0;
            }

            // Start wave
            if (SystemAPI.TryGetSingletonRW<WaveDirector>(out var director))
            {
                director.ValueRW.IsWaveActive = true;
                director.ValueRW.CurrentWave++;

                // Get current night number
                if (SystemAPI.TryGetSingleton<DayNightCycle>(out var cycle))
                {
                    director.ValueRW.WaveStartTime = SystemAPI.GetSingleton<GameTime>().ElapsedTime;
                    director.ValueRW.EnemiesSpawnedThisWave = 0;

                    // Calculate enemies for this wave (scaling difficulty)
                    if (SystemAPI.TryGetSingleton<WaveConfig>(out var config))
                    {
                        float scaling = math.pow(config.EnemyCountScaling, cycle.CurrentNight - 1);
                        director.ValueRW.TotalEnemiesToSpawn = (int)(config.BaseEnemyCount * scaling);
                        director.ValueRW.DifficultyMultiplier = 1f + (cycle.CurrentNight * 0.2f);
                    }
                }
            }
        }
    }
}
