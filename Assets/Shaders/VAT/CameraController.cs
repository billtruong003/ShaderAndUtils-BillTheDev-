using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace BillTheDev.CameraControl
{
    public class AdvancedCameraController : MonoBehaviour
    {
        #region Button Reference Structures

        [System.Serializable]
        private struct MovementButtons
        {
            public Button forward;
            public Button backward;
            public Button left;
            public Button right;
        }

        [System.Serializable]
        private struct VerticalButtons
        {
            public Button up;
            public Button down;
        }

        [System.Serializable]
        private struct RotationButtons
        {
            public Button lookUp;
            public Button lookDown;
            public Button lookLeft;
            public Button lookRight;
        }

        #endregion

        #region Inspector Fields

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 15f;
        [SerializeField] private float fastMoveMultiplier = 2.5f;
        [SerializeField] private float verticalSpeed = 10f;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 80f;
        [SerializeField] private float maxVerticalAngle = 89f;

        [Header("Button References")]
        [SerializeField] private MovementButtons movementButtons;
        [SerializeField] private VerticalButtons verticalButtons;
        [SerializeField] private RotationButtons rotationButtons;
        [SerializeField] private Button fastModeToggleButton; // Optional

        #endregion

        #region Private State

        private Vector3 _movementInput = Vector3.zero;
        private Vector2 _rotationInput = Vector2.zero;
        private float _verticalLookAngle = 0f;
        private bool _isFastModeActive = false;
        private List<EventTrigger> _createdTriggers = new List<EventTrigger>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupButtonListeners();
            _verticalLookAngle = transform.localEulerAngles.x;
        }

        private void OnDestroy()
        {
            // Clean up to prevent memory leaks
            foreach (var trigger in _createdTriggers)
            {
                if (trigger != null)
                {
                    trigger.triggers.Clear();
                }
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleRotation();
        }

        #endregion

        #region Internal Logic

        private void HandleMovement()
        {
            if (_movementInput.sqrMagnitude < 0.01f) return;

            float currentMoveSpeed = _isFastModeActive ? moveSpeed * fastMoveMultiplier : moveSpeed;
            float currentVerticalSpeed = _isFastModeActive ? verticalSpeed * fastMoveMultiplier : verticalSpeed;

            Vector3 horizontalMovement = new Vector3(_movementInput.x, 0, _movementInput.z).normalized * currentMoveSpeed;
            Vector3 verticalMovement = Vector3.up * _movementInput.y * currentVerticalSpeed;

            transform.Translate(horizontalMovement * Time.deltaTime, Space.Self);
            transform.Translate(verticalMovement * Time.deltaTime, Space.World);
        }

        private void HandleRotation()
        {
            if (_rotationInput.sqrMagnitude < 0.01f) return;

            // Yaw (Left/Right) rotation
            transform.Rotate(Vector3.up, _rotationInput.y * rotationSpeed * Time.deltaTime, Space.World);

            // Pitch (Up/Down) rotation
            _verticalLookAngle -= _rotationInput.x * rotationSpeed * Time.deltaTime;
            _verticalLookAngle = Mathf.Clamp(_verticalLookAngle, -maxVerticalAngle, maxVerticalAngle);

            transform.localEulerAngles = new Vector3(_verticalLookAngle, transform.localEulerAngles.y, 0);
        }

        private void SetupButtonListeners()
        {
            // Movement
            AssignHoldAndRelease(movementButtons.forward, () => _movementInput.z = 1, () => _movementInput.z = 0);
            AssignHoldAndRelease(movementButtons.backward, () => _movementInput.z = -1, () => _movementInput.z = 0);
            AssignHoldAndRelease(movementButtons.left, () => _movementInput.x = -1, () => _movementInput.x = 0);
            AssignHoldAndRelease(movementButtons.right, () => _movementInput.x = 1, () => _movementInput.x = 0);

            // Vertical
            AssignHoldAndRelease(verticalButtons.up, () => _movementInput.y = 1, () => _movementInput.y = 0);
            AssignHoldAndRelease(verticalButtons.down, () => _movementInput.y = -1, () => _movementInput.y = 0);

            // Rotation
            AssignHoldAndRelease(rotationButtons.lookUp, () => _rotationInput.x = 1, () => _rotationInput.x = 0);
            AssignHoldAndRelease(rotationButtons.lookDown, () => _rotationInput.x = -1, () => _rotationInput.x = 0);
            AssignHoldAndRelease(rotationButtons.lookLeft, () => _rotationInput.y = -1, () => _rotationInput.y = 0);
            AssignHoldAndRelease(rotationButtons.lookRight, () => _rotationInput.y = 1, () => _rotationInput.y = 0);

            // Fast Mode (uses onClick because it's a toggle)
            if (fastModeToggleButton != null)
            {
                fastModeToggleButton.onClick.AddListener(ToggleFastMode);
            }
        }

        private void AssignHoldAndRelease(Button button, UnityAction onPointerDownAction, UnityAction onPointerUpAction)
        {
            if (button == null) return;

            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();
            _createdTriggers.Add(trigger);

            // Pointer Down Entry
            var pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDownEntry.callback.AddListener((data) => { onPointerDownAction(); });
            trigger.triggers.Add(pointerDownEntry);

            // Pointer Up Entry
            var pointerUpEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUpEntry.callback.AddListener((data) => { onPointerUpAction(); });
            trigger.triggers.Add(pointerUpEntry);
        }

        private void ToggleFastMode()
        {
            _isFastModeActive = !_isFastModeActive;
        }

        #endregion
    }
}