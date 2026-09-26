using UnityEngine;
using UnityEngine.Events;

// Layer 3: add ingredients to the pot in the order shown by the coloured dots on the lid.
// A wrong ingredient makes smoke, resets the order and costs time.
public class SequencePuzzle : MonoBehaviour
{
    [SerializeField] int layerIndex = 3;
    [SerializeField] string[] correctOrder = { "oil", "onion", "eggplant", "leaves" };
    [SerializeField] float wrongPenaltySeconds = 20f;
    [SerializeField] GameTimer timer;
    [SerializeField] ParticleSystem smoke;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip sizzleClip, failClip;
    public UnityEvent onSolved;

    int step;

    public void Submit(string id)
    {
        if (!PuzzleManager.Instance.IsActive(layerIndex)) return;

        if (id == correctOrder[step])
        {
            step++;
            audioSource.PlayOneShot(sizzleClip);

            if (step == correctOrder.Length)
            {
                onSolved.Invoke();
                PuzzleManager.Instance.CompleteLayer(layerIndex);
            }
        }
        else
        {
            step = 0;
            smoke.Play();
            audioSource.PlayOneShot(failClip);
            timer.Penalize(wrongPenaltySeconds);
        }
    }
}
