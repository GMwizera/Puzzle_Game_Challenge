using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Layer 3: carry the prep bowls to the pot in the order shown by the coloured dots on the wall.
// A wrong bowl makes smoke, sends every bowl back to its place and costs time.
public class SequencePuzzle : MonoBehaviour, IDropTarget
{
    [SerializeField] int layerIndex = 3;
    [SerializeField] string[] correctOrder = { "oil", "onion", "eggplant", "leaves" };
    [SerializeField] float wrongPenaltySeconds = 20f;
    [SerializeField] GameTimer timer;
    [SerializeField] ParticleSystem smoke;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip sizzleClip, failClip;
    public UnityEvent onSolved;

    readonly List<Pickup> added = new();

    public string DropPrompt => "Add to pot";
    public IReadOnlyList<string> CorrectOrder => correctOrder;

    public bool TryPlace(Pickup item)
    {
        if (!PuzzleManager.Instance.IsActive(layerIndex)) return false;

        if (item.itemId == correctOrder[added.Count])
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

        // Wrong ingredient: the dish is spoiled, start the order again.
        smoke.Play();
        audioSource.PlayOneShot(failClip);
        timer.Penalize(wrongPenaltySeconds);

        foreach (Pickup bowl in added) bowl.ReturnHome();
        added.Clear();
        item.ReturnHome();
        return true;
    }
}
