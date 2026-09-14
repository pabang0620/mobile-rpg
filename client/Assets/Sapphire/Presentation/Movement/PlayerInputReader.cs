using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Movement
{
    /// <summary>
    /// Polls Keyboard.current (WASD + arrow keys) and exposes the currently
    /// held movement direction plus an edge-triggered interact event.
    /// Holds no Domain state - callers decide what to do with the input.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public event Action InteractPressed;

        public bool TryGetHeldDirection(out GridDirection direction)
        {
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
