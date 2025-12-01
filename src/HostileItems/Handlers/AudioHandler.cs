using System.Collections;
using System.Linq;
using UnityEngine;

namespace HostileItems.Handlers;

public static class AudioHandler {
    private static SFX_Instance[]? _cachedBonkSounds;

    public static IEnumerator LoadSounds() {
        yield return null; 

        Item? coconut = Resources.FindObjectsOfTypeAll<Item>().FirstOrDefault(x => x.name.Contains("Coconut"));
        Bonkable? bonkComponent = coconut?.GetComponent<Bonkable>();

        if (bonkComponent?.bonk != null) {
            _cachedBonkSounds = bonkComponent.bonk;
            Debug.Log($"[HostileItems] Stole audio from {coconut?.name}");
        } else {
            Debug.LogWarning("[HostileItems] Failed to find Coconut audio!");
        }
    }

    public static void PlayBonk(Vector3 position) {
        if (_cachedBonkSounds == null || _cachedBonkSounds.Length == 0) return;

        foreach (SFX_Instance sfx in _cachedBonkSounds) {
            if (sfx) sfx.Play(position);
        }
    }
}