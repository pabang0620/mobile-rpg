using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Movement
{
    /// <summary>
    /// Polls Keyboard.current (WASD + arrow keys) and, when assigned, the
    /// left-side VirtualMovementPad (touch/mouse), then exposes which grid
    /// directions are RAW-held this frame plus an edge-triggered interact
    /// event. Holds no Domain state - callers (GridMoveInputBuffer) decide
    /// priority between simultaneously-held directions. The virtual pad
    /// takes priority when it reports a direction (2026-09-14 UI overhaul)
    /// so a held keyboard key doesn't fight a simultaneous touch drag;
    /// keyboard still works on its own when the pad is idle, keeping PC and
    /// mobile input on the same code path.
    ///
    /// 2026-09-16: replaced the old TryGetHeldDirection(out GridDirection),
    /// which picked a single winner via a FIXED if/else priority chain
    /// (always Up > Down > Left > Right). That silently ignored a Right tap
    /// while Down was held, because Down was checked first every frame
    /// regardless of which key was actually pressed more recently - see
    /// GridMoveInputBuffer's doc. Returning the raw per-direction state here
    /// instead lets the Domain layer decide priority by press recency.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private VirtualMovementPad virtualPad;

        public event Action InteractPressed;

        public HeldDirections GetHeldDirections()
        {
            if (virtualPad != null && virtualPad.HeldDirection.HasValue)
            {
                return HeldDirections.Only(virtualPad.HeldDirection.Value);
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return HeldDirections.None;
            }

            // 2026-09-16: `.isPressed` alone reflects only the settled
            // device state at the moment this frame's Update() reads it.
            // The new Input System coalesces multiple sub-frame events
            // before that read (default Dynamic Update), so a real physical
            // tap whose press-and-release both land inside one Update
            // interval can end up reading `.isPressed == false` for every
            // frame this method is ever called in - the whole tap vanishes,
            // never even registering as a turn-in-place. `wasPressedThisFrame`
            // is exactly the guard for this: the Input System guarantees it
            // is true for at least the one frame a press was detected in,
            // regardless of how quickly it was released. OR'ing it in here
            // means a same-frame press+release still counts as "held" for
            // that one frame - enough for GridMoveInputBuffer to register
            // the turn (or move, if already facing that way).
            return new HeldDirections(
                up: keyboard.wKey.isPressed || keyboard.wKey.wasPressedThisFrame
                    || keyboard.upArrowKey.isPressed || keyboard.upArrowKey.wasPressedThisFrame,
                down: keyboard.sKey.isPressed || keyboard.sKey.wasPressedThisFrame
                    || keyboard.downArrowKey.isPressed || keyboard.downArrowKey.wasPressedThisFrame,
                left: keyboard.aKey.isPressed || keyboard.aKey.wasPressedThisFrame
                    || keyboard.leftArrowKey.isPressed || keyboard.leftArrowKey.wasPressedThisFrame,
                right: keyboard.dKey.isPressed || keyboard.dKey.wasPressedThisFrame
                    || keyboard.rightArrowKey.isPressed || keyboard.rightArrowKey.wasPressedThisFrame);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
            {
                InteractPressed?.Invoke();
            }
        }
    }
}
