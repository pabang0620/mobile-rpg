using System;
using UnityEngine;
using UnityEngine.UI;
using Sapphire.Domain.Character;
using Sapphire.Presentation.Movement;
using Sapphire.Presentation.Skills;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Builds the right-side radial skill menu: a big center "기본공격"
    /// button surrounded by a single tight fan of all 5 skill buttons. Split
    /// out of <see cref="VillageHubUiBuilder"/> (same "keep files under ~500
    /// lines" split this codebase already applies elsewhere) - this file owns
    /// only the skill fan, the rest of the HUD stays in VillageHubUiBuilder.
    ///
    /// 2026-09-16 (F3 fix, orchestrator visual QA pass): the previous 4-fan +
    /// 1-outside-button layout (radius 200, arc 100-190deg plus a lone button
    /// at 220deg) spread the buttons across nearly half the screen height,
    /// reading as scattered rather than a single compact control cluster.
    /// Replaced with one 5-button fan, radius 172, spanning 80-200deg (120deg
    /// total, 4 gaps of 30deg) so all 5 SkillCatalog entries sit at a uniform,
    /// visibly tight distance from the basic-attack button - see the
    /// coordinate-math verification block below for the exact numbers.
    ///
    /// Character-flow slice: this fan is now built once per character class
    /// (Mage/Warrior), each with its own icon sheet and its own root
    /// GameObject ("RadialSkillMenu_Mage"/"RadialSkillMenu_Warrior") so
    /// SceneComposer can activate exactly one at runtime. The geometry
    /// (radius/arc/sizes) is identical for both classes - only the icon
    /// sheet path, SkillCatalog.ForClass selection and the built
    /// RadialSkillMenu's characterClass field differ.
    /// </summary>
    internal static class VillageHubSkillMenuBuilder
    {
        internal static GameObject Build(GameObject canvasGo, PlayerGridController playerController, SkillCastFeedback castFeedback, CharacterClass characterClass, string iconSheetFileName)
        {
            SkillDefinition[] skills = SkillCatalog.ForClass(characterClass);
            if (skills.Length != 5)
            {
                throw new Exception($"VillageHubSkillMenuBuilder's fan geometry (5 fan slots) assumes exactly 5 SkillCatalog entries, found {skills.Length} for {characterClass}.");
            }

            Sprite skillFrameSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/SkillButtonFrameGold.png", "SkillButtonFrame_Skill");
            Sprite basicAttackFrameSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/SkillButtonFrameGold.png", "SkillButtonFrame_BasicAttack");
            string iconSheetPath = SapphireSceneBuilder.UiArtDir + "/" + iconSheetFileName;

            var rootGo = new GameObject("RadialSkillMenu_" + characterClass, typeof(RectTransform));
            rootGo.transform.SetParent(canvasGo.transform, false);
            var rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0f);
            rootRect.anchorMax = new Vector2(1f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = Vector2.zero;
            // Distance from the canvas's bottom-right corner to the basic
            // attack button's center - see the coordinate-math verification
            // in this method's trailing comment block for why (-112, 118)
            // keeps every button on-screen at the 1280x720 reference
            // resolution.
            rootRect.anchoredPosition = new Vector2(-112f, 118f);

            const float basicAttackSize = 132f;
            const float skillButtonSize = 80f;

            // Frame "opening" (inner navy interior, not the cell boundary) as a
            // fraction of each frame's own cell size - measured via PIL: the
            // widest contiguous non-border-colored run across several rows near
            // each cell's vertical center. Skill cell (659x665): runs of
            // 377-429px, median 413 -> 413/659 = 0.627. BasicAttack cell
            // (824x854): runs of 488-556px, median 541 -> 541/824 = 0.657.
            const float skillOpeningFraction = 0.627f;
            const float basicAttackOpeningFraction = 0.657f;
            // Spec: icon diameter ~= 62% of the frame's inner opening diameter
            // (not 62% of the full button - the previous inset=size*0.2
            // convention sized icons at 60% of the FULL button, spilling onto
            // the frame's own ring art).
            const float iconToOpeningRatio = 0.62f;
            float skillIconSize = skillButtonSize * skillOpeningFraction * iconToOpeningRatio;
            float basicAttackIconSize = basicAttackSize * basicAttackOpeningFraction * iconToOpeningRatio;

            Sprite attackIcon = LoadSkillIcon(iconSheetPath, "SkillIcons_BasicAttack");
            Button attackButton = BuildRadialButton(rootGo, basicAttackFrameSprite, "AttackButton", Vector2.zero, basicAttackSize, attackIcon, basicAttackIconSize);

            // Fan: all 5 buttons, radius 172, arc 80deg-200deg (120deg span, 4
            // gaps of 30deg each - 0deg = screen-right, angles increase
            // counter-clockwise/upward). Adjacent center-to-center chord =
            // 2*172*sin(15deg) = 88.98 units > the 80-unit button diameter
            // (the distance two same-size circles need to just touch) for
            // every adjacent pair, a ~9-unit clearance gap - verified below,
            // not assumed. skills[0] sits at 80deg (nearest the top of the
            // fan) through skills[4] at 200deg (nearest the bottom), matching
            // the 1-5 key bindings in RadialSkillMenu.Update in that same order.
            const float skillRadius = 172f;
            const float fanArcStartDeg = 80f;
            const float fanArcEndDeg = 200f;
            const int fanCount = 5;
            var skillButtons = new Button[skills.Length];
            var buttonOffsets = new Vector2[skills.Length];

            for (int i = 0; i < fanCount; i++)
            {
                float t = i / (float)(fanCount - 1);
                float angleRad = Mathf.Lerp(fanArcStartDeg, fanArcEndDeg, t) * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * skillRadius;
                buttonOffsets[i] = offset;

                SkillDefinition skill = skills[i];
                Sprite iconSprite = LoadSkillIcon(iconSheetPath, skill.IconSpriteName);
                skillButtons[i] = BuildRadialButton(rootGo, skillFrameSprite, "SkillButton_" + i, offset, skillButtonSize, iconSprite, skillIconSize);
            }

            VerifyNoOverlap(buttonOffsets, skillButtonSize, basicAttackSize);
            VerifyOnScreen(rootRect.anchoredPosition, buttonOffsets, skillButtonSize, basicAttackSize);

            BuildSkillSystems(attackButton, skillButtons, playerController, castFeedback, characterClass);

            return rootGo;
        }

        // Coordinate-math verification (REMEDIATION_PLAN.md Phase 5 principle:
        // measure what actually renders, don't assume geometry is correct) -
        // every adjacent pair of the 5 fan buttons must be farther apart than
        // the 80-unit touching distance for two same-size circles, and every
        // button must be farther from the basic-attack center than half the
        // sum of their diameters (106 units: 66 + 40).
        private static void VerifyNoOverlap(Vector2[] offsets, float skillButtonSize, float basicAttackSize)
        {
            float minTouchingDistance = skillButtonSize; // two skillButtonSize-diameter circles
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                float distance = Vector2.Distance(offsets[i], offsets[i + 1]);
                if (distance <= minTouchingDistance)
                {
                    throw new Exception($"Skill buttons {i} and {i + 1} are only {distance} units apart, at or under the {minTouchingDistance}-unit touching distance.");
                }
            }

            float attackClearance = basicAttackSize * 0.5f + skillButtonSize * 0.5f;
            foreach (Vector2 offset in offsets)
            {
                float distanceFromAttack = offset.magnitude;
                if (distanceFromAttack <= attackClearance)
                {
                    throw new Exception($"A skill button sits {distanceFromAttack} units from the basic-attack button, at or under the {attackClearance}-unit clearance the two buttons' sizes require.");
                }
            }
        }

        // Every button (basic attack + the 5 fan buttons) must stay fully
        // inside the 1280x720 reference canvas given the root's bottom-right-
        // corner anchoredPosition.
        private static void VerifyOnScreen(Vector2 rootAnchoredPosition, Vector2[] fanOffsets, float skillButtonSize, float basicAttackSize)
        {
            const float canvasWidth = 1280f;
            const float canvasHeight = 720f;
            float basicAttackHalf = basicAttackSize * 0.5f;
            float skillHalf = skillButtonSize * 0.5f;

            float rootAbsoluteX = canvasWidth + rootAnchoredPosition.x;
            float rootAbsoluteY = rootAnchoredPosition.y;

            CheckBounds("AttackButton", rootAbsoluteX, rootAbsoluteY, basicAttackHalf, canvasWidth, canvasHeight);
            for (int i = 0; i < fanOffsets.Length; i++)
            {
                CheckBounds("SkillButton_" + i, rootAbsoluteX + fanOffsets[i].x, rootAbsoluteY + fanOffsets[i].y, skillHalf, canvasWidth, canvasHeight);
            }
        }

        private static void CheckBounds(string name, float centerX, float centerY, float half, float canvasWidth, float canvasHeight)
        {
            if (centerX - half < 0f || centerX + half > canvasWidth || centerY - half < 0f || centerY + half > canvasHeight)
            {
                throw new Exception($"{name} at ({centerX},{centerY}) with half-size {half} falls outside the {canvasWidth}x{canvasHeight} reference canvas.");
            }
        }

        // All 6 skill icons (basic attack + the 5 SkillCatalog entries) live in
        // a single 6-cell sheet, one sheet per class (SkillIconsSetGold.png
        // for Mage, WarriorSkillIconsSetGold.png for Warrior) - see
        // WarriorArtImportConfigurator for the warrior sheet's slice names.
        private static Sprite LoadSkillIcon(string sheetPath, string spriteName)
        {
            return VillageHubUiBuilder.LoadNamedSprite(sheetPath, spriteName);
        }

        // 2026-09-16: keyHint parameter removed entirely (REMEDIATION_PLAN.md
        // Phase 2 item 6 - "1/2/3/4/5/J" on-screen labels are gone; the keyboard
        // shortcuts themselves are untouched in RadialSkillMenu.Update, only the
        // visible hint Text GameObjects are deleted here). iconSize is now
        // passed in explicitly (computed by the caller from the measured frame
        // opening) instead of a constant size*0.2 inset fraction of the whole
        // button - see Build above. The old labelText fallback branch (used
        // when iconSprite was null) is also removed: every caller in this file
        // always supplies a resolved icon sprite (LoadSkillIcon throws if one
        // is missing), so that branch was dead code.
        private static Button BuildRadialButton(GameObject parent, Sprite circleSprite, string name, Vector2 anchoredPosition, float buttonSize, Sprite iconSprite, float iconSize)
        {
            var buttonGo = new GameObject(name, typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent.transform, false);
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(buttonSize, buttonSize);
            buttonRect.anchoredPosition = anchoredPosition;
            var buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.sprite = circleSprite;
            Button button = buttonGo.GetComponent<Button>();

            var iconGo = new GameObject("Icon", typeof(Image));
            iconGo.transform.SetParent(buttonGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            iconRect.anchoredPosition = Vector2.zero;
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            return button;
        }

        private static void BuildSkillSystems(Button attackButton, Button[] skillButtons, PlayerGridController playerController, SkillCastFeedback castFeedback, CharacterClass characterClass)
        {
            var skillSystemsGo = new GameObject("SkillSystems_" + characterClass);
            var radialSkillMenu = skillSystemsGo.AddComponent<RadialSkillMenu>();
            // Reuses VillageHubUiBuilder's AssignField (code-reviewer flagged
            // this file's own copy as a duplicated reflection helper - see that
            // method's doc comment) instead of a local copy.
            VillageHubUiBuilder.AssignField(radialSkillMenu, "basicAttackButton", attackButton);
            VillageHubUiBuilder.AssignField(radialSkillMenu, "skillButtons", skillButtons);
            VillageHubUiBuilder.AssignField(radialSkillMenu, "player", playerController);
            // RangeTile markers are intentionally disabled: casts show only
            // the authored VFX, never cyan checkbox/grid overlays.
            VillageHubUiBuilder.AssignField(radialSkillMenu, "rangeIndicator", null);
            VillageHubUiBuilder.AssignField(radialSkillMenu, "castFeedback", castFeedback);
            VillageHubUiBuilder.AssignField(radialSkillMenu, "characterClass", characterClass);
        }
    }
}
