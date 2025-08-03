using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;

namespace FPS
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Title("Dependencies")]
        [SerializeField, Required] private Camera mainCamera;

        [Title("Configuration")]
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionLayer;

        [Title("UI Dependencies")]
        [SerializeField, Required] private GameObject interactionIndicator; // Chấm tròn giữa màn hình
        [SerializeField, Required] private TextMeshProUGUI interactionPromptText;

        private IInteractable currentInteractable;

        private void Start()
        {
            interactionIndicator.SetActive(false);
            interactionPromptText.gameObject.SetActive(false);
        }

        private void Update()
        {
            FindInteractable();
            HandleInteractionInput();
        }

        private void FindInteractable()
        {
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayer))
            {
                if (hit.collider.TryGetComponent(out IInteractable interactable))
                {
                    if (interactable != currentInteractable)
                    {
                        currentInteractable = interactable;
                        interactionIndicator.SetActive(true);
                        interactionPromptText.gameObject.SetActive(true);
                        interactionPromptText.text = currentInteractable.InteractionPrompt;
                    }
                    return;
                }
            }

            // Nếu không trỏ vào vật nào hoặc vật không có IInteractable
            if (currentInteractable != null)
            {
                currentInteractable = null;
                interactionIndicator.SetActive(false);
                interactionPromptText.gameObject.SetActive(false);
            }
        }

        private void HandleInteractionInput()
        {
            if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
            {
                currentInteractable.Interact(this);
            }
        }
    }
}