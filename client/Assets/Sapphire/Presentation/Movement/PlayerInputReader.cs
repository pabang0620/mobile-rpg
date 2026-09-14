using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Movement
{
    /// <summary>
    /// Polls Keyboard.current (WASD + arrow keys) and, when assigned, the
    /// left-side VirtualMovementPad (touch/mouse), then exposes the
    /// currently held movement direction plus an edge-triggered interact
    /// event. Holds no Domain state - callers decide what to do with the
    /// input. The virtual pad takes priority when it reports a direction
    /// (2026-09-14 UI overhaul) so a held keyboard key doesn't fight a
    /// simultaneous touch drag; keyboard still works on its own when the pad
    /// is idle, keeping PC and mobile input on the same code path.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private VirtualMovementPad virtualPad;

        public event Action InteractPressed;

        public bool TryGetHeldDirection(out GridDirection direction)
        {
            if (virtualPad != null && virtualPad.HeldDirection.HasValue)
            {
                direction = virtualPad.HeldDirection.Value;
                return true;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                direction = default;
                return false;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                direction = GridDirection.Up;
                return true;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                direction = GridDirection.Down;
                return true;
            }

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                direction = GridDirection.Left;
                return true;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                direction = GridDirection.Right;
                return true;
            }

            direction = default;
            return false;
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
