using BepInEx.Configuration;

namespace InsanePhysics;

public class Config {
    public ConfigEntry<float> PlayerForceMultiplier { get; private set; }
    public ConfigEntry<float> HostileObjectPower { get; private set; }
    public ConfigEntry<bool> EnableHostileObjects { get; private set; }

    public Config(ConfigFile config) {
        PlayerForceMultiplier = config.Bind("Physics", "ForceMultiplier", 1.0f, "Multiplier for all physics forces applied to characters.");
        
        EnableHostileObjects = config.Bind("Chaos", "EnableHostileObjects", true, "If true, items occasionally launch themselves at players.");
        HostileObjectPower = config.Bind("Chaos", "HostileObjectPower", 300.0f, "Sets the force of hostile items flying towards players.");
    }
}