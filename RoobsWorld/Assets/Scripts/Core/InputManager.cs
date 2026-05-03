using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Logical input maps supported by the game (player controls, UI navigation, editor tools).
    /// </summary>
    public enum InputMapType
    {
        Player,
        UI,
        Editor
    }

    /// <summary>
    /// Wrapper around Unity's Input System that exposes high-level movement, look, and action inputs for gameplay systems.
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        private InputActionAsset _inputActions;
        private InputActionMap _currentMap;
        private Coroutine _hapticCoroutine;

        public InputActionAsset LoadedInputActions => _inputActions;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool PausePressed { get; private set; }

        public event Action OnJump;
        public event Action OnAttack;
        public event Action OnInteract;
        public event Action OnPause;

        protected override void OnInitialize()
        {
            if (_inputActions == null)
                _inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
            if (_inputActions == null)
                _inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions.inputactions");

            if (_inputActions != null)
            {
                BindActions();
                SwitchActionMap(InputMapType.Player);
                Debug.Log("[InputManager] Loaded InputActionAsset from Resources.");
            }
            else
            {
                Debug.LogWarning("[InputManager] No InputActionAsset. Assign via Bootstrap or place in Resources/ as InputSystem_Actions.");
            }
        }

        /// <summary>Optional: set from Bootstrap so the asset can live in Assets/ without Resources.</summary>
        public void SetInputAsset(InputActionAsset asset)
        {
            _inputActions = asset;
            if (_inputActions != null)
            {
                _currentMap?.Disable();
                BindActions();
                SwitchActionMap(InputMapType.Player);
            }
        }

        public void SwitchActionMap(InputMapType mapType)
        {
            _currentMap?.Disable();
            string mapName = mapType.ToString();

            if (_inputActions == null) return;

            _currentMap = _inputActions.FindActionMap(mapName);
            _currentMap?.Enable();
        }

        public void EnableInput()
        {
            _currentMap?.Enable();
        }

        public void DisableInput()
        {
            _currentMap?.Disable();
        }

        private void BindActions()
        {
            var playerMap = _inputActions.FindActionMap("Player");
            if (playerMap == null) return;

            var moveAction = playerMap.FindAction("Move");
            var lookAction = playerMap.FindAction("Look");
            var jumpAction = playerMap.FindAction("Jump");
            var attackAction = playerMap.FindAction("Attack");
            var interactAction = playerMap.FindAction("Interact");
            var pauseAction = playerMap.FindAction("Pause");

            if (moveAction != null)
            {
                moveAction.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
                moveAction.canceled += _ => MoveInput = Vector2.zero;
            }

            if (lookAction != null)
            {
                lookAction.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
                lookAction.canceled += _ => LookInput = Vector2.zero;
            }

            if (jumpAction != null)
                jumpAction.performed += _ => { JumpPressed = true; OnJump?.Invoke(); };

            if (attackAction != null)
                attackAction.performed += _ => { AttackPressed = true; OnAttack?.Invoke(); };

            if (interactAction != null)
                interactAction.performed += _ => { InteractPressed = true; OnInteract?.Invoke(); };

            if (pauseAction != null)
                pauseAction.performed += _ => { PausePressed = true; OnPause?.Invoke(); };
        }

        /// <summary>
        /// Triggers controller rumble for the given duration. No-op when no gamepad is connected.
        /// Cancels any in-progress haptic before starting the new one.
        /// </summary>
        public void RequestHaptic(float leftMotor, float rightMotor, float duration)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null) return;
            if (_hapticCoroutine != null) StopCoroutine(_hapticCoroutine);
            gamepad.SetMotorSpeeds(leftMotor, rightMotor);
            _hapticCoroutine = StartCoroutine(StopHapticAfter(duration));
        }

        private IEnumerator StopHapticAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            Gamepad.current?.SetMotorSpeeds(0f, 0f);
            _hapticCoroutine = null;
        }

        private void LateUpdate()
        {
            JumpPressed = false;
            AttackPressed = false;
            InteractPressed = false;
            PausePressed = false;
        }
    }
}
