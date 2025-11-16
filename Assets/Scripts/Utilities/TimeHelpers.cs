using Unity.Entities;
using Unity.Burst;

namespace DotsRTS.Utilities
{
    /// <summary>
    /// Singleton component for tracking game time
    /// Updated each frame by TimeUpdateSystem
    /// </summary>
    public struct GameTime : IComponentData
    {
        public float DeltaTime;
        public float ElapsedTime;
        public uint FrameCount;
    }

    /// <summary>
    /// Burst-compatible time utility functions
    /// </summary>
    [BurstCompile]
    public static class TimeHelpers
    {
        /// <summary>
        /// Convert seconds to frames (at 60 FPS)
        /// </summary>
        [BurstCompile]
        public static int SecondsToFrames(float seconds, float fps = 60f)
        {
            return (int)(seconds * fps);
        }

        /// <summary>
        /// Convert frames to seconds (at 60 FPS)
        /// </summary>
        [BurstCompile]
        public static float FramesToSeconds(int frames, float fps = 60f)
        {
            return frames / fps;
        }

        /// <summary>
        /// Check if enough time has elapsed since last event
        /// </summary>
        [BurstCompile]
        public static bool HasElapsed(float lastEventTime, float currentTime, float interval)
        {
            return (currentTime - lastEventTime) >= interval;
        }

        /// <summary>
        /// Get time remaining until next event
        /// </summary>
        [BurstCompile]
        public static float TimeRemaining(float lastEventTime, float currentTime, float interval)
        {
            float elapsed = currentTime - lastEventTime;
            return interval - elapsed;
        }
    }
}
