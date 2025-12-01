using HarmonyLib;
using UnityEngine;
using HostileItems; 

namespace HostileItems.Patches;

[HarmonyPatch(typeof(Character))]
public class CharacterPatch {
    private static bool ShouldApplyForces(Character character) {
        if (!character.data) return true;
        return !character.data.isClimbing && !character.data.isRopeClimbing && !character.data.isVineClimbing;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Character.AddForce), typeof(Vector3), typeof(float), typeof(float))]
    public static void AddForce_Prefix(Character __instance, ref Vector3 move) {
        if (ShouldApplyForces(__instance)) {
            move *= Plugin.Config.PlayerForceMultiplier.Value;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Character.RPCA_AddForceAtPosition))]
    public static void RPCA_AddForceAtPosition_Prefix(Character __instance, ref Vector3 force) {
        if (ShouldApplyForces(__instance)) {
            force *= Plugin.Config.PlayerForceMultiplier.Value;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Character.RPCA_AddForceToBodyPart))]
    public static void RPCA_AddForceToBodyPart_Prefix(Character __instance, ref Vector3 force, ref Vector3 wholeBodyForce) {
        if (!ShouldApplyForces(__instance)) return;
        force *= Plugin.Config.PlayerForceMultiplier.Value;
        wholeBodyForce *= Plugin.Config.PlayerForceMultiplier.Value;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Character.DragTowards))]
    public static void DragTowards_Prefix(Character __instance, ref float force) {
        if (ShouldApplyForces(__instance)) {
            force *= Plugin.Config.PlayerForceMultiplier.Value;
        }
    }
}