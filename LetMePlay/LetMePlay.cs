﻿using BepInEx;

using HarmonyLib;

using System.Reflection;

using static LetMePlay.PluginConfig;

namespace LetMePlay {
  [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
  public class LetMePlay : BaseUnityPlugin {
    public const string PluginGUID = "redseiko.valheim.letmeplay";
    public const string PluginName = "LetMePlay";
    public const string PluginVersion = "1.4.0";
    
    Harmony _harmony;

    public void Awake() {
      BindConfig(Config);

      _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
    }

    public void OnDestroy() {
      _harmony?.UnpatchSelf();
    }
  }
}
