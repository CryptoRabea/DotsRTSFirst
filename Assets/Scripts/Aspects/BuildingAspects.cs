using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Buildings;
using DotsRTS.Components.Combat;

namespace DotsRTS.Aspects
{
    /// <summary>
    /// Basic building aspect
    /// </summary>
    public readonly partial struct BuildingAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRO<LocalTransform> m_Transform;
        private readonly RefRW<BuildingData> m_Data;
        private readonly RefRO<Health> m_Health;

        public float3 Position => m_Transform.ValueRO.Position;
        public BuildingType Type => m_Data.ValueRO.Type;
        public bool IsConstructed => m_Data.ValueRO.IsConstructed;
        public float ConstructionProgress => m_Data.ValueRO.ConstructionProgress;
        public int2 GridPosition => m_Data.ValueRO.GridPosition;
        public int2 GridSize => m_Data.ValueRO.GridSize;

        public float CurrentHealth => m_Health.ValueRO.Current;
        public bool IsDestroyed => m_Health.ValueRO.IsDead;

        public void UpdateConstructionProgress(float progress)
        {
            m_Data.ValueRW.ConstructionProgress = math.clamp(progress, 0f, 1f);
            m_Data.ValueRW.IsConstructed = progress >= 1f;
        }
    }

    /// <summary>
    /// Tower aspect - defensive structure
    /// </summary>
    public readonly partial struct TowerAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRO<LocalTransform> m_Transform;
        private readonly RefRW<TowerData> m_TowerData;
        private readonly RefRO<BuildingData> m_BuildingData;

        public float3 Position => m_Transform.ValueRO.Position;
        public bool IsConstructed => m_BuildingData.ValueRO.IsConstructed;

        public float AttackRange => m_TowerData.ValueRO.AttackRange;
        public float AttackDamage => m_TowerData.ValueRO.AttackDamage;
        public float AttackCooldown => m_TowerData.ValueRO.AttackCooldown;
        public float LastAttackTime => m_TowerData.ValueRO.LastAttackTime;

        public Entity CurrentTarget
        {
            get => m_TowerData.ValueRO.CurrentTarget;
            set => m_TowerData.ValueRW.CurrentTarget = value;
        }

        public bool CanAttackNow(float currentTime)
        {
            return IsConstructed && (currentTime - LastAttackTime) >= AttackCooldown;
        }

        public void RecordAttack(float currentTime)
        {
            m_TowerData.ValueRW.LastAttackTime = currentTime;
        }

        public bool IsInRange(float3 targetPosition)
        {
            float distSq = math.distancesq(Position, targetPosition);
            return distSq <= (AttackRange * AttackRange);
        }
    }

    /// <summary>
    /// Production building aspect
    /// </summary>
    public readonly partial struct ProductionAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRW<ProductionBuilding> m_Production;
        private readonly RefRO<BuildingData> m_BuildingData;

        public bool IsConstructed => m_BuildingData.ValueRO.IsConstructed;
        public bool IsProducing => m_Production.ValueRO.IsProducing;
        public float ProductionProgress => m_Production.ValueRO.ProductionProgress;
        public int QueueCount => m_Production.ValueRO.QueueCount;

        public void StartProduction(Entity unitEntity, float productionTime)
        {
            if (!IsConstructed) return;

            m_Production.ValueRW.CurrentProducingUnit = unitEntity;
            m_Production.ValueRW.ProductionTime = productionTime;
            m_Production.ValueRW.ProductionProgress = 0f;
            m_Production.ValueRW.IsProducing = true;
        }

        public void UpdateProduction(float deltaProgress)
        {
            if (!IsProducing) return;

            m_Production.ValueRW.ProductionProgress += deltaProgress;

            if (m_Production.ValueRO.ProductionProgress >= 1f)
            {
                CompleteProduction();
            }
        }

        public void CompleteProduction()
        {
            m_Production.ValueRW.IsProducing = false;
            m_Production.ValueRW.ProductionProgress = 0f;
            m_Production.ValueRW.QueueCount = math.max(0, m_Production.ValueRO.QueueCount - 1);
        }
    }

    /// <summary>
    /// Wall aspect
    /// </summary>
    public readonly partial struct WallAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRO<LocalTransform> m_Transform;
        private readonly RefRW<WallData> m_WallData;
        private readonly RefRO<Health> m_Health;

        public float3 Position => m_Transform.ValueRO.Position;
        public int2 GridPosition => m_WallData.ValueRO.GridPosition;
        public WallOrientation Orientation => m_WallData.ValueRO.Orientation;
        public bool IsCorner => m_WallData.ValueRO.IsCorner;
        public bool IsDestroyed => m_Health.ValueRO.IsDead;

        public void SetOrientation(WallOrientation orientation)
        {
            m_WallData.ValueRW.Orientation = orientation;
        }

        public void ConnectWall(Entity neighbor, int2 direction)
        {
            if (direction.x == 0 && direction.y == 1)
                m_WallData.ValueRW.ConnectedWallNorth = neighbor;
            else if (direction.x == 0 && direction.y == -1)
                m_WallData.ValueRW.ConnectedWallSouth = neighbor;
            else if (direction.x == 1 && direction.y == 0)
                m_WallData.ValueRW.ConnectedWallEast = neighbor;
            else if (direction.x == -1 && direction.y == 0)
                m_WallData.ValueRW.ConnectedWallWest = neighbor;
        }
    }
}
