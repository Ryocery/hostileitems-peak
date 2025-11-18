using BepInEx;
using BepInEx.Logging;

namespace InsanePhysics;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin {
    private static ManualLogSource Log { get; set; } = null!;

    private void Awake() {
        Log = Logger;
        Log.LogInfo($"Plugin {Name} is loaded!");
    }
}
