using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Sapphire.Domain.Grid;
using Sapphire.Presentation.Combat;
using Sapphire.Presentation.Movement;
using Sapphire.Presentation.World;

namespace Sapphire.EditorTools
{
    [InitializeOnLoad]
    public static class SlimeKingdomPlaySmoke
    {
        const string Pending = "Sapphire.SlimeKingdom.SmokePending";
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        static double started;

        static SlimeKingdomPlaySmoke()
        {
            if (SessionState.GetBool(Pending, false)) Attach();
        }

        // Batch entry uses no -quit; completion below owns the exit code.
        public static void Run()
        {
            if (!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Smoke check is batch-only.");
            EditorSceneManager.OpenScene("Assets/Sapphire/Scenes/SlimeKingdom.unity");
            SessionState.SetBool(Pending, true);
            Attach();
            EditorApplication.EnterPlaymode();
        }

        static void Attach()
        {
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Pending, false)) { EditorApplication.update -= Tick; return; }
            if (EditorApplication.timeSinceStartup - started > 60) { Finish(false, "Timed out entering PlayMode."); return; }
            if (!EditorApplication.isPlaying || Time.timeSinceLevelLoad < .65f) return;
            try
            {
                var player = UnityEngine.Object.FindObjectsByType<PlayerGridController>(FindObjectsSortMode.None).Single(p => p.isActiveAndEnabled);
                if (player.Mover == null) throw new InvalidOperationException("Active player has no initialized GridMover.");
                var builder = UnityEngine.Object.FindAnyObjectByType<TilemapGridMapBuilder>();
                GridMap grid = builder.Build();
                GridMap playerMap = (GridMap)typeof(PlayerGridController).GetField("map", PrivateInstance).GetValue(player);
                if (!ReferenceEquals(grid, playerMap)) throw new InvalidOperationException("Player and monsters do not share occupancy.");
                var monsters = UnityEngine.Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);
                if (monsters.Length < 4 || !monsters.Any(m => m.IsBoss)) throw new InvalidOperationException("Encounters/boss missing at runtime.");
                foreach (var monster in monsters)
                {
                    if (monster.Health == null || monster.Stats == null || monster.Health.IsDead)
                        throw new InvalidOperationException("Monster initialization failed: " + monster.name);
                    if (monster.GetComponentsInChildren<MonsterHpBar>(true).Length != 1)
                        throw new InvalidOperationException("Duplicate/missing HP bar: " + monster.name);
                }
                var foam = UnityEngine.Object.FindAnyObjectByType<ModularFoamAnimator>();
                if (foam == null) throw new InvalidOperationException("Shoreline animator is missing.");
                // Frame zero is also valid after a complete cycle; observe the next
                // nonzero frame instead of failing because the editor tick arrived late.
                if ((int)typeof(ModularFoamAnimator).GetField("frame", PrivateInstance).GetValue(foam) == 0) return;

                var subject = monsters.First(m => !m.IsBoss);
                GridCoord origin = new GridCoord(subject.GridX, subject.GridY);
                GridCoord[] neighbours = {new GridCoord(origin.X+1,origin.Y),new GridCoord(origin.X-1,origin.Y),new GridCoord(origin.X,origin.Y+1),new GridCoord(origin.X,origin.Y-1)};
                GridCoord next = neighbours.First(c => grid.IsWalkable(c) && !c.Equals(player.Mover.Position));
                MethodInfo move = typeof(MonsterController).GetMethod("TryMoveTo", PrivateInstance);
                move.Invoke(subject, new object[]{-1,-1});
                if (subject.GridX != origin.X || subject.GridY != origin.Y || grid.IsWalkable(origin))
                    throw new InvalidOperationException("Rejected move changed occupancy.");
                move.Invoke(subject, new object[]{next.X,next.Y});
                if (subject.GridX != next.X || subject.GridY != next.Y || !grid.IsWalkable(origin) || grid.IsWalkable(next))
                    throw new InvalidOperationException("Accepted monster move failed to synchronize occupancy.");
                subject.Health.TakeDamage(subject.Health.MaxHp);
                if (!grid.IsWalkable(next)) throw new InvalidOperationException("Monster death left an invisible blocker.");
                Finish(true, "PASS: saved scene enters PlayMode with one active player, initialized monsters/boss, one HP bar per monster and animated shoreline.\nPASS: rejected move preserves occupancy; accepted move and death update the shared player/monster grid.\n");
            }
            catch (Exception error) { Finish(false, error.ToString()); }
        }

        static void Finish(bool passed, string message)
        {
            SessionState.SetBool(Pending, false);
            EditorApplication.update -= Tick;
            string directory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../verification"));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "slime-kingdom-play-smoke.txt"), message);
            if (passed) Debug.Log("SLIME_PLAY_SMOKE PASS: " + message); else Debug.LogError("SLIME_PLAY_SMOKE FAIL: " + message);
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
