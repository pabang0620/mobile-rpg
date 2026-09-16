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
        // 2026-09-16 (premium select/create/login rebuild): the card's own
        // background Image must switch sprite between CharacterSlotFrameV2
        // (filled - has the top badge notch + bottom divider baked in) and
        // CharacterSlotFrameEmptyV2 (empty - plain dashed outline, no
        // notch/divider) per-slot, not just toggle filledRoot/emptyRoot's
        // children visibility as before (both variants used to share one
        // beige CharacterSlotFrame.png background).
        public Image cardBackground;
        public GameObject filledRoot;
        public GameObject emptyRoot;
        public Image classBadgeImage;
        public Image portraitImage;
        public Text nameText;
        public Text classLevelText;
        public Button selectButton;
        public Button deleteButton;
        public Button createButton;
    }
}
