namespace DefenderOfIndependence.Level1
{
    public interface IPlayerInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract { get; }
        void Interact();
    }
}
