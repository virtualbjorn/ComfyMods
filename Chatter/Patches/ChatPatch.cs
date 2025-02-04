﻿using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;

using static Chatter.Chatter;
using static Chatter.PluginConfig;

namespace Chatter.Patches {
  [HarmonyPatch(typeof(Chat))]
  class ChatPatch {
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Chat.Awake))]
    static void AwakePostfix(ref Chat __instance) {
      _chatInputField = __instance.m_input;

      BindChatConfig(__instance, _chatPanel);
      MessageRows.ClearItems();
      ToggleChatter(IsModEnabled.Value);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Chat.OnNewChatMessage))]
    static void OnNewChatMessagePrefix(
        ref long senderID, ref Vector3 pos, ref Talker.Type type, ref string user, ref string text) {
      if (!IsModEnabled.Value) {
        return;
      }

      _isCreatingChatMessage = true;

      ChatMessage message = new() {
        MessageType = GetMessageType(type),
        Timestamp = DateTime.Now,
        SenderId = senderID,
        Position = pos,
        TalkerType = type,
        Username = user,
        Text = Regex.Replace(text, @"(<|>)", " "),
      };

      AddChatMessage(message);
    }

    static ChatMessageType GetMessageType(Talker.Type talkerType) {
      return talkerType switch {
        Talker.Type.Normal => ChatMessageType.Say,
        Talker.Type.Shout => ChatMessageType.Shout,
        Talker.Type.Whisper => ChatMessageType.Whisper,
        Talker.Type.Ping => ChatMessageType.Ping,
        _ => ChatMessageType.Text
      };
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Chat.OnNewChatMessage))]
    static void OnNewChatMessagePostfix() {
      if (IsModEnabled.Value) {
        _isCreatingChatMessage = false;
      }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Chat.Update))]
    static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions) {
      return new CodeMatcher(instructions)
          .MatchForward(
              useEnd: true,
              new CodeMatch(OpCodes.Ldarg_0),
              new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Chat), nameof(Chat.m_hideTimer))),
              new CodeMatch(OpCodes.Ldarg_0),
              new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Chat), nameof(Chat.m_hideDelay))),
              new CodeMatch(OpCodes.Clt),
              new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive))))
          .InsertAndAdvance(
              new CodeInstruction(OpCodes.Ldarg_0),
              new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Chat), nameof(Chat.m_hideTimer))),
              Transpilers.EmitDelegate<Action<float>>(HideChatPanelDelegate))
          .MatchForward(
              useEnd: false,
              new CodeMatch(OpCodes.Ldarg_0),
              new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Terminal), nameof(Terminal.m_input))),
              new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "get_gameObject")),
              new CodeMatch(OpCodes.Ldc_I4_1),
              new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive))),
              new CodeMatch(OpCodes.Ldarg_0),
              new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Terminal), nameof(Terminal.m_input))),
              new CodeMatch(
                  OpCodes.Callvirt, AccessTools.Method(typeof(InputField), nameof(InputField.ActivateInputField))))
          .InsertAndAdvance(Transpilers.EmitDelegate<Action>(EnableChatPanelDelegate))
          .MatchForward(
              useEnd: false,
              new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Terminal), nameof(Terminal.m_input))),
              new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "get_gameObject")),
              new CodeMatch(OpCodes.Ldc_I4_0),
              new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive))),
              new CodeMatch(OpCodes.Ldarg_0),
              new CodeMatch(OpCodes.Ldc_I4_0),
              new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(Terminal), nameof(Terminal.m_focused))))
          .Advance(offset: 3)
          .InsertAndAdvance(Transpilers.EmitDelegate<Func<bool, bool>>(DisableChatPanelDelegate))
          .InstructionEnumeration();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Chat.Update))]
    static void UpdatePostfix(ref Chat __instance) {
      if (!IsModEnabled.Value || !IsChatPanelVisible) {
        return;
      }

      if (ScrollContentUpShortcut.Value.IsDown()) {
        Chatter.ChatPanel?.OffsetVerticalScrollPosition(ScrollContentOffsetInterval.Value);
        __instance.m_hideTimer = 0f;
      }

      if (ScrollContentDownShortcut.Value.IsDown()) {
        Chatter.ChatPanel?.OffsetVerticalScrollPosition(-ScrollContentOffsetInterval.Value);
        __instance.m_hideTimer = 0f;
      }
    }


    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Chat.AddInworldText))]
    static IEnumerable<CodeInstruction> AddInworldTextTranspiler(IEnumerable<CodeInstruction> instructions) {
      return new CodeMatcher(instructions)
          .MatchForward(
              useEnd: false,
              new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(string), nameof(string.ToUpper))))
          .SetInstructionAndAdvance(Transpilers.EmitDelegate<Func<string, string>>(ToUpperDelegate))
          .InstructionEnumeration();
    }

    static string ToUpperDelegate(string text) {
      return IsModEnabled.Value ? text : text.ToUpper();
    }
  }
}
