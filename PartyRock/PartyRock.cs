﻿using BepInEx;

using HarmonyLib;

using Steamworks;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.EventSystems;

using static PartyRock.PlayerListPanel;
using static PartyRock.PluginConfig;

namespace PartyRock {
  [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
  public class PartyRock : BaseUnityPlugin {
    public const string PluginGuid = "redseiko.valheim.partyrock";
    public const string PluginName = "PartyRock";
    public const string PluginVersion = "1.0.0";

    Harmony _harmony;

    public void Awake() {
      BindConfig(Config);
      _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGuid);
    }

    public void OnDestroy() {
      _harmony?.UnpatchSelf();
    }

    static PlayerListPanel _playerListPanel;
    static GameObject _playerCardHand;
    static readonly List<Card> _playerCards = new();

    public static void ToggleCardPanel() {
      if (!Hud.m_instance) {
        return;
      }

      if (_playerCards.Count == 0) {
        _playerCardHand = new("Player.CardHand", typeof(RectTransform));
        _playerCardHand.SetParent(Hud.m_instance.m_rootObject.transform);
        _playerCardHand.RectTransform()
            .SetAnchorMin(new(0.5f, 0.5f))
            .SetAnchorMax(new(0.5f, 0.5f))
            .SetPivot(new(0.5f, 0.5f))
            .SetPosition(CardHandPosition.Value);

        int numberOfCards = CardHandCount.Value;
        int startModifier = (numberOfCards % 2) - 1;
        float twistPerCard = CardHandCardTwist.Value;
        float nudgePerCard = CardHandCardNudge.Value;
        float startTwist = ((numberOfCards + startModifier) / 2f) * twistPerCard;
        float startNudge = ((numberOfCards + startModifier) / 2f) * nudgePerCard;

        for (int i = 0; i < numberOfCards; i++) {
          Card card = new(_playerCardHand.transform);
          card.Panel.RectTransform()
              .SetPosition(new(i * CardHandCardSpacing.Value, 0f))
              .SetSizeDelta(new(225f, 360f))
              .SetAnchorMin(new(0.5f, 0.5f))
              .SetAnchorMax(new(0.5f, 0.5f))
              .SetPivot(new(0.5f, 0.5f));

          card.Panel.RectTransform().Rotate(0f, 0f, startTwist - (i * twistPerCard));
          card.Panel.RectTransform().Translate(0f, -Mathf.Abs(startNudge - (i * nudgePerCard)), 0f);

          card.Panel.AddComponent<CardHandCardHover>();

          _playerCards.Add(card);
        }
      } else {
        foreach (Card card in _playerCards) {
          Destroy(card.Panel);
        }

        Destroy(_playerCardHand);
        _playerCards.Clear();
      }
    }

    public static void TogglePlayerListPanel() {
      if (!Hud.m_instance) {
        return;
      }

      if (!_playerListPanel?.Panel) {
        _playerListPanel = new(Hud.m_instance.m_rootObject.transform);

        _playerListPanel.Panel.RectTransform()
            .SetPosition(new(50, 125))
            .SetSizeDelta(new(250, 400));

        _playerListPanel.Panel.SetActive(false);
      }

      bool toggle = !_playerListPanel.Panel.activeSelf;
      _playerListPanel.Panel.SetActive(toggle);

      if (toggle) {
        ZLog.Log($"PartyRock: My SteamId is... {SteamUser.GetSteamID()}");
        PopulatePlayerList();
      } else {
        if (UpdatePlayerSlotsCoroutine != null) {
          Hud.m_instance.StopCoroutine(UpdatePlayerSlotsCoroutine);
          UpdatePlayerSlotsCoroutine = null;
        }
      }
    }

    static void PopulatePlayerList() {
      _playerListPanel.ClearList();

      foreach (ZNet.PlayerInfo playerInfo in ZNet.m_instance.m_players.Take(4)) {
        PlayerSlot slot = _playerListPanel.CreatePlayerSlot(playerInfo.m_name);
        ModelCache.Add(new PlayerSlotModel() { PlayerZdoid = playerInfo.m_characterID, Slot = slot });
      }

      UpdatePlayerSlotsCoroutine = Hud.m_instance.StartCoroutine(UpdatePlayerSlots());
    }

    public class PlayerSlotModel {
      public ZDOID PlayerZdoid { get; set; }
      public PlayerSlot Slot { get; set; }
    }

    public static readonly List<PlayerSlotModel> ModelCache = new();
    public static readonly int HealthHashCode = "health".GetStableHashCode();
    public static readonly int MaxHealthHashCode = "max_health".GetStableHashCode();

    static Coroutine UpdatePlayerSlotsCoroutine;

    static IEnumerator UpdatePlayerSlots() {
      WaitForSeconds waitInterval = new(seconds: 0.25f);
      ZDOMan zdoMan = ZDOMan.m_instance;

      while (true) {
        yield return waitInterval;

        foreach (PlayerSlotModel model in ModelCache) {
          zdoMan.RequestZDO(model.PlayerZdoid);
          ZDO playerZdo = zdoMan.GetZDO(model.PlayerZdoid);

          if (playerZdo == null) {
            continue;
          }

          float health = playerZdo.GetFloat(HealthHashCode, 0f);
          float maxHealth = playerZdo.GetFloat(MaxHealthHashCode, 25f);

          model.Slot.SetHealthValues(health, maxHealth);
        }
      }
    }
  }

  public static class ObjectExtensions {
    public static T Ref<T>(this T o) where T : UnityEngine.Object {
      return o ? o : null;
    }
  }
}