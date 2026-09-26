using UnityEngine;
using UnityEngine.Events;

// Layer 2, step 2: light the stove. Only works when the knob is on the blue flame.
// Lighting it on the wrong colour costs time, so guessing through every colour is punished.
public class Igniter : Interactable
{
    [SerializeField] int layerIndex = 2;
    [SerializeField] StoveKnob knob;
    [SerializeField] GameTimer timer;
    [SerializeField] float wrongPenaltySeconds = 15f;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip igniteClip, failClip;
    public UnityEvent onSolved;

    public override void Interact(PlayerInteractor player)
    {
        if (!PuzzleManager.Instance.IsActive(layerIndex)) return;

        if (knob.OnCorrectColour)
        {
            audioSource.PlayOneShot(igniteClip);
            onSolved.Invoke();
            PuzzleManager.Instance.CompleteLayer(layerIndex);
        }
        else
        {
            audioSource.PlayOneShot(failClip);
            timer.Penalize(wrongPenaltySeconds);
        }
    }
}
