﻿using HarmonyLib;

using UnityEngine;

using static GetOffMyLawn.PluginConfig;

namespace GetOffMyLawn {
  [HarmonyPatch(typeof(BaseAI))]
  public class BaseAIPatch {
    static readonly string[] TargetRayMask = new string[] {
        "Default", "static_solid", "Default_small", "vehicle",
      };

    [HarmonyPostfix]
    [HarmonyPatch(nameof(BaseAI.Awake))]
    static void AwakePostfix(ref BaseAI __instance) {
      if (IsModEnabled.Value) {
        __instance.m_monsterTargetRayMask = LayerMask.GetMask(TargetRayMask);
      }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(BaseAI.FindRandomStaticTarget))]
    static bool FindRandomStaticTargetPrefix(ref StaticTarget __result) {
      if (!IsModEnabled.Value) {
        return true;
      }

      __result = null;
      return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(BaseAI.FindClosestStaticPriorityTarget))]
    static bool FindClosestStaticPriorityTargetPrefix(ref StaticTarget __result) {
      if (!IsModEnabled.Value) {
        return true;
      }

      __result = null;
      return false;
    }
  }
}
