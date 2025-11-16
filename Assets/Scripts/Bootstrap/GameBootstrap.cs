using Unity.Entities;
using UnityEngine;

namespace DotsRTS.Bootstrap
{
    /// <summary>
    /// Main game bootstrap - initializes the DOTS world and core systems
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private static GameBootstrap s_Instance;

        public static GameBootstrap Instance => s_Instance;

        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool enableLogging = true;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);

            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            if (enableLogging)
            {
                Debug.Log("[GameBootstrap] Initializing DOTS RTS Framework...");
            }

            // World is automatically created by Unity DOTS
            // Systems are auto-registered via [CreateAfter]/[UpdateInGroup] attributes

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                if (enableLogging)
                {
                    Debug.Log($"[GameBootstrap] Default world '{world.Name}' ready");
                    Debug.Log($"[GameBootstrap] Systems registered: {world.Systems.Count}");
                }
            }
            else
            {
                Debug.LogError("[GameBootstrap] Default world not found!");
            }

            if (enableLogging)
            {
                Debug.Log("[GameBootstrap] Initialization complete");
            }
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }
    }
}
