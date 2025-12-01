using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HostileItems.Handlers;
using InsanePhysics;
using UnityEngine;

namespace HostileItems;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin {
    public static ManualLogSource Log { get; private set; } = null!;
    
    private Config? _config;
    private ProjectileHandler? _projectileHandler;
    private NetworkHandler? _networkHandler;
    private Harmony? _harmony;

    private void Awake() {
        Log = Logger;
        Log.LogInfo($"Plugin {Name} is loading...");
        _config = new Config(Config);

        _harmony = new Harmony(Id);
        _harmony.PatchAll();
        Log.LogInfo("Harmony Patches applied.");

        if (_config.EnableHostileObjects.Value) {
            _networkHandler = gameObject.AddComponent<NetworkHandler>();
            GameObject o = gameObject;
            _projectileHandler = o.AddComponent<ProjectileHandler>();
            Log.LogInfo("Hostile Physics Handlers initialized.");
        }

        Log.LogInfo($"Plugin {Name} loaded successfully.");
    }

    private void Update() {
        _projectileHandler?.Update();
    }

    private void OnDestroy() {
        _projectileHandler?.Dispose();
        _networkHandler?.Dispose();
        _harmony?.UnpatchSelf();
    }
}