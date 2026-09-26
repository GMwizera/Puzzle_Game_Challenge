using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlacementZone : MonoBehaviour
{
    public int layerIndex;
    public string[] acceptedIds;
    public Transform[] slots;
    public AudioSource audioSource;
    public AudioClip acceptClip;
    public AudioClip rejectClip;
    public UnityEvent onSolved;

    List<string> placedIds = new List<string>();

    public bool HasItem(string id)
    {
        return placedIds.Contains(id);
    }

    public bool TryPlace(Pickup item)
    {
        if (!PuzzleManager.Instance.IsActive(layerIndex))
        {
            return false;
        }

        int index = System.Array.IndexOf(acceptedIds, item.itemId);

        if (index == -1 || placedIds.Contains(item.itemId))
        {
            audioSource.PlayOneShot(rejectClip);
            return false;
        }

        placedIds.Add(item.itemId);

        if (slots.Length == acceptedIds.Length)
        {
            item.SnapTo(slots[index]);
        }
        else
        {
            item.SnapTo(slots[placedIds.Count - 1]);
        }

        audioSource.PlayOneShot(acceptClip);

        if (placedIds.Count == acceptedIds.Length)
        {
            onSolved.Invoke();
            PuzzleManager.Instance.CompleteLayer(layerIndex);
        }

        return true;
    }
}
