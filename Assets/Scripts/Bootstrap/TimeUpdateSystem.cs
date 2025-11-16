using Unity.Entities;
using Unity.Burst;
using DotsRTS.Utilities;
using UnityEngine;

namespace DotsRTS.Bootstrap
{
    /// <summary>
    /// Updates the GameTime singleton component each frame
    /// Provides deltaTime and elapsedTime to all systems
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct TimeUpdateSystem : ISystem
    {
        private bool m_Initialized;
        private Entity m_TimeEntity;

        public void OnCreate(ref SystemState state)
        {
            m_Initialized = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            // Initialize time entity on first run
            if (!m_Initialized)
            {
                m_TimeEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(m_TimeEntity, new GameTime
                {
                    DeltaTime = 0f,
                    ElapsedTime = 0f,
                    FrameCount = 0
                });
                state.EntityManager.SetName(m_TimeEntity, "GameTime");
                m_Initialized = true;

                #if UNITY_EDITOR
                Debug.Log("[TimeUpdateSystem] GameTime entity created");
                #endif
            }

            // Update time values
            if (SystemAPI.TryGetSingletonRW<GameTime>(out var gameTime))
            {
                gameTime.ValueRW.DeltaTime = SystemAPI.Time.DeltaTime;
                gameTime.ValueRW.ElapsedTime = (float)SystemAPI.Time.ElapsedTime;
                gameTime.ValueRW.FrameCount++;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
