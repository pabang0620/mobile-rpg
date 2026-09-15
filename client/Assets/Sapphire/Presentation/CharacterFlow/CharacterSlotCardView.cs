using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sapphire.Presentation.CharacterFlow
{
    /// <summary>
    /// The two sub-views one character-select slot card toggles between:
    /// "filled" (an existing character - portrait/name/class+level/선택/삭제)
    /// or "empty" (just a "생성" prompt). Plain [Serializable] data class
    /// (not a MonoBehaviour) so CharacterSelectController can hold a plain
    /// array of these, same "array of Editor-built UI refs" convention
    /// MainMenuPanel's Button[]/labels[] already uses, one level deeper.
    /// </summary>
    [Serializable]
    public sealed class CharacterSlotCardView
    {
        public GameObject filledRoot;
        public GameObject emptyRoot;
        public Image portraitImage;
        public Text nameText;
        public Text classLevelText;
        public Button selectButton;
        public Button deleteButton;
        public Button createButton;
    }
}
