using Unity.Entities;
using Unity.Burst;
using DotsRTS.Config;
using UnityEngine;

namespace DotsRTS.Bootstrap
{
    /// <summary>
    /// Initializes game configuration as a singleton entity
    /// Runs once at startup
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ConfigLoaderSystem : ISystem
    {
        private bool m_Initialized;

        [BurstCompile]
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

            // Create default config (can be overridden by loading from ScriptableObject)
            var config = GameConfig.CreateDefault();

            // Create singleton entity with config
            var configEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(configEntity, config);
            state.EntityManager.SetName(configEntity, "GameConfig");

            m_Initialized = true;
            state.Enabled = false;

            #if UNITY_EDITOR
            Debug.Log("[ConfigLoaderSystem] Game configuration initialized");
            #endif
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
