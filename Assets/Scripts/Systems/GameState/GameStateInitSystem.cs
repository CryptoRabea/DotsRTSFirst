using Unity.Burst;
using Unity.Entities;
using DotsRTS.Components.GameState;
using DotsRTS.Bootstrap;
using UnityEngine;

namespace DotsRTS.Systems.GameState
{
    /// <summary>
    /// Game state initialization system
    /// Sets up the game state singleton and victory conditions
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GameStateInitSystem : ISystem
    {
        private bool m_Initialized;

        public void OnCreate(ref SystemState state)
        {
            m_Initialized = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (m_Initialized)
            {
                state.Enabled = false;
                return;
            }

            // Create game state singleton
            var gameStateEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(gameStateEntity, new Components.GameState.GameState
            {
                Phase = GamePhase.Initializing,
                GameTime = 0f,
                IsPaused = false,
                PlayerID = 0
            });
            state.EntityManager.SetName(gameStateEntity, "GameState");

            // Create victory conditions
            var victoryEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(victoryEntity, new VictoryConditions
            {
                SurviveNights = 10, // Win by surviving 10 nights
                HeadquartersDestroyed = false,
                AllEnemiesDefeated = false,
                NightsSurvived = 0
            });
            state.EntityManager.SetName(victoryEntity, "VictoryConditions");

            m_Initialized = true;
            state.Enabled = false;

            #if UNITY_EDITOR
            Debug.Log("[GameStateInit] Game state initialized - Survive 10 nights to win!");
            #endif
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }

    /// <summary>
    /// Victory/defeat detection system
    /// Checks win/loss conditions each frame
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameStateSystemGroup), OrderLast = true)]
    public partial struct VictoryDetectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VictoryConditions>();
            state.RequireForUpdate<Components.GameState.GameState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<Components.GameState.GameState>();
            var victory = SystemAPI.GetSingletonRW<VictoryConditions>();

            // Skip if game already over
            if (gameState.ValueRO.Phase == GamePhase.Victory ||
                gameState.ValueRO.Phase == GamePhase.Defeat)
            {
                return;
            }

            // Check defeat condition: Headquarters destroyed
            if (victory.ValueRO.HeadquartersDestroyed)
            {
                gameState.ValueRW.Phase = GamePhase.Defeat;
                #if UNITY_EDITOR
                UnityEngine.Debug.Log("[VictoryDetection] DEFEAT - Headquarters destroyed!");
                #endif
                return;
            }

            // Update nights survived
            if (SystemAPI.TryGetSingleton<DayNightCycle>(out var cycle))
            {
                victory.ValueRW.NightsSurvived = cycle.CurrentNight;

                // Check victory condition: Survived required nights
                if (cycle.CurrentNight >= victory.ValueRO.SurviveNights && !cycle.IsNight)
                {
                    gameState.ValueRW.Phase = GamePhase.Victory;
                    #if UNITY_EDITOR
                    UnityEngine.Debug.Log($"[VictoryDetection] VICTORY - Survived {victory.ValueRO.SurviveNights} nights!");
                    #endif
                    return;
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
