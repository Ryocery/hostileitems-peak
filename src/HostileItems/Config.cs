using BepInEx.Configuration;

namespace HostileItems;

public class Config {
    public ConfigEntry<float> HostileObjectPower { get; private set; }
    public ConfigEntry<bool> EnableHostileObjects { get; private set; }
    public ConfigEntry<float> ImpactForceMultiplier { get; private set; }

    public Config(ConfigFile config) {
        EnableHostileObjects = config.Bind("Chaos", "EnableHostileObjects", true, "If true, items occasionally launch themselves at players.");
        HostileObjectPower = config.Bind("Chaos", "HostileObjectPower", 300.0f, "Sets the force of hostile items flying towards players.");
        ImpactForceMultiplier = config.Bind("Physics", "ImpactForceMultiplier", 10.0f, "Multiplier for the force applied to players when hit by a hostile item.");
    }
}