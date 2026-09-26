using UnityEngine;
using UnityEngine.Events;

public class Igniter : Interactable
{
    public int layerIndex = 2;
    public StoveKnob knob;
    public GameTimer timer;
    public float wrongPenaltySeconds = 15f;
    public AudioSource audioSource;
    public AudioClip igniteClip;
    public AudioClip failClip;
    public UnityEvent onSolved;

    public override void Interact(PlayerInteractor player)
    {
        if (!PuzzleManager.Instance.IsActive(layerIndex))
        {
            return;
        }

        if (knob.IsOnCorrectColour())
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
