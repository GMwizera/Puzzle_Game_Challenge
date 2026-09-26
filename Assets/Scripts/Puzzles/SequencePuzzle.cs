using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SequencePuzzle : MonoBehaviour
{
    public int layerIndex = 3;
    public string[] correctOrder = { "oil", "onion", "eggplant", "leaves" };
    public float wrongPenaltySeconds = 20f;
    public GameTimer timer;
    public ParticleSystem smoke;
    public AudioSource audioSource;
    public AudioClip sizzleClip;
    public AudioClip failClip;
    public UnityEvent onSolved;

    List<Pickup> added = new List<Pickup>();

    public bool TryPlace(Pickup item)
    {
        if (!PuzzleManager.Instance.IsActive(layerIndex))
        {
            return false;
        }

        string nextId = correctOrder[added.Count];

        if (item.itemId == nextId)
        {
            added.Add(item);
            item.gameObject.SetActive(false);
            audioSource.PlayOneShot(sizzleClip);

            if (added.Count == correctOrder.Length)
            {
                onSolved.Invoke();
                PuzzleManager.Instance.CompleteLayer(layerIndex);
            }
            return true;
        }

        smoke.Play();
        audioSource.PlayOneShot(failClip);
        timer.Penalize(wrongPenaltySeconds);

        foreach (Pickup bowl in added)
        {
            bowl.ReturnHome();
        }
        added.Clear();
        item.ReturnHome();
        return true;
    }
}
