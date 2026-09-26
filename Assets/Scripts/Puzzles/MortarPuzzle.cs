using UnityEngine;
using UnityEngine.Events;

public class MortarPuzzle : Interactable
{
    public int layerIndex = 1;
    public int hitsNeeded = 6;
    public float beatInterval = 0.8f;
    public float tolerance = 0.2f;
    public AudioSource audioSource;
    public AudioClip beatClip;
    public AudioClip poundClip;
    public AudioClip missClip;
    public Renderer leaves;
    public Color rawColor = new Color(0.55f, 0.8f, 0.35f);
    public Color doneColor = new Color(0.1f, 0.3f, 0.1f);
    public UnityEvent onSolved;

    int hits = 0;
    float lastBeat = -10f;
    float nextBeat = 0f;

    void Start()
    {
        leaves.material.color = rawColor;
    }

    void Update()
    {
        leaves.enabled = PuzzleManager.Instance.completed >= layerIndex;

        if (!PuzzleManager.Instance.IsActive(layerIndex))
        {
            return;
        }

        if (Time.time >= nextBeat)
        {
            lastBeat = Time.time;
            nextBeat = Time.time + beatInterval;
            audioSource.PlayOneShot(beatClip);
        }
    }

    public override void Interact(PlayerInteractor player)
    {
        if (!PuzzleManager.Instance.IsActive(layerIndex))
        {
            return;
        }

        float timeSinceBeat = Time.time - lastBeat;
        float timeToNextBeat = nextBeat - Time.time;
        float offBeat = Mathf.Min(timeSinceBeat, timeToNextBeat);

        if (offBeat <= tolerance)
        {
            hits = hits + 1;
            audioSource.PlayOneShot(poundClip);
            float progress = (float)hits / hitsNeeded;
            leaves.material.color = Color.Lerp(rawColor, doneColor, progress);

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
