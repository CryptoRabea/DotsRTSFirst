using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using DotsRTS.Components.Resources;
using DotsRTS.Bootstrap;
using UnityEngine;

namespace DotsRTS.Systems.Economy
{
    /// <summary>
    /// Global resource management system
    /// Initializes and updates the global resource singleton
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EconomySystemGroup), OrderFirst = true)]
    public partial struct ResourceManagementSystem : ISystem
    {
        private bool m_Initialized;

        public void OnCreate(ref SystemState state)
        {
            m_Initialized = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            // Initialize global resources on first update
            if (!m_Initialized)
            {
                var config = SystemAPI.GetSingleton<Config.GameConfig>();

                var resourceEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(resourceEntity, new GlobalResources
                {
                    Wood = config.StartingWood,
                    Stone = config.StartingStone,
                    Food = config.StartingFood,
                    Gold = config.StartingGold,
                    CurrentPopulation = 0,
                    MaxPopulation = 5 // Starting cap
                });
                state.EntityManager.SetName(resourceEntity, "GlobalResources");

                m_Initialized = true;

                #if UNITY_EDITOR
                Debug.Log($"[ResourceManagement] Initialized with Wood: {config.StartingWood}, Stone: {config.StartingStone}");
                #endif
            }

            // Process resource transactions
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transaction, entity) in
                SystemAPI.Query<RefRW<ResourceTransaction>>().WithEntityAccess())
            {
                if (!transaction.ValueRO.IsProcessed)
                {
                    ProcessTransaction(ref state, transaction.ValueRO);
                    transaction.ValueRW.IsProcessed = true;
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        /// <summary>
        /// Process a resource transaction
        /// </summary>
        private void ProcessTransaction(ref SystemState state, ResourceTransaction transaction)
        {
            if (!SystemAPI.TryGetSingletonRW<GlobalResources>(out var resources))
                return;

            switch (transaction.Type)
            {
                case Components.Units.ResourceType.Wood:
                    resources.ValueRW.Wood += transaction.Amount;
                    break;
                case Components.Units.ResourceType.Stone:
                    resources.ValueRW.Stone += transaction.Amount;
                    break;
                case Components.Units.ResourceType.Food:
                    resources.ValueRW.Food += transaction.Amount;
                    break;
                case Components.Units.ResourceType.Gold:
                    resources.ValueRW.Gold += transaction.Amount;
                    break;
            }
        }
    }
}
