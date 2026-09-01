namespace RailCraft.ThirdPerson.Player
{
    public interface IPlayerInteractable
    {
        string InteractionPrompt { get; }

        bool CanInteract(InteractionContext context);

        void Interact(InteractionContext context);
    }
}
