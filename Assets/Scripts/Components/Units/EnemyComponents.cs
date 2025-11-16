using Unity.Entities;
using Unity.Mathematics;

namespace DotsRTS.Components.Units
{
    /// <summary>
    /// Base tag for all enemy entities
    /// </summary>
    public struct EnemyTag : IComponentData
    {
    }

    /// <summary>
    /// Basic enemy type
    /// </summary>
    public struct BasicEnemyTag : IComponentData
    {
    }

    /// <summary>
    /// Siege enemy (can damage buildings)
    /// </summary>
    public struct SiegeEnemyTag : IComponentData
    {
    }

    /// <summary>
    /// Fast enemy type
    /// </summary>
    public struct FastEnemyTag : IComponentData
    {
    }

    /// <summary>
    /// Tank enemy (high health, slow)
    /// </summary>
    public struct TankEnemyTag : IComponentData
    {
    }

    /// <summary>
    /// Enemy type identifier
    /// </summary>
    public enum EnemyType : byte
    {
        Basic,
        Fast,
        Tank,
        Siege,
        Boss
    }

    /// <summary>
    /// Enemy data and stats
    /// </summary>
    public struct EnemyData : IComponentData
    {
        public EnemyType Type;
        public float MoveSpeed;
        public float AttackDamage;
        public float AttackRange;
        public float AttackCooldown;
        public float LastAttackTime;
        public int WaveNumber;          // Which wave spawned this enemy
        public float ThreatLevel;       // AI priority for targeting
    }

    /// <summary>
    /// Enemy AI state
    /// </summary>
    public enum EnemyAIState : byte
    {
        Spawning,
        SeekingTarget,
        MovingToTarget,
        Attacking,
        Dead
    }

    /// <summary>
    /// Enemy AI component
    /// </summary>
    public struct EnemyAI : IComponentData
    {
        public EnemyAIState State;
        public Entity CurrentTarget;
        public float3 TargetPosition;
        public float RetargetTimer;
        public float RetargetInterval;
    }

    /// <summary>
    /// Component for enemies spawned in specific wave
    /// </summary>
    public struct WaveData : IComponentData
    {
        public int WaveNumber;
        public int NightNumber;
        public float SpawnTime;
    }
}
