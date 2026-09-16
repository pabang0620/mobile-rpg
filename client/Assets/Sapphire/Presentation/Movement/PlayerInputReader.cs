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

            return new HeldDirections(
                up: keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed,
                down: keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed,
                left: keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed,
                right: keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed);
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
