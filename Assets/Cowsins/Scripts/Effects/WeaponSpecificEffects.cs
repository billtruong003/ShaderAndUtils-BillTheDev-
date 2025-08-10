/// <summary>
/// This script belongs to cowsins as a part of the cowsins FPS Engine. All rights reserved. 
/// </summary>
using cowsins;
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace cowsins
{
    public class WeaponSpecificEffects : MonoBehaviour
    {
        public enum SwayMethod
        {
            Simple, PivotBased
        }

        [Header("CONFIGURATION")]
        [SerializeField] private SwayMethod swayMethod;

        [Header("AIMING MODIFIERS")]
        [Tooltip("Reduces sway intensity when aiming. 0 means no sway, 1 means full sway.")]
        [SerializeField, Range(0, 1)] private float adsSwayReduction = 0.1f;
        [Tooltip("How much faster the weapon stabilizes when aiming. Higher values mean a quicker 'lock' into ADS state.")]
        [SerializeField] private float aimingSmoothMultiplier = 4.0f;

        [Header("PROCEDURAL IDLE SWAY")]
        [SerializeField] private float idleSwaySpeed = 1f;
        [SerializeField] private Vector3 idleSwayAmplitude = new Vector3(0.005f, 0.005f, 0f);
        [SerializeField] private Vector3 idleRotationSwayAmplitude = new Vector3(0.2f, 0.2f, 0.5f);

        [Header("SIMPLE SWAY - REACTIVE")]
        [SerializeField] private float simple_PositionAmount = 0.02f;
        [SerializeField] private float simple_MaxPositionAmount = 0.06f;
        [SerializeField] private float simple_SmoothAmount = 6f;
        [SerializeField] private float simple_TiltAmount = 4f;
        [SerializeField] private float simple_MaxTiltAmount = 5f;
        [SerializeField] private float simple_SmoothTiltAmount = 12f;

        [Header("PIVOT SWAY - REACTIVE")]
        [SerializeField] private Transform pivot;
        [SerializeField] private float pivot_SwaySpeed = 10f;
        [SerializeField] private Vector2 pivot_MovementAmount = Vector2.one;
        [SerializeField] private Vector2 pivot_RotationAmount = Vector2.one;
        [SerializeField] private float pivot_TiltAmount = 2f;

        [Header("CROUCH TILT")]
        [SerializeField] private Vector3 tiltRotation = new Vector3(5, 0, -5);
        [SerializeField] private Vector3 tiltPositionOffset = new Vector3(0, -0.05f, 0);
        [SerializeField] private float tiltSpeed = 8f;

        private delegate void SwayAction();
        private SwayAction applySway;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Coroutine tiltCoroutine;

        private Vector2 perlinNoise;
        private Vector2 swayInput;

        private IPlayerMovementEventsProvider playerMovementProvider;
        private IWeaponBehaviourProvider weaponController;
        private IPlayerControlProvider playerControl;

        private void Start()
        {
            InitializeDependencies();
            InitializeSwayMethod();
            InitializeState();
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (!playerControl.IsControllable) return;

            UpdatePerlinNoise();
            applySway?.Invoke();
        }

        private void InitializeDependencies()
        {
            var playerDependencies = FindFirstObjectByType<PlayerDependencies>();
            weaponController = playerDependencies.WeaponBehaviour;
            playerMovementProvider = playerDependencies.PlayerMovementEvents;
            playerControl = playerDependencies.PlayerControl;
        }

        private void InitializeSwayMethod()
        {
            applySway = swayMethod == SwayMethod.Simple ? (SwayAction)ApplySimpleSway : ApplyPivotSway;
        }

        private void InitializeState()
        {
            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;
        }

        private void SubscribeToEvents()
        {
            if (playerMovementProvider == null) return;
            playerMovementProvider.AddCrouchListener(HandleCrouch);
            playerMovementProvider.AddUncrouchListener(HandleUnCrouch);
        }

        private void UnsubscribeFromEvents()
        {
            if (playerMovementProvider == null) return;
            playerMovementProvider.RemoveCrouchListener(HandleCrouch);
            playerMovementProvider.RemoveUncrouchListener(HandleUnCrouch);
        }

        private void UpdatePerlinNoise()
        {
            float time = Time.time * idleSwaySpeed;
            perlinNoise.x = (Mathf.PerlinNoise(time, 0) - 0.5f) * 2f;
            perlinNoise.y = (Mathf.PerlinNoise(0, time) - 0.5f) * 2f;
        }

        private void CalculateSwayInputs()
        {
            swayInput.x = -InputManager.mousex / 10 - 2 * InputManager.controllerx;
            swayInput.y = -InputManager.mousey / 10 - 2 * InputManager.controllery;
        }

        private float GetCurrentSwayMultiplier()
        {
            return weaponController.IsAiming ? adsSwayReduction : 1f;
        }

        private float GetCurrentSmoothMultiplier()
        {
            return weaponController.IsAiming ? aimingSmoothMultiplier : 1f;
        }

        private void ApplySimpleSway()
        {
            CalculateSwayInputs();
            float swayMultiplier = GetCurrentSwayMultiplier();
            float smoothMultiplier = GetCurrentSmoothMultiplier();

            float moveX = Mathf.Clamp(swayInput.x * simple_PositionAmount, -simple_MaxPositionAmount, simple_MaxPositionAmount);
            float moveY = Mathf.Clamp(swayInput.y * simple_PositionAmount, -simple_MaxPositionAmount, simple_MaxPositionAmount);

            Vector3 reactivePosSway = new Vector3(moveX, moveY, 0);
            Vector3 proceduralPosSway = new Vector3(perlinNoise.x * idleSwayAmplitude.x, perlinNoise.y * idleSwayAmplitude.y, 0);
            Vector3 totalPosSway = (reactivePosSway + proceduralPosSway) * swayMultiplier;
            Vector3 finalPosition = initialPosition + totalPosSway;

            transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition, Time.deltaTime * simple_SmoothAmount * smoothMultiplier);

            float tiltX = Mathf.Clamp(swayInput.x * simple_TiltAmount, -simple_MaxTiltAmount, simple_MaxTiltAmount);
            Quaternion reactiveRotSway = Quaternion.Euler(0, 0, tiltX);
            Quaternion proceduralRotSway = Quaternion.Euler(perlinNoise.y * idleRotationSwayAmplitude.x, perlinNoise.x * idleRotationSwayAmplitude.y, perlinNoise.x * -idleRotationSwayAmplitude.z);
            Quaternion totalRotSway = Quaternion.SlerpUnclamped(Quaternion.identity, proceduralRotSway * reactiveRotSway, swayMultiplier);
            Quaternion finalRotation = initialRotation * totalRotSway;

            transform.localRotation = Quaternion.Slerp(transform.localRotation, finalRotation, Time.deltaTime * simple_SmoothTiltAmount * smoothMultiplier);
        }

        private void ApplyPivotSway()
        {
            if (pivot == null) return;

            CalculateSwayInputs();
            float swayMultiplier = GetCurrentSwayMultiplier();
            float smoothMultiplier = GetCurrentSmoothMultiplier();

            Vector3 reactivePosSway = new Vector3(-swayInput.x * pivot_MovementAmount.x, -swayInput.y * pivot_MovementAmount.y, 0) * 0.1f;
            Vector3 proceduralPosSway = new Vector3(perlinNoise.x * idleSwayAmplitude.x, perlinNoise.y * idleSwayAmplitude.y, 0);
            Vector3 totalPosSway = (reactivePosSway + proceduralPosSway) * swayMultiplier;
            Vector3 targetPosition = totalPosSway;

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * pivot_SwaySpeed * smoothMultiplier);

            Quaternion proceduralRot = Quaternion.Euler(perlinNoise.y * idleRotationSwayAmplitude.x, perlinNoise.x * idleRotationSwayAmplitude.y, perlinNoise.x * -idleRotationSwayAmplitude.z);
            Quaternion reactiveRot = Quaternion.Euler(swayInput.y * pivot_RotationAmount.y, -swayInput.x * pivot_RotationAmount.x, -swayInput.x * pivot_TiltAmount);
            Quaternion totalSwayRotation = Quaternion.SlerpUnclamped(Quaternion.identity, reactiveRot * proceduralRot, swayMultiplier);
            Quaternion targetRotation = initialRotation * totalSwayRotation;

            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * pivot_SwaySpeed * smoothMultiplier);
        }

        private void HandleCrouch()
        {
            if (!weaponController.IsAiming)
                StartCrouchTilt(tiltRotation, initialPosition + tiltPositionOffset);
        }

        private void HandleUnCrouch()
        {
            StartCrouchTilt(initialRotation.eulerAngles, initialPosition);
        }

        private void StartCrouchTilt(Vector3 targetRot, Vector3 targetPos)
        {
            if (tiltCoroutine != null) StopCoroutine(tiltCoroutine);
            tiltCoroutine = StartCoroutine(TiltRoutine(targetRot, targetPos));
        }

        private IEnumerator TiltRoutine(Vector3 targetRotation, Vector3 targetPosition)
        {
            Quaternion targetQuat = Quaternion.Euler(targetRotation);
            float interpolant = 0f;

            while (interpolant < 1.0f)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetQuat, Time.deltaTime * tiltSpeed);
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * tiltSpeed);

                interpolant = Mathf.Max(
                    1 - (Quaternion.Angle(transform.localRotation, targetQuat) / 180f),
                    1 - (Vector3.Distance(transform.localPosition, targetPosition) / 1f)
                );

                yield return null;
            }

            transform.localRotation = targetQuat;
            transform.localPosition = targetPosition;
        }

        public void SetAimingModifiers(float reduction, float multiplier)
        {
            adsSwayReduction = Mathf.Clamp01(reduction);
            aimingSmoothMultiplier = Mathf.Max(1f, multiplier);
        }

        public void ResetAimingModifiers()
        {
            adsSwayReduction = 1.0f;
            aimingSmoothMultiplier = 1.0f;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(WeaponSpecificEffects))]
    public class WeaponSpecificEffectsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var myScript = target as WeaponSpecificEffects;

            EditorGUILayout.LabelField("CONFIGURATION", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("swayMethod"));
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("AIMING MODIFIERS", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("adsSwayReduction"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("aimingSmoothMultiplier"));
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("PROCEDURAL IDLE SWAY", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idleSwaySpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idleSwayAmplitude"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idleRotationSwayAmplitude"));
            EditorGUILayout.Space(15);

            EditorGUILayout.LabelField("REACTIVE SWAY SETTINGS", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if ((WeaponSpecificEffects.SwayMethod)serializedObject.FindProperty("swayMethod").enumValueIndex == WeaponSpecificEffects.SwayMethod.Simple)
            {
                EditorGUILayout.LabelField("Simple Sway", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("simple_PositionAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("simple_MaxPositionAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("simple_SmoothAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("simple_TiltAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("simple_MaxTiltAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("simple_SmoothTiltAmount"));
            }
            else
            {
                EditorGUILayout.LabelField("Pivot Sway", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pivot"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pivot_SwaySpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pivot_MovementAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pivot_RotationAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pivot_TiltAmount"));
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(15);

            EditorGUILayout.LabelField("CROUCH TILT", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tiltRotation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tiltPositionOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tiltSpeed"));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}