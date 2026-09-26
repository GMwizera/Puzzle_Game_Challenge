using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Trigger area that accepts specific items. Used for Layer 0 (counter) and Layer 4 (serving tray).
// Each accepted id can only be placed once, and decoys (e.g. the banana) are rejected.
public class PlacementZone : MonoBehaviour, IDropTarget
{
    [SerializeField] int layerIndex;
    [SerializeField] string[] acceptedIds;
    [SerializeField] Transform[] slots;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip acceptClip, rejectClip;
    public UnityEvent onSolved;

    // Raised for every accepted item, so other objects (e.g. prep bowls) can react.
    public static event Action<Pickup> ItemPlaced;

    readonly HashSet<string> filled = new();

    public string DropPrompt => "Place";

    public bool TryPlace(Pickup item)
    {
        if (!PuzzleManager.Instance.IsActive(layerIndex)) return false;

        if (Array.IndexOf(acceptedIds, item.itemId) < 0 || !filled.Add(item.itemId))
        {
            audioSource.PlayOneShot(rejectClip);
            return false;
        }

        // With one slot per accepted id, each item has its own spot (e.g. in front of its prep bowl).
        int slot = slots.Length == acceptedIds.Length ? Array.IndexOf(acceptedIds, item.itemId) : filled.Count - 1;
        item.SnapTo(slots[slot]);
        audioSource.PlayOneShot(acceptClip);
        ItemPlaced?.Invoke(item);

        if (filled.Count == acceptedIds.Length)
        {
            onSolved.Invoke();
            PuzzleManager.Instance.CompleteLayer(layerIndex);
        }
        return true;
    }
}
