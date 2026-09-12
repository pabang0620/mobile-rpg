using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lighthaven2D
{
    // Fixture balance for the asset integration slice, deliberately not the live economy.
    public sealed class BattleSession
    {
        // Calibrated against the visible top faces in SapphirePlatformMap.png at 1280x720.
        public const float GroundTop = 214f;
        public const float HeroHalfHeight = 90f;
        public readonly struct PlatformSurface
        {
            public readonly float MinX, MaxX, Top;
            public PlatformSurface(float minX, float maxX, float top) { MinX = minX; MaxX = maxX; Top = top; }
            public bool Contains(float x) => x >= MinX && x <= MaxX;
        }
        public sealed class Enemy
        {
            public int Id, Hp = 80, MaxHp = 80;
            public Vector2 Position, Facing = Vector2.left, Spawn;
            public float Windup, Recover, Respawn, Flash;
            public bool Dead => Hp <= 0;
        }
        public struct Impact { public Vector2 Position; public int Amount; public bool Hero; }
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Impact> Impacts = new List<Impact>();
        public readonly float[] Cooldowns = new float[4];
        public readonly int[] Costs = { 12, 24, 16, 22 };
        public readonly float[] MaxCooldowns = { 2.8f, 5.5f, 4.0f, 7.0f };
        public readonly PlatformSurface[] Platforms =
        {
            new PlatformSurface(320, 540, 414),
            new PlatformSurface(615, 840, 473),
            new PlatformSurface(895, 1118, 395)
        };
        public Vector2 Hero = new Vector2(360, GroundTop + HeroHalfHeight), Facing = Vector2.right;
        public float Time, Mp = 100, ManualUntil, AttackAge = -1, Invulnerable, ShieldUntil, DodgeCooldown, VerticalVelocity, SupportTop = GroundTop;
        public int Hp = 150, MaxHp = 150, HpPotions = 10, MpPotions = 10, Kills, Xp, Gold, Level = 1, CastSkill = -1, DamageEvents;
        public bool Auto = true, Closed, Grounded = true;
        public bool Dead => Hp <= 0;
        public bool AutoActing => Auto && Time >= ManualUntil && !Dead;
        public bool Attacking => AttackAge >= 0;
        public string LastAction = "달빛 회랑에 입장했습니다";
        Enemy target; bool hit; Vector2 castFacing, dodgeDirection; float dodgeUntil;
        public BattleSession()
        {
            for (int i = 0; i < 3; i++) { var p = new Vector2(800 + i * 150, GroundTop + 70); Enemies.Add(new Enemy { Id = i, Position = p, Spawn = p }); }
        }
        public Enemy Nearest()
        {
            Enemy best = null; float distance = float.MaxValue;
            foreach (var e in Enemies) if (!e.Dead && Vector2.Distance(Hero, e.Position) < distance) { best = e; distance = Vector2.Distance(Hero, e.Position); }
            return best;
        }
        public void Manual() { if (Auto) ManualUntil = Time + 3; }
        public void ToggleAuto() { Auto = !Auto; ManualUntil = Time; LastAction = Auto ? "자동사냥 시작" : "자동사냥 종료"; }
        public bool Attack(int skill = -1, bool manual = true)
        {
            if (Closed || Dead || Attacking || Time < dodgeUntil || skill < -1 || skill > 3) return false;
            if (skill >= 0 && (Cooldowns[skill] > 0 || Mp < Costs[skill])) { LastAction = Mp < Costs[skill] ? "마나가 부족합니다" : "스킬 재사용 대기 중"; return false; }
            var e = Nearest();
            if (skill < 2 && (e == null || Vector2.Distance(Hero, e.Position) > 390)) { LastAction = "적에게 더 가까이 이동하세요"; return false; }
            if (manual) Manual();
            if (skill >= 0) { Mp -= Costs[skill]; Cooldowns[skill] = MaxCooldowns[skill]; }
            if (skill == 2) { dodgeDirection = Horizontal(Facing); Hero = ClampHorizontal(Hero + dodgeDirection * 165); Invulnerable = Time + .5f; LastAction = "점멸"; return true; }
            if (skill == 3) { ShieldUntil = Time + 3.5f; LastAction = "마나 보호막"; return true; }
            target = e; castFacing = (e.Position - Hero).normalized; Facing = castFacing;
            CastSkill = skill; AttackAge = 0; hit = false;
            LastAction = skill == 0 ? "비전 화살" : skill == 1 ? "수정 파동" : "지팡이 공격";
            return true;
        }
        public bool Dodge(Vector2 input)
        {
            if (Closed || Dead || DodgeCooldown > 0) return false;
            Manual(); AttackAge = -1; target = null;
            dodgeDirection = Horizontal(Mathf.Abs(input.x) > .01f ? input : Facing);
            dodgeUntil = Time + .22f; Invulnerable = Time + .42f; DodgeCooldown = 2;
            LastAction = "회피"; return true;
        }
        public bool Jump()
        {
            if (Closed || Dead || !Grounded || Attacking) return false;
            Manual(); Grounded = false; VerticalVelocity = 720; SupportTop = -1;
            LastAction = "점프"; return true;
        }
        public bool UseHpPotion()
        {
            if (Closed || Dead || HpPotions <= 0 || Hp >= MaxHp) return false;
            HpPotions--; Hp = Mathf.Min(MaxHp, Hp + 60); Manual(); LastAction = "체력 물약"; return true;
        }
        public bool UseMpPotion()
        {
            if (Closed || Dead || MpPotions <= 0 || Mp >= 100) return false;
            MpPotions--; Mp = Mathf.Min(100, Mp + 45); Manual(); LastAction = "마나 물약"; return true;
        }
        public void Tick(float delta, Vector2 input)
        {
            if (Closed) return;
            float dt = Mathf.Clamp(delta, 0, .05f); Time += dt;
            for (int i = 0; i < 4; i++) Cooldowns[i] = Mathf.Max(0, Cooldowns[i] - dt);
            DodgeCooldown = Mathf.Max(0, DodgeCooldown - dt);
            if (Dead) return;
            Mp = Mathf.Min(100, Mp + dt * 7);
            input = new Vector2(Mathf.Clamp(input.x, -1, 1), 0);
            if (Mathf.Abs(input.x) > .01f) Manual();
            if (Time < dodgeUntil) Hero = ClampHorizontal(Hero + dodgeDirection * (dt * 550));
            else if (!Attacking)
            {
                var move = input;
                var nearest = Nearest();
                if (AutoActing && nearest != null)
                {
                    var difference = nearest.Position - Hero;
                    if (Mathf.Abs(difference.x) > 255 || Mathf.Abs(difference.y) > 110) move = new Vector2(Mathf.Sign(difference.x), 0);
                    else if (CanAutoCast(1) && Enemies.FindAll(e => !e.Dead && Vector2.Distance(Hero, e.Position) <= 410).Count >= 2) Attack(1, false);
                    else if (CanAutoCast(0)) Attack(0, false);
                    else Attack(-1, false);
                }
                if (Mathf.Abs(move.x) > .01f) { Facing = Horizontal(move); Hero = ClampHorizontal(Hero + Facing * (dt * 150)); }
            }
            StepVertical(dt);
            if (Attacking)
            {
                AttackAge += dt;
                if (!hit && AttackAge >= .36f)
                {
                    hit = true;
                    if (CastSkill == 1)
                    {
                        foreach (var e in Enemies) if (CanHit(e, 410)) Hurt(e, 52);
                    }
                    else if (target != null && CanHit(target, 420)) Hurt(target, CastSkill == 0 ? 60 : 30 + Level * 2);
                }
                if (AttackAge >= .94f) { AttackAge = -1; target = null; }
            }
            foreach (var e in Enemies)
            {
                e.Flash = Mathf.Max(0, e.Flash - dt);
                if (e.Dead)
                {
                    e.Respawn -= dt;
                    if (e.Respawn <= 0) { e.Hp = e.MaxHp; e.Position = e.Spawn; e.Windup = e.Recover = 0; }
                    continue;
                }
                if (e.Windup > 0)
                {
                    e.Windup -= dt;
                    if (e.Windup <= 0)
                    {
                        var d = Hero - e.Position;
                        if (d.magnitude <= 110 && (d.sqrMagnitude < .001f || Vector2.Dot(d.normalized, e.Facing) >= .35f) && Time >= Invulnerable)
                        {
                            int damage = Time < ShieldUntil ? 3 : 11; Hp = Mathf.Max(0, Hp - damage); DamageEvents++;
                            Impacts.Add(new Impact { Position = Hero, Amount = damage, Hero = true });
                            if (Dead) { AttackAge = -1; LastAction = "쓰러졌습니다 · 다시 도전할 수 있습니다"; break; }
                        }
                        e.Recover = .7f;
                    }
                }
                else if (e.Recover > 0) e.Recover -= dt;
                else
                {
                    var d = Hero - e.Position;
                    if (Mathf.Abs(d.x) > 88) { e.Facing = new Vector2(Mathf.Sign(d.x),0); e.Position = ClampHorizontal(e.Position + e.Facing * (dt * 60)); }
                    else { e.Facing = new Vector2(Mathf.Sign(d.x),0); e.Windup = .9f; }
                }
            }
        }
        void StepVertical(float dt)
        {
            if (Grounded && SupportTop > GroundTop && !HasSupport(Hero.x, SupportTop)) { Grounded = false; VerticalVelocity = 0; SupportTop = -1; }
            if (Grounded) { Hero.y = SupportTop + HeroHalfHeight; return; }
            var previousFoot = Hero.y - HeroHalfHeight;
            VerticalVelocity -= 1000 * dt;
            Hero.y += VerticalVelocity * dt;
            var currentFoot = Hero.y - HeroHalfHeight;
            if (VerticalVelocity > 0) return;
            var landing = float.MinValue;
            if (previousFoot >= GroundTop && currentFoot <= GroundTop) landing = GroundTop;
            foreach (var platform in Platforms)
                if (platform.Contains(Hero.x) && previousFoot >= platform.Top && currentFoot <= platform.Top)
                    landing = Mathf.Max(landing, platform.Top);
            if (landing == float.MinValue) return;
            SupportTop = landing; Hero.y = landing + HeroHalfHeight; VerticalVelocity = 0; Grounded = true;
        }
        bool HasSupport(float x, float top)
        {
            foreach (var platform in Platforms) if (Mathf.Abs(platform.Top - top) < .1f && platform.Contains(x)) return true;
            return top == GroundTop;
        }
        bool CanHit(Enemy e, float range) => !e.Dead && Vector2.Distance(Hero, e.Position) <= range && Vector2.Dot((e.Position - Hero).normalized, castFacing) >= .25f;
        bool CanAutoCast(int skill) => Cooldowns[skill] <= 0 && Mp >= Costs[skill];
        void Hurt(Enemy e, int damage)
        {
            if (e.Dead) return;
            e.Hp = Mathf.Max(0, e.Hp - damage); e.Flash = .13f;
            Impacts.Add(new Impact { Position = e.Position, Amount = damage });
            if (e.Dead) { e.Windup = 0; e.Respawn = 3.4f; Kills++; Xp += 25; Gold += 12; if (Xp >= Level * 100) { Xp -= Level * 100; Level++; Hp = MaxHp; LastAction = "LEVEL UP · 체력 회복"; } }
        }
        public void Close() { Closed = true; AttackAge = -1; target = null; Impacts.Clear(); }
        static Vector2 Horizontal(Vector2 value) => new Vector2(value.x < 0 ? -1 : 1, 0);
        static Vector2 ClampHorizontal(Vector2 p) => new Vector2(Mathf.Clamp(p.x, 75, 1205), p.y);
    }
}
