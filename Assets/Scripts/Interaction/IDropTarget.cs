// Anything the player can put a carried item into: the counter, the serving tray, the pot.
public interface IDropTarget
{
    string DropPrompt { get; }

    // Returns true if the target took the item (the player's hands are then empty).
    bool TryPlace(Pickup item);
}
