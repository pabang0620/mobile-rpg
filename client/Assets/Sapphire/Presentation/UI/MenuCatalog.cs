namespace Sapphire.Presentation.UI
{
    /// <summary>One clickable slot in the Odin-style menu grid.</summary>
    public readonly struct MenuItemDefinition
    {
        public readonly string Id;
        public readonly string Label;
        public readonly string IconSpriteName;
        public readonly bool IsAvailable;

        public MenuItemDefinition(string id, string label, string iconSpriteName, bool isAvailable)
        {
            Id = id;
            Label = label;
            IconSpriteName = iconSpriteName;
            IsAvailable = isAvailable;
        }
    }

    /// <summary>One section header + its row of items in the Odin-style menu.</summary>
    public readonly struct MenuSectionDefinition
    {
        public readonly string Title;
        public readonly MenuItemDefinition[] Items;

        public MenuSectionDefinition(string title, MenuItemDefinition[] items)
        {
            Title = title;
            Items = items;
        }
    }

    /// <summary>
    /// Static data source for the Odin-style right-side menu panel
    /// (2026-09-16 Phase 3, REMEDIATION_PLAN.md D2(b) - replaces the old flat
    /// 7-item text-list MainMenuPanel that violated docs/planning/01_PRODUCT.md
    /// line 52 ("메뉴는 가방/장비/퀘스트/설정 4개만 실제 제공, 미구현 레이드
    /// 버튼 금지")).
    ///
    /// Only 4 items are IsAvailable (장비/가방/퀘스트/설정, matching the SSOT
    /// exactly); the other 4 are locked placeholders that show the finished
    /// grid shape without wiring a button to a system that doesn't exist yet -
    /// see VillageHubUiBuilder.BuildOdinMenu for how IsAvailable drives the
    /// gray tint / lock badge / Button.interactable=false.
    ///
    /// IconSpriteName values are the sprite names sliced out of
    /// MenuIconsSet.png by ArtImportConfigurator.ConfigureMenuIconsSet - adding
    /// a new item here only requires that sprite to already exist in that
    /// sheet (or a new one to be imported); this class and the builder that
    /// reads it need no further changes to add/remove/reorder items.
    /// </summary>
    public static class MenuCatalog
    {
        public static readonly MenuSectionDefinition[] Sections =
        {
            new MenuSectionDefinition("성장", new[]
            {
                new MenuItemDefinition("equipment", "장비", "MenuIcons_Equipment", true),
                new MenuItemDefinition("bag", "가방", "MenuIcons_Bag", true),
                new MenuItemDefinition("skillbook", "스킬북", "MenuIcons_SkillBook", false),
                new MenuItemDefinition("character_info", "캐릭터정보", "MenuIcons_CharacterInfo", false),
            }),
            new MenuSectionDefinition("모험", new[]
            {
                new MenuItemDefinition("quest", "퀘스트", "MenuIcons_Quest", true),
                new MenuItemDefinition("map", "지도", "MenuIcons_Map", false),
                new MenuItemDefinition("dungeon", "던전", "MenuIcons_Dungeon", false),
            }),
            new MenuSectionDefinition("시스템", new[]
            {
                new MenuItemDefinition("settings", "설정", "MenuIcons_Settings", true),
            }),
        };
    }
}
