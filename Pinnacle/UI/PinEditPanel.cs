﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using static Pinnacle.PluginConfig;

namespace Pinnacle {
  public class PinEditPanel {
    public const long DefaultSharedPinOwnerId = long.MaxValue;

    public GameObject Panel { get; private set; }
    public LabelValueRow PinName { get; private set; }

    public LabelRow PinIconSelectorLabelRow { get; private set; }
    public PinIconSelector PinIconSelector { get; private set; }

    public LabelValueRow PinType { get; private set; }

    public LabelRow PinModifierRow { get; private set; }
    public ToggleCell PinChecked { get; private set; }
    public ToggleCell PinShared { get; private set; }

    public LabelRow PinPositionLabelRow { get; private set; }
    public VectorCell PinPosition { get; private set; }

    // Styling.
    readonly List<Text> Labels = new();
    readonly List<GameObject> ValueCells = new();

    // HasFocus.
    readonly List<GameObject> Selectables = new();

    Coroutine _setActiveCoroutine;

    public PinEditPanel(Transform parentTransform) {
      Panel = CreatePanel(parentTransform);

      PinName = new(Panel.transform);
      PinName.Label.SetText("Name");
      PinName.Value.InputField.onEndEdit.AddListener(OnPinNameValueChange);

      PinIconSelectorLabelRow = new(Panel.transform);
      PinIconSelectorLabelRow.Label
          .SetAlignment(TextAnchor.UpperLeft)
          .SetText("Icon");

      PinIconSelector = new(PinIconSelectorLabelRow.Row.transform);
      PinIconSelector.OnPinIconClicked += (_, pinType) => OnPinTypeValueChange(pinType);

      PinType = new(Panel.transform);
      PinType.Label.SetText("Type");
      PinType.Value.InputField.SetInteractable(false);

      PinModifierRow = new(Panel.transform);
      PinModifierRow.Label.SetText("Modifier");

      PinChecked = new(PinModifierRow.Row.transform);
      PinChecked.Label.SetText("Checked");
      PinChecked.Toggle.onValueChanged.AddListener(OnPinCheckedChange);

      PinShared = new(PinModifierRow.Row.transform);
      PinShared.Label.SetText("Shared");
      PinShared.Toggle.onValueChanged.AddListener(OnPinSharedChange);

      PinPositionLabelRow = new(Panel.transform);
      PinPositionLabelRow.Label.SetText("Position");

      PinPosition = new(PinPositionLabelRow.Row.transform);

      PinPosition.XValue.InputField.textComponent.SetColor(new(1f, 0.878f, 0.51f));
      PinPosition.XValue.InputField.contentType = InputField.ContentType.DecimalNumber;
      PinPosition.XValue.InputField.characterValidation = InputField.CharacterValidation.Decimal;
      PinPosition.XValue.InputField.onEndEdit.AddListener(_ => OnPinPositionValueChange());

      PinPosition.YValue.InputField.textComponent.SetColor(new(0.565f, 0.792f, 0.976f));
      PinPosition.YValue.InputField.contentType = InputField.ContentType.DecimalNumber;
      PinPosition.YValue.InputField.characterValidation = InputField.CharacterValidation.Decimal;
      PinPosition.YValue.InputField.onEndEdit.AddListener(_ => OnPinPositionValueChange());

      PinPosition.ZValue.InputField.textComponent.SetColor(new(0.647f, 0.839f, 0.655f));
      PinPosition.ZValue.InputField.contentType = InputField.ContentType.DecimalNumber;
      PinPosition.ZValue.InputField.characterValidation = InputField.CharacterValidation.Decimal;
      PinPosition.ZValue.InputField.onEndEdit.AddListener(_ => OnPinPositionValueChange());

      Selectables.AddRange(Panel.GetComponentsInChildren<Selectable>().Select(s => s.gameObject));

      Labels.Add(
          PinName.Label,
          PinIconSelectorLabelRow.Label,
          PinType.Label,
          PinModifierRow.Label,
          PinPositionLabelRow.Label);

      ValueCells.Add(
          PinName.Value.Cell,
          PinType.Value.Cell);

      SetPanelStyle();
    }

    public void SetActive(bool toggle) {
      if (_setActiveCoroutine != null) {
        Minimap.m_instance.StopCoroutine(_setActiveCoroutine);
      }

      _setActiveCoroutine =
          Minimap.m_instance.StartCoroutine(
              LerpCanvasGroupAlpha(
                  Panel.GetComponent<CanvasGroup>(), toggle ? 1f : 0f, PinEditPanelToggleLerpDuration.Value));
    }

    static IEnumerator LerpCanvasGroupAlpha(CanvasGroup canvasGroup, float targetAlpha, float lerpDuration) {
      float timeElapsed = 0f;
      float sourceAlpha = canvasGroup.alpha;

      canvasGroup.SetBlocksRaycasts(targetAlpha > 0f);

      while (timeElapsed < lerpDuration) {
        float t = timeElapsed / lerpDuration;
        t = t * t * (3f - (2f * t));

        canvasGroup.SetAlpha(Mathf.Lerp(sourceAlpha, targetAlpha, t));
        timeElapsed += Time.deltaTime;

        yield return null;
      }

      canvasGroup.SetAlpha(targetAlpha);
    }

    public void SetPanelStyle() {
      float labelWidth = Labels.Select(label => label.GetPreferredWidth()).Max();
      Labels.ForEach(l => l.GetComponent<LayoutElement>().SetPreferred(width: labelWidth));
      ValueCells.ForEach(cell => cell.GetComponent<LayoutElement>().SetPreferred(width: 200f));
    }

    public bool HasFocus() {
      GameObject selected = EventSystem.current.currentSelectedGameObject;

      return selected
          && Selectables.Any(s => s == selected)
          && (!selected.TryGetComponent(out InputField inputField)
              || inputField.isFocused
              || Input.GetKeyDown(KeyCode.Return));
    }

    public Minimap.PinData TargetPin { get; private set; }

    public void SetTargetPin(Minimap.PinData pin) {
      TargetPin = pin;

      if (pin == null) {
        return;
      }

      PinName.Value.InputField.text = pin.m_name;

      PinIconSelector.UpdateIcons(pin.m_type);
      PinType.Value.InputField.text = pin.m_type.ToString();

      PinChecked.Toggle.SetIsOnWithoutNotify(pin.m_checked);
      PinShared.Toggle.SetIsOnWithoutNotify(pin.m_ownerID != 0L);

      PinPosition.XValue.InputField.text = $"{pin.m_pos.x:F0}";
      PinPosition.YValue.InputField.text = $"{pin.m_pos.y:F0}";
      PinPosition.ZValue.InputField.text = $"{pin.m_pos.z:F0}";
    }

    void OnPinNameValueChange(string name) {
      if (TargetPin == null) {
        return;
      }

      TargetPin.m_name = name;
      TargetPin.m_nameElement.SetText(name);
    }

    void OnPinTypeValueChange(Minimap.PinType pinType) {
      if (TargetPin == null) {
        return;
      }

      TargetPin.m_type = pinType;
      TargetPin.m_icon = Minimap.m_instance.GetSprite(pinType);
      TargetPin.m_iconElement.SetSprite(TargetPin.m_icon);

      PinIconSelector.UpdateIcons(pinType);
      PinType.Value.InputField.text = pinType.ToString();
    }

    void OnPinCheckedChange(bool pinChecked) {
      if (TargetPin == null) {
        return;
      }

      TargetPin.m_checked = pinChecked;
      TargetPin.m_checkedElement.SetActive(pinChecked);
    }

    void OnPinSharedChange(bool pinShared) {
      if (TargetPin == null) {
        return;
      }

      TargetPin.m_ownerID = TargetPin.m_ownerID == 0L ? DefaultSharedPinOwnerId : 0L;
    }

    void OnPinPositionValueChange() {
      if (TargetPin == null) {
        return;
      }

      if (!float.TryParse(PinPosition.XValue.InputField.text, out float x)
          || !float.TryParse(PinPosition.YValue.InputField.text, out float y)
          || !float.TryParse(PinPosition.ZValue.InputField.text, out float z)) {
        PinPosition.XValue.InputField.text = $"{TargetPin.m_pos.x:F0}";
        PinPosition.YValue.InputField.text = $"{TargetPin.m_pos.y:F0}";
        PinPosition.ZValue.InputField.text = $"{TargetPin.m_pos.z:F0}";

        return;
      }

      TargetPin.m_pos = new(x, y, z);
      TargetPin.m_uiElement.SetPosition(GetMapImagePosition(TargetPin.m_pos));

      CenterMapHelper.CenterMapOnPosition(TargetPin.m_pos);
    }

    static Vector2 GetMapImagePosition(Vector3 mapPosition) {
      Minimap.m_instance.WorldToMapPoint(mapPosition, out float mx, out float my);
      return Minimap.m_instance.MapPointToLocalGuiPos(mx, my, Minimap.m_instance.m_mapImageLarge);
    }

    GameObject CreatePanel(Transform parentTransform) {
      GameObject panel = new("Panel", typeof(RectTransform));
      panel.SetParent(parentTransform);

      panel.AddComponent<VerticalLayoutGroup>()
          .SetChildControl(width: true, height: true)
          .SetChildForceExpand(width: false, height: false)
          .SetPadding(left: 2, right: 2, top: 8, bottom: 8)
          .SetSpacing(4f);

      panel.AddComponent<ContentSizeFitter>()
          .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
          .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);

      panel.AddComponent<Image>()
          .SetType(Image.Type.Sliced)
          .SetSprite(UIBuilder.CreateRoundedCornerSprite(128, 128, 16))
          .SetColor(new(0f, 0f, 0f, 0.9f));

      panel.AddComponent<CanvasGroup>()
          .SetBlocksRaycasts(true);

      return panel;
    }

    GameObject CreateChildRow(Transform parentTransform) {
      GameObject row = new("Row", typeof(RectTransform));
      row.SetParent(parentTransform);

      row.AddComponent<HorizontalLayoutGroup>()
          .SetChildControl(width: true, height: true)
          .SetChildForceExpand(width: false, height: false)
          .SetPadding(left: 8, right: 8, top: 2, bottom: 2)
          .SetSpacing(12f)
          .SetChildAlignment(TextAnchor.MiddleCenter);

      return row;
    }

    GameObject CreateChildLabel(Transform parentTransform) {
      GameObject label = UIBuilder.CreateLabel(parentTransform);
      label.SetName("Label");

      label.Text()
          .SetAlignment(TextAnchor.MiddleLeft)
          .SetText("Label");

      return label;
    }
  }
}
