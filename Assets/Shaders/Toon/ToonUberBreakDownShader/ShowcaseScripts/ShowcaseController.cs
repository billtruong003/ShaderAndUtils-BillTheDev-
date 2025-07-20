using UnityEngine;

public sealed class ShowcaseController : MonoBehaviour
{
    // --- Configuration: Automatic Behavior ---
    [Header("Automatic Behavior")]
    [SerializeField]
    private Vector3 automaticRotationSpeed = new Vector3(0f, 20f, 0f);

    [SerializeField]
    private bool enableFloatingEffect = true;

    [SerializeField]
    private float floatingAmplitude = 0.1f;

    [SerializeField]
    private float floatingFrequency = 0.5f;

    // --- Configuration: User Interaction ---
    [Header("User Interaction")]
    [SerializeField]
    private bool enableMouseInteraction = true;

    [SerializeField]
    private float mouseDragSensitivity = 0.25f;

    [SerializeField]
    private float timeUntilAutoResume = 3.0f;

    // --- Internal State ---
    private Vector3 initialPosition;
    private bool isBeingDragged = false;
    private float idleTimer = 0f;
    private Vector3 lastMousePosition;

    private void Awake()
    {
        StoreInitialPosition();
    }

    private void Update()
    {
        if (enableMouseInteraction)
        {
            HandleMouseInput();
        }

        if (isBeingDragged)
        {
            HandleDragRotation();
        }
        else
        {
            HandleAutomaticBehavior();
        }
    }

    private void StoreInitialPosition()
    {
        initialPosition = transform.position;
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isBeingDragged = true;
            idleTimer = 0f;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isBeingDragged = false;
        }
    }

    private void HandleDragRotation()
    {
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
        lastMousePosition = Input.mousePosition;

        // Rotate around world Y-axis based on horizontal mouse movement
        transform.Rotate(Vector3.up, -mouseDelta.x * mouseDragSensitivity, Space.World);

        // Rotate around world X-axis based on vertical mouse movement
        transform.Rotate(Vector3.right, mouseDelta.y * mouseDragSensitivity, Space.World);
    }

    private void HandleAutomaticBehavior()
    {
        if (idleTimer < timeUntilAutoResume)
        {
            idleTimer += Time.deltaTime;
            return;
        }

        ApplyAutomaticRotation();
        ApplyFloatingEffect();
    }

    private void ApplyAutomaticRotation()
    {
        transform.Rotate(automaticRotationSpeed * Time.deltaTime, Space.World);
    }

    private void ApplyFloatingEffect()
    {
        if (!enableFloatingEffect)
        {
            return;
        }

        float sineWave = Mathf.Sin(Time.time * floatingFrequency * Mathf.PI * 2f);
        Vector3 floatingOffset = Vector3.up * sineWave * floatingAmplitude;
        transform.position = initialPosition + floatingOffset;
    }
}