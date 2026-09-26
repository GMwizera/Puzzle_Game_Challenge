using UnityEngine;

public class LayerClue : MonoBehaviour
{
    public int layerIndex;
    public float idleAlpha = 0.45f;
    public float doneAlpha = 0.2f;
    public float pulseSpeed = 2.5f;
    public float pulseScale = 0.05f;

    CanvasGroup group;
    Vector3 startScale;

    void Start()
    {
        group = GetComponent<CanvasGroup>();
        startScale = transform.localScale;
    }

    void Update()
    {
        int completed = PuzzleManager.Instance.completed;
        float targetAlpha = idleAlpha;
        float scale = 1f;

        if (completed > layerIndex)
        {
            targetAlpha = doneAlpha;
        }
        else if (completed == layerIndex)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            targetAlpha = Mathf.Lerp(0.75f, 1f, wave);
            scale = 1f + pulseScale * wave;
        }

        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, Time.deltaTime * 2f);
        transform.localScale = startScale * scale;
    }
}
