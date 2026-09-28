using UnityEngine;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Deterministic, sprite-independent anchor points for skill VFX, shared
    /// by SkillVfxPlayer (mage) and WarriorSkillVfxPlayer (warrior).
    ///
    /// 2026-09-28 (character/VFX position fix, docs/HANDOFF.md): VFX used to
    /// be anchored at SpriteRenderer.bounds.center of the caster (which
    /// changes per animation frame, causing visible jitter) plus a pile of
    /// hand-tuned per-effect magic-number offsets (VfxCenteringOffset,
    /// thunderOffset, shieldOffset, ...) that were re-tuned by eye against
    /// whatever the sprite bounds happened to be that day. None of that lined
    /// up reliably with SkillRangeIndicator, which draws its tile markers
    /// straight from GridWorldConversion.GridToWorld. Anchoring off the
    /// caster's GridCoord instead - the same source of truth the range
    /// indicator uses - makes ground-area VFX land exactly on the indicated
    /// tiles and removes the frame-to-frame jitter.
    /// </summary>
    public static class VfxAnchors
    {
        /// <summary>
        /// Extra lift applied only to a horizontal-facing (left/right)
        /// directional effect, so it reads at chest height instead of
        /// skimming along the ground - up/down facings already travel along
        /// the vertical tile-center line, which reads correctly with no lift.
        /// </summary>
        public const float ProjectileLift = 0.15f;

        /// <summary>Ground-area anchor: the exact tile center, matching
        /// SkillRangeIndicator's own markers (thunder field, whirlwind,
        /// war-cry shockwave, ground slam, teleport departure/arrival rings).</summary>
        public static Vector3 TileCenter(GridCoord cell)
        {
            WorldPoint p = GridWorldConversion.GridToWorld(cell);
            return new Vector3(p.X, p.Y, 0f);
        }

        /// <summary>Self-buff anchor: the caster's body center (feet + the
        /// measured body-center height), not the ground (mage ManaShield,
        /// warrior ShieldBlockBarrier).</summary>
        public static Vector3 BodyCenter(GridCoord casterCell)
        {
            WorldPoint feet = GridWorldConversion.GridToActorFeet(casterCell);
            return new Vector3(feet.X, feet.Y + GridWorldConversion.ActorBodyCenterHeight, 0f);
        }

        /// <summary>
        /// Start point for a directional effect that travels along the
        /// facing row/column (BasicAttackSlash, DashStreak, IceSpike
        /// projectile, LightningSpear): the caster tile's own edge in the
        /// facing direction, at tile-center height, plus <see cref="ProjectileLift"/>
        /// for a horizontal facing only.
        /// </summary>
        public static Vector3 DirectionalStart(GridCoord casterCell, GridDirection facing)
        {
            Vector3 center = TileCenter(casterCell);
            GridCoord offset = facing.ToOffset();
            float lift = offset.Y == 0 ? ProjectileLift : 0f;
            return new Vector3(
                center.x + offset.X * 0.5f,
                center.y + offset.Y * 0.5f + lift,
                0f);
        }
    }
}
