using System;
using UnityEngine;
using UnityEngine.UI;
using Sapphire.Presentation.Movement;
using Sapphire.Presentation.Skills;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Builds the right-side radial skill menu: a big center "기본공격"
    /// button, a fan of exactly 4 skill buttons around it, and a 5th skill
    /// button outside the fan to its left. Split out of
    /// <see cref="VillageHubUiBuilder"/> (same "keep files under ~500 lines"
    /// split this codebase already applies elsewhere) - this file owns only
    /// the skill fan, the rest of the HUD stays in VillageHubUiBuilder.
    ///
    /// 2026-09-16 (REMEDIATION_PLAN.md Phase 2 item 5, content-mismatch note):
    /// the plan's "arcane bolt / frost wave / blink / shield" naming and a
    /// literal movement-speed "dash" for the 5th slot describe an OLDER
    /// SkillCatalog (docs/HANDOFF.md's most recent entries still reflect it)
    /// that commit 9d2a71d ("Implement centered wizard skills and menu UI",
    /// same day, earlier than this change) already replaced with 5
    /// differently-named real spells (마력쉴드/텔레포트/낙뢰/고드름/번개창) and
    /// no movement-speed skill at all - GridMoveAnimator.ActivateSpeedBoost/
    /// IsSpeedBoosted are still present but have been orphaned (uncalled)
    /// since that commit. Rather than resurrect a "dash" ability the current
    /// design no longer has, or delete one of the 5 real, already-wired
    /// spells to force-fit the plan's literal wording, this keeps all 5 real
    /// spells reachable exactly as before (same key bindings 1-5, same
    /// CastSkill(index) calls) and satisfies the plan's LAYOUT requirement
    /// instead: SkillCatalog.All[0..3] sit in the verified 4-button fan, and
    /// SkillCatalog.All[4] (번개창) takes the "outside the fan, to the left of
    /// basic attack" slot the plan calls "Dash". See docs/DECISIONS.md for
    /// the same note.
    /// </summary>
    internal static class VillageHubSkillMenuBuilder
    {
        internal static void Build(GameObject canvasGo, PlayerGridController playerController, SkillCastFeedback castFeedback)
        {
            if (SkillCatalog.All.Length != 5)
            {
                throw new Exception($"VillageHubSkillMenuBuilder's fan geometry (4 fan slots + 1 outside slot) assumes exactly 5 SkillCatalog entries, found {SkillCatalog.All.Length}.");
            }

            Sprite skillFrameSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/SkillButtonFrameGold.png", "SkillButtonFrame_Skill");
            Sprite basicAttackFrameSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/SkillButtonFrameGold.png", "SkillButtonFrame_BasicAttack");

            var rootGo = new GameObject("RadialSkillMenu", typeof(RectTransform));
            rootGo.transform.SetParent(canvasGo.transform, false);
            var rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0f);
            rootRect.anchorMax = new Vector2(1f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = Vector2.zero;
            // Distance from the canvas's bottom-right corner - see the
            // coordinate-math verification in this method's trailing comment
            // block for why (-100, 200) keeps every button on-screen at the
            // 1280x720 reference resolution.
            rootRect.anchoredPosition = new Vector2(-100f, 200f);

            const float basicAttackSize = 140f;
            const float skillButtonSize = 96f;

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
            float skillIconSize = skillButtonSize * skillOpeningFraction * iconToOpeningRatio; // ~37.3
            float basicAttackIconSize = basicAttackSize * basicAttackOpeningFraction * iconToOpeningRatio; // ~57.0

            Sprite attackIcon = LoadSkillIcon("SkillIcons_BasicAttack");
            Button attackButton = BuildRadialButton(rootGo, basicAttackFrameSprite, "AttackButton", Vector2.zero, basicAttackSize, attackIcon, basicAttackIconSize);

            // Fan: exactly 4 buttons, radius 200, arc 100deg-190deg (90deg span,
            // 3 gaps of 30deg). Adjacent center-to-center chord =
            // 2*200*sin(15deg) = 103.53 units > 96-unit button diameter (the
            // distance two same-size circles need to just touch) for every
            // adjacent pair - verified below, not assumed.
            const float skillRadius = 200f;
            const float fanArcStartDeg = 100f;
            const float fanArcEndDeg = 190f;
            const int fanCount = 4;
            var skillButtons = new Button[SkillCatalog.All.Length];
            var buttonOffsets = new Vector2[SkillCatalog.All.Length];

            for (int i = 0; i < fanCount; i++)
            {
                float t = i / (float)(fanCount - 1);
                float angleRad = Mathf.Lerp(fanArcStartDeg, fanArcEndDeg, t) * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * skillRadius;
                buttonOffsets[i] = offset;

                SkillDefinition skill = SkillCatalog.All[i];
                Sprite iconSprite = LoadSkillIcon(skill.IconSpriteName);
                skillButtons[i] = BuildRadialButton(rootGo, skillFrameSprite, "SkillButton_" + i, offset, skillButtonSize, iconSprite, skillIconSize);
            }

            // 5th slot, outside the fan (plan's "Dash" position - see the class-
            // level note above for why SkillCatalog.All[4] sits here instead of
            // a movement-speed ability): one more 30deg step past the fan's
            // 190deg edge, same radius, so its distance to both the fan's
            // nearest button (190deg) and to the basic-attack button stays
            // provably non-overlapping using the exact same chord formula as
            // the fan itself.
            const float outsideAngleDeg = fanArcEndDeg + 30f; // 220deg
            {
                float angleRad = outsideAngleDeg * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * skillRadius;
                buttonOffsets[4] = offset;

                SkillDefinition skill = SkillCatalog.All[4];
                Sprite iconSprite = LoadSkillIcon(skill.IconSpriteName);
                skillButtons[4] = BuildRadialButton(rootGo, skillFrameSprite, "SkillButton_4", offset, skillButtonSize, iconSprite, skillIconSize);
            }

            VerifyNoOverlap(buttonOffsets, skillButtonSize, basicAttackSize);
            VerifyOnScreen(rootRect.anchoredPosition, buttonOffsets, skillButtonSize, basicAttackSize);

            BuildSkillSystems(attackButton, skillButtons, playerController, castFeedback);
        }

        // Coordinate-math verification (REMEDIATION_PLAN.md Phase 5 principle:
        // measure what actually renders, don't assume geometry is correct) -
        // every adjacent pair (the 4 fan buttons in arc order, plus the outside
        // 5th button as a 5th arc step) must be farther apart than the 96-unit
        // touching distance for two same-size circles, and every button must be
        // farther from the basic-attack center than half the sum of their
        // diameters (118 units: 70 + 48).
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

        // Every button (basic attack, the 4 fan buttons, the outside button)
        // must stay fully inside the 1280x720 reference canvas given the root's
        // bottom-right-corner anchoredPosition.
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
        // the single SkillIconsSetGold.png sheet - sprite names unchanged from
        // previous passes.
        private static Sprite LoadSkillIcon(string spriteName)
        {
            return VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/SkillIconsSetGold.png", spriteName);
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

        private static void BuildSkillSystems(Button attackButton, Button[] skillButtons, PlayerGridController playerController, SkillCastFeedback castFeedback)
        {
            var skillSystemsGo = new GameObject("SkillSystems");
            var rangeIndicator = skillSystemsGo.AddComponent<SkillRangeIndicator>();
            var radialSkillMenu = skillSystemsGo.AddComponent<RadialSkillMenu>();
            // Reuses VillageHubUiBuilder's AssignField (code-reviewer flagged
            // this file's own copy as a duplicated reflection helper - see that
            // method's doc comment) instead of a local copy.
            VillageHubUiBuilder.AssignField(radialSkillMenu, "basicAttackButton", attackButton);
            VillageHubUiBuilder.AssignField(radialSkillMenu, "skillButtons", skillButtons);
            VillageHubUiBuilder.AssignField(radialSkillMenu, "player", playerController);
            VillageHubUiBuilder.AssignField(radialSkillMenu, "rangeIndicator", rangeIndicator);
            VillageHubUiBuilder.AssignField(radialSkillMenu, "castFeedback", castFeedback);
        }
    }
}
