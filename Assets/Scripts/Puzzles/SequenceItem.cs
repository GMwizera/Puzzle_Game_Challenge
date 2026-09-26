using UnityEngine;

// A prepared ingredient bowl beside the stove. Clicking it adds it to the pot.
public class SequenceItem : Interactable
{
    [SerializeField] SequencePuzzle puzzle;
    [SerializeField] string itemId;

    public override void Interact(PlayerInteractor player) => puzzle.Submit(itemId);
}
