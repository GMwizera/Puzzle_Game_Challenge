using UnityEngine;
using UnityEngine.Events;

// Layer 1: pound the cassava leaves on the beat. Missing the beat resets the count.
public class MortarPuzzle : Interactable
{
    [SerializeField] int layerIndex = 1;
    [SerializeField] int hitsNeeded = 6;
    [SerializeField] float beatInterval = 0.8f;
    [SerializeField] float tolerance = 0.2f;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip beatClip, poundClip, missClip;
    [SerializeField] Renderer leaves;
    [SerializeField] Color rawColor = new Color(0.55f, 0.8f, 0.35f);
    [SerializeField] Color doneColor = new Color(0.1f, 0.3f, 0.1f);
    public UnityEvent onSolved;

    int hits;
    float lastBeat = -10f;
    float nextBeat;

    bool Active => PuzzleManager.Instance.IsActive(layerIndex);

    void Start() => leaves.material.color = rawColor;

    void Update()
    {
        // The beat only plays while this is the current layer, which also acts as a sound clue.
        if (!Active) return;

        if (Time.time >= nextBeat)
        {
            lastBeat = Time.time;
            nextBeat = Time.time + beatInterval;
            audioSource.PlayOneShot(beatClip);
        }
    }

    public override void Interact(PlayerInteractor player)
    {
        if (!Active) return;

        float offBeat = Mathf.Min(Time.time - lastBeat, nextBeat - Time.time);

        if (offBeat <= tolerance)
        {
           hits++;
Debug.Log($"hits {hits} / {hitsNeeded}");
            audioSource.PlayOneShot(poundClip);
            leaves.material.color = Color.Lerp(rawColor, doneColor, (float)hits / hitsNeeded);

            if (hits >= hitsNeeded)
            {
                onSolved.Invoke();
                PuzzleManager.Instance.CompleteLayer(layerIndex);
            }
        }
        else
        {
            hits = 0;
            audioSource.PlayOneShot(missClip);
            leaves.material.color = rawColor;
        }
    }
}
