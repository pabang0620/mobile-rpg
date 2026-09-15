using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Small construction helpers shared by LoginUiBuilder/
    /// CharacterSelectUiBuilder/CharacterCreateUiBuilder - the same
    /// EventSystem+Canvas scaffold and a couple of generic
    /// sliced-panel/labeled-button builders every one of those three needs.
    /// Deliberately separate from VillageHubUiBuilder's private
    /// BuildEventSystem/BuildCanvas (same settings, duplicated here) rather
    /// than making those internal and reusing them, so nothing about
    /// VillageHub's own UI construction changes as a side effect of this
    /// character-flow work.
    /// </summary>
    internal static class CharacterFlowUiScaffold
    {
        internal static GameObject BuildEventSystemAndCanvas()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasGo;
        }

        internal static Image BuildFullScreenBackground(GameObject canvasGo, Sprite backgroundSprite)
        {
            var backgroundGo = new GameObject("Background", typeof(Image));
            backgroundGo.transform.SetParent(canvasGo.transform, false);
            backgroundGo.transform.SetAsFirstSibling();
            var rect = backgroundGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = backgroundGo.GetComponent<Image>();
            image.sprite = backgroundSprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A gold pill button (MenuButtonGold.png, 9-sliced) with a centered Korean-font label - the shape every "게임 시작"/"선택"/"삭제"/"생성" button in this flow shares.</summary>
        internal static Button BuildLabeledButton(GameObject parent, Sprite buttonSprite, string name, Vector2 anchoredPosition, Vector2 sizeDelta, string label, int fontSize = 26)
        {
            var buttonGo = new GameObject(name, typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent.transform, false);
            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            var image = buttonGo.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            Button button = buttonGo.GetComponent<Button>();

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(buttonGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = VillageHubUiBuilder.LoadKoreanFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = fontSize;
            text.text = label;

            return button;
        }

        /// <summary>A 9-sliced Image (e.g. CharacterSlotFrame/InputFieldFrame/MessagePanelFrameGold) sized/positioned as a plain background panel, no children.</summary>
        internal static Image BuildSlicedPanel(GameObject parent, Sprite sprite, string name, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var panelGo = new GameObject(name, typeof(Image));
            panelGo.transform.SetParent(parent.transform, false);
            var rect = panelGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            var image = panelGo.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            return image;
        }

        internal static Text BuildLabel(GameObject parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, string content, int fontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var textGo = new GameObject(name, typeof(Text));
            textGo.transform.SetParent(parent.transform, false);
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            var text = textGo.GetComponent<Text>();
            text.font = VillageHubUiBuilder.LoadKoreanFont();
            text.alignment = alignment;
            text.color = Color.white;
            text.fontSize = fontSize;
            text.text = content;
            return text;
        }
    }
}
