using UnityEngine;
using UnityEngine.Events;

// Trigger area that accepts specific items. Used for Layer 0 (counter) and Layer 4 (serving tray).
public class PlacementZone : MonoBehaviour
{
    [SerializeField] int layerIndex;
    [SerializeField] string[] acceptedIds;
    [SerializeField] Transform[] slots;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip acceptClip, rejectClip;
    public UnityEvent onSolved;

    int placed;

    readonly System.Collections.Generic.HashSet<string> filled = new();

public bool TryPlace(Pickup item)
{
    if (!PuzzleManager.Instance.IsActive(layerIndex)) return false;

    if (System.Array.IndexOf(acceptedIds, item.itemId) < 0 || !filled.Add(item.itemId))
    {
        audioSource.PlayOneShot(rejectClip);
        return false;
    }

    item.SnapTo(slots[filled.Count - 1]);
    audioSource.PlayOneShot(acceptClip);

    if (filled.Count == acceptedIds.Length)
    {
        onSolved.Invoke();
        PuzzleManager.Instance.CompleteLayer(layerIndex);
    }
    return true;
}

    // public bool TryPlace(Pickup item)
    // {
    //     if (!PuzzleManager.Instance.IsActive(layerIndex)) return false;

    //     if (System.Array.IndexOf(acceptedIds, item.itemId) < 0)
    //     {
    //         audioSource.PlayOneShot(rejectClip);
    //         return false;
    //     }

    //     item.SnapTo(slots[placed]);
    //     placed++;
    //     audioSource.PlayOneShot(acceptClip);

    //     if (placed == acceptedIds.Length)
    //     {
    //         onSolved.Invoke();
    //         PuzzleManager.Instance.CompleteLayer(layerIndex);
    //     }
    //     return true;
    // }
}
