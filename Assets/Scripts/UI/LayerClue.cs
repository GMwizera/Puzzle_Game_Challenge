using UnityEngine;

// Put on a world-space clue (recipe card, flame icon, lid dots).
// It is faint before its layer, gently pulses while its layer is the current one,
// and fades back once that layer is solved, so the room itself shows where to look next.
[RequireComponent(typeof(CanvasGroup))]
public class LayerClue : MonoBehaviour
{
    [SerializeField] int layerIndex;
    [SerializeField] float idleAlpha = 0.45f;
    [SerializeField] float doneAlpha = 0.2f;
    [SerializeField] float pulseSpeed = 2.5f;
    [SerializeField] float pulseScale = 0.05f;

    CanvasGroup group;
    Vector3 baseScale;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        PuzzleManager pm = PuzzleManager.Instance;
        float targetAlpha;
        float scale = 1f;

        if (pm.Completed > layerIndex)
        {
            targetAlpha = doneAlpha;
        }
        else if (pm.Completed == layerIndex)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            targetAlpha = Mathf.Lerp(0.75f, 1f, wave);
            scale = 1f + pulseScale * wave;
        }
        else
        {
            targetAlpha = idleAlpha;
        }

        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, Time.deltaTime * 2f);
        transform.localScale = baseScale * scale;
    }
}
