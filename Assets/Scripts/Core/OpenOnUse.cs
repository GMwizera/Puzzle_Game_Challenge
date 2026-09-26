using UnityEngine;

// A one-shot interactable that swings a door open, e.g. a cupboard or pantry door.
public class OpenOnUse : Interactable
{
    [SerializeField] Door door;

    bool used;

    public override string Prompt => used ? "" : base.Prompt;

    public override void Interact(PlayerInteractor player)
    {
        if (used) return;
        used = true;
        door.Open();
    }
}
