using System.Collections.Generic;
using UnityEngine;

namespace Lighthaven2D
{
    // M2b AUTOHUNT_DECISION_TECH_SPEC. Pure decision logic only: reads a per-tick snapshot of
    // BattleSession state and returns an intent. BattleSession is the only thing that ever applies
    // an intent, and only through its own existing public methods (Attack/Dodge/Jump/UseHpPotion,
    // movement). This class never references UnityEngine.UI and never assigns a BattleSession field.
    public enum AutoHuntState { Suspended, Avoid, Return, Reposition, Engage, Approach, Searching }
    public enum AutoAttackAction { None, Basic, Skill }

    public struct AutoHuntEnemySnapshot
    {
        public int Id;
        public Vector2 Position;
        public int Hp, MaxHp;
        public bool Dead;
        public float Windup;
        public Vector2 Facing;
    }

    public struct AutoHuntSnapshot
    {
        public Vector2 HeroPosition, HeroFacing;
        public bool Grounded;
        public int Hp, MaxHp;
        public float Mp;
        public int HpPotions;
        public float Invulnerable, ShieldUntil, DodgeCooldown;
        public List<AutoHuntEnemySnapshot> Enemies;
        public float[] Cooldowns, MaxCooldowns;
        public int[] Costs;
        public float Time;
        public bool Auto;
        public float ManualUntil;
        public float MovableRangeMinX, MovableRangeMaxX;
        public bool ProgressSincePreviousDecision;
    }

    public struct AutoIntent
    {
        public int MoveDirection;
        public bool WantsDodge;
        public Vector2 DodgeAwayFrom;
        public AutoAttackAction Attack;
        public int SkillIndex;
        public int TargetId;
        public bool UseHpPotion;
        public string Reason;

        public static AutoIntent Idle(string reason) => new AutoIntent { MoveDirection = 0, Attack = AutoAttackAction.None, TargetId = -1, Reason = reason };
    }

    public struct AutoDebugState
    {
        public AutoHuntState State;
        public int TargetId;
        public float SecondsSinceProgress;
        public float LastHpPotionAt;
    }

    public sealed class AutoHuntDirector
    {
        public const float DecisionInterval = 0.2f;
        public const float ReacquireSeconds = 2.0f;
        public const float ReturnSeconds = 5.0f;
        public const float BlacklistSeconds = 4.0f;
        public const float AvoidRadius = 130f;
        public const float LowHpRatio = 0.35f;
        public const float SafeAnchorRadius = 24f;
        // Fixture threshold for BattleSession.HasProgressedSinceAutoBaseline: minimum horizontal
        // displacement (px) since the last layer-B decision to count as "progressed" (M2b tech spec).
        public const float ProgressDistanceThreshold = 4f;
        public static readonly Vector2 SafeAnchor = new Vector2(360f, BattleSession.GroundTop + BattleSession.HeroHalfHeight);

        int lockOnTargetId = -1;
        readonly Dictionary<int, float> blacklistUntil = new Dictionary<int, float>();
        float noProgressTimer;
        float lastDecisionTime = -1f;
        AutoIntent cachedIntent = AutoIntent.Idle("AUTO · 대기");
        AutoHuntState state = AutoHuntState.Searching;
        float lastHpPotionAt = -1f;

        public bool DecisionMadeThisEvaluate { get; private set; }
        public AutoIntent LastCachedIntent => cachedIntent;
        public AutoDebugState Debug => new AutoDebugState { State = state, TargetId = lockOnTargetId, SecondsSinceProgress = noProgressTimer, LastHpPotionAt = lastHpPotionAt };

        public void Reset()
        {
            lockOnTargetId = -1;
            blacklistUntil.Clear();
            noProgressTimer = 0;
            lastDecisionTime = -1f;
            cachedIntent = AutoIntent.Idle("AUTO · 대기");
            state = AutoHuntState.Searching;
            lastHpPotionAt = -1f;
            DecisionMadeThisEvaluate = false;
        }

        // Layer A (Suspended, Avoid) is recomputed every call, bypassing the 0.2s decision cadence
        // that governs layer B (Return/Reposition/Engage/Approach/Searching + target selection).
        public AutoIntent Evaluate(AutoHuntSnapshot s)
        {
            DecisionMadeThisEvaluate = false;
            bool autoActing = IsAutoActing(s);

            if (!autoActing)
            {
                state = AutoHuntState.Suspended;
                lastDecisionTime = -1f; // resuming re-decides immediately instead of waiting out a stale cadence
                cachedIntent = AutoIntent.Idle("AUTO · 대기");
                return cachedIntent;
            }

            var threat = FindThreat(s);
            if (threat.HasValue)
            {
                bool canDodge = s.DodgeCooldown <= 0;
                var away = s.HeroPosition - threat.Value.Position;
                int retreatDirection = away.x < 0 ? -1 : 1;
                state = AutoHuntState.Avoid;
                return new AutoIntent
                {
                    MoveDirection = canDodge ? 0 : retreatDirection,
                    WantsDodge = canDodge,
                    DodgeAwayFrom = threat.Value.Position,
                    Attack = AutoAttackAction.None,
                    TargetId = -1,
                    Reason = canDodge ? "AUTO · 위험 회피(구르기)" : "AUTO · 위험 회피(후퇴)"
                };
            }

            bool decisionDue = lastDecisionTime < 0 || s.Time - lastDecisionTime >= DecisionInterval;
            if (decisionDue)
            {
                float elapsed = lastDecisionTime < 0 ? DecisionInterval : s.Time - lastDecisionTime;
                cachedIntent = DecideLayerB(s, elapsed);
                lastDecisionTime = s.Time;
                DecisionMadeThisEvaluate = true;
            }
            return cachedIntent;
        }

        // Independent of the layer A/B cache: re-checked every call with the latest Hp so a potion
        // is never delayed by the 0.2s decision cadence (parent decision 4).
        public bool ShouldAutoUseHpPotion(AutoHuntSnapshot s)
        {
            if (!IsAutoActing(s) || s.HpPotions <= 0 || s.MaxHp <= 0) return false;
            if ((float)s.Hp / s.MaxHp >= LowHpRatio) return false;
            lastHpPotionAt = s.Time;
            return true;
        }

        static bool IsAutoActing(AutoHuntSnapshot s) => s.Auto && s.Time >= s.ManualUntil && s.Hp > 0;

        AutoHuntEnemySnapshot? FindThreat(AutoHuntSnapshot s)
        {
            if (s.Time < s.Invulnerable || s.Time < s.ShieldUntil) return null;
            foreach (var e in s.Enemies)
            {
                if (e.Dead || e.Windup <= 0) continue;
                if (Vector2.Distance(s.HeroPosition, e.Position) <= AvoidRadius) return e;
            }
            return null;
        }

        AutoIntent DecideLayerB(AutoHuntSnapshot s, float elapsedSinceLastDecision)
        {
            PruneBlacklist(s.Time);
            var selection = SelectTarget(s);

            if (selection.InRange && selection.Enemy.HasValue)
            {
                noProgressTimer = 0;
                state = AutoHuntState.Engage;
                return BuildEngageIntent(s, selection.Enemy.Value);
            }

            noProgressTimer = s.ProgressSincePreviousDecision ? 0 : noProgressTimer + elapsedSinceLastDecision;

            if (noProgressTimer >= ReturnSeconds)
            {
                state = AutoHuntState.Return;
                if (Vector2.Distance(s.HeroPosition, SafeAnchor) <= SafeAnchorRadius) noProgressTimer = 0;
                var diff = SafeAnchor - s.HeroPosition;
                return new AutoIntent
                {
                    MoveDirection = Mathf.Abs(diff.x) < .5f ? 0 : (diff.x < 0 ? -1 : 1),
                    Attack = AutoAttackAction.None,
                    TargetId = -1,
                    Reason = "AUTO · 복귀(무진행 " + noProgressTimer.ToString("0.0") + "s)"
                };
            }

            if (noProgressTimer >= ReacquireSeconds)
            {
                state = AutoHuntState.Reposition;
                if (lockOnTargetId >= 0) blacklistUntil[lockOnTargetId] = s.Time + BlacklistSeconds;
                lockOnTargetId = -1;
                var nearest = SelectNearestExcludingBlacklist(s);
                var intent = AutoIntent.Idle("AUTO · 재탐색(무진행 " + noProgressTimer.ToString("0.0") + "s)");
                if (nearest.HasValue)
                {
                    lockOnTargetId = nearest.Value.Id;
                    var diff = nearest.Value.Position - s.HeroPosition;
                    intent.MoveDirection = diff.x < 0 ? -1 : 1;
                    intent.TargetId = nearest.Value.Id;
                }
                return intent;
            }

            if (selection.Enemy.HasValue)
            {
                state = AutoHuntState.Approach;
                var diff = selection.Enemy.Value.Position - s.HeroPosition;
                return new AutoIntent
                {
                    MoveDirection = diff.x < 0 ? -1 : 1,
                    Attack = AutoAttackAction.None,
                    TargetId = selection.Enemy.Value.Id,
                    Reason = "AUTO · 접근"
                };
            }

            state = AutoHuntState.Searching;
            lockOnTargetId = -1;
            return AutoIntent.Idle("AUTO · 대상 탐색 중");
        }

        AutoIntent BuildEngageIntent(AutoHuntSnapshot s, AutoHuntEnemySnapshot target)
        {
            lockOnTargetId = target.Id;
            var intent = new AutoIntent { MoveDirection = 0, TargetId = target.Id };
            int nearbyAlive = 0;
            foreach (var e in s.Enemies) if (!e.Dead && Vector2.Distance(s.HeroPosition, e.Position) <= BattleSession.AoeGroupRadius) nearbyAlive++;
            if (nearbyAlive >= 2 && s.Cooldowns[1] <= 0 && s.Mp >= s.Costs[1])
            {
                intent.Attack = AutoAttackAction.Skill; intent.SkillIndex = 1; intent.Reason = "AUTO · 광역 스킬";
            }
            else if (s.Cooldowns[0] <= 0 && s.Mp >= s.Costs[0])
            {
                intent.Attack = AutoAttackAction.Skill; intent.SkillIndex = 0; intent.Reason = "AUTO · 마나 절약 · 스킬";
            }
            else
            {
                intent.Attack = AutoAttackAction.Basic; intent.Reason = "AUTO · 마나 절약 · 기본공격";
            }
            return intent;
        }

        struct TargetSelection { public AutoHuntEnemySnapshot? Enemy; public bool InRange; }

        // Target priority (parent decision 2): 1) retain lock-on while alive & in range,
        // 2) lowest HP in range (nearest breaks ties), 3) nearest alive excluding blacklist.
        TargetSelection SelectTarget(AutoHuntSnapshot s)
        {
            if (lockOnTargetId >= 0)
            {
                foreach (var e in s.Enemies)
                {
                    if (e.Id != lockOnTargetId || e.Dead) continue;
                    if (Vector2.Distance(s.HeroPosition, e.Position) <= BattleSession.AttackRange)
                        return new TargetSelection { Enemy = e, InRange = true };
                    break;
                }
            }
            lockOnTargetId = -1;

            AutoHuntEnemySnapshot? lowestHp = null; float lowestHpDistance = float.MaxValue;
            foreach (var e in s.Enemies)
            {
                if (e.Dead) continue;
                float distance = Vector2.Distance(s.HeroPosition, e.Position);
                if (distance > BattleSession.AttackRange) continue;
                if (lowestHp == null || e.Hp < lowestHp.Value.Hp || (e.Hp == lowestHp.Value.Hp && distance < lowestHpDistance))
                {
                    lowestHp = e; lowestHpDistance = distance;
                }
            }
            if (lowestHp.HasValue) { lockOnTargetId = lowestHp.Value.Id; return new TargetSelection { Enemy = lowestHp, InRange = true }; }

            var nearest = SelectNearestExcludingBlacklist(s);
            return new TargetSelection { Enemy = nearest, InRange = false };
        }

        AutoHuntEnemySnapshot? SelectNearestExcludingBlacklist(AutoHuntSnapshot s)
        {
            AutoHuntEnemySnapshot? best = null; float bestDistance = float.MaxValue;
            foreach (var e in s.Enemies)
            {
                if (e.Dead) continue;
                if (blacklistUntil.TryGetValue(e.Id, out var until) && until > s.Time) continue;
                float distance = Vector2.Distance(s.HeroPosition, e.Position);
                if (best == null || distance < bestDistance) { best = e; bestDistance = distance; }
            }
            return best;
        }

        void PruneBlacklist(float time)
        {
            if (blacklistUntil.Count == 0) return;
            List<int> expired = null;
            foreach (var pair in blacklistUntil) if (pair.Value <= time) (expired ??= new List<int>()).Add(pair.Key);
            if (expired == null) return;
            foreach (var id in expired) blacklistUntil.Remove(id);
        }
    }
}
