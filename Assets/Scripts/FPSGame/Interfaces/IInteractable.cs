namespace FPS
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact(PlayerInteraction interactor);
    }
}