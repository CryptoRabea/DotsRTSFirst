using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Movement;
using DotsRTS.Bootstrap;
using DotsRTS.Utilities;
using DotsRTS.Config;
using MovementComponent = DotsRTS.Components.Movement.Movement;

namespace DotsRTS.Systems.Movement
{
    /// <summary>
    /// Flow field pathfinding system - enables efficient movement for massive agent counts
    /// Uses grid-based navigation where each cell stores a direction to the goal
    /// Optimized for 100k-1M+ entities
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MovementSystemGroup))]
    [UpdateBefore(typeof(TargetSeekingSystem))]
    public partial struct FlowFieldSystem : ISystem
    {
        private NativeArray<float3> m_FlowField;
        private int m_GridSize;
        private float m_CellSize;
        private bool m_IsInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameConfig>();
            m_IsInitialized = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Initialize flow field on first update
            if (!m_IsInitialized)
            {
                var config = SystemAPI.GetSingleton<GameConfig>();
                m_GridSize = config.MapSize;
                m_CellSize = config.GridCellSize;
                m_FlowField = new NativeArray<float3>(
                    m_GridSize * m_GridSize,
                    Allocator.Persistent
                );
                m_IsInitialized = true;

                // Initialize with default directions (toward center)
                InitializeFlowField(state, new float3(0, 0, 0));
            }

            // Update agent cells and apply flow directions
            new UpdateFlowFieldAgentsJob
            {
                FlowField = m_FlowField,
                GridSize = m_GridSize,
                CellSize = m_CellSize
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (m_IsInitialized && m_FlowField.IsCreated)
            {
                m_FlowField.Dispose();
            }
        }

        /// <summary>
        /// Initialize flow field with directions pointing toward target
        /// Simple version - can be enhanced with dijkstra/A* for obstacle avoidance
        /// </summary>
        private void InitializeFlowField(SystemState state, float3 targetPosition)
        {
            new InitializeFlowFieldJob
            {
                FlowField = m_FlowField,
                GridSize = m_GridSize,
                CellSize = m_CellSize,
                TargetPosition = targetPosition
            }.Schedule(m_FlowField.Length, 64).Complete();
        }
    }

    /// <summary>
    /// Initialize flow field with directions toward target
    /// </summary>
    [BurstCompile]
    public struct InitializeFlowFieldJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<float3> FlowField;
        public int GridSize;
        public float CellSize;
        public float3 TargetPosition;

        [BurstCompile]
        public void Execute(int index)
        {
            // Convert 1D index to 2D grid coordinates
            int x = index % GridSize;
            int z = index / GridSize;

            // Calculate world position of this cell
            float3 cellWorldPos = new float3(
                (x - GridSize / 2) * CellSize,
                0,
                (z - GridSize / 2) * CellSize
            );

            // Calculate direction to target
            float3 direction = math.normalizesafe(TargetPosition - cellWorldPos);

            FlowField[index] = direction;
        }
    }

    /// <summary>
    /// Update flow field agents - set their velocity based on flow field
    /// </summary>
    [BurstCompile]
    public partial struct UpdateFlowFieldAgentsJob : IJobEntity
    {
        [ReadOnly] public NativeArray<float3> FlowField;
        public int GridSize;
        public float CellSize;

        [BurstCompile]
        private void Execute(
            in LocalTransform transform,
            ref FlowFieldAgent agent,
            ref MovementComponent movement)
        {
            if (!agent.UseFlowField) return;

            // Convert world position to grid coordinates
            int x = (int)((transform.Position.x / CellSize) + GridSize / 2);
            int z = (int)((transform.Position.z / CellSize) + GridSize / 2);

            // Clamp to grid bounds
            x = math.clamp(x, 0, GridSize - 1);
            z = math.clamp(z, 0, GridSize - 1);

            // Update current cell
            agent.CurrentCell = new int2(x, z);

            // Get flow direction from grid
            int index = z * GridSize + x;
            if (index >= 0 && index < FlowField.Length)
            {
                agent.FlowDirection = FlowField[index];

                // Apply flow direction to velocity
                if (math.lengthsq(agent.FlowDirection) > 0.001f)
                {
                    float3 desiredVelocity = agent.FlowDirection * movement.MoveSpeed;
                    movement.Velocity = math.lerp(movement.Velocity, desiredVelocity, 0.1f);
                }
            }
        }
    }

    /// <summary>
    /// Flow field manager singleton - stores configuration
    /// </summary>
    public struct FlowFieldConfig : IComponentData
    {
        public int GridSize;
        public float CellSize;
        public float3 TargetPosition;
        public bool NeedsRecalculation;
    }
}
