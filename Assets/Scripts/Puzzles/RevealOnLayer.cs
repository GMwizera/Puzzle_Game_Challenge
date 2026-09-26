using System.Collections;
using UnityEngine;

// Keeps an object hidden (and untouchable) until a layer is solved, then pops it in.
// Used for the finished dish, which only exists once the cooking layer is done.
public class RevealOnLayer : MonoBehaviour
{
    [SerializeField] int revealAfterLayer = 3;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip revealClip;

    Renderer[] renderers;
    Collider[] colliders;
    Pickup pickup;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        pickup = GetComponent<Pickup>();
        SetVisible(false);
    }

    void Start() => PuzzleManager.Instance.LayerCompleted += OnLayerCompleted;

    void OnDestroy()
    {
        if (PuzzleManager.Instance != null) PuzzleManager.Instance.LayerCompleted -= OnLayerCompleted;
    }

    void OnLayerCompleted(int layer)
    {
        if (layer != revealAfterLayer) return;
        SetVisible(true);
        if (audioSource != null && revealClip != null) audioSource.PlayOneShot(revealClip);
        StartCoroutine(Pop());
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers) r.enabled = visible;
        foreach (Collider c in colliders) c.enabled = visible;
        if (pickup != null) pickup.SetLocked(!visible);
    }

    IEnumerator Pop()
    {
        Vector3 scale = transform.localScale;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
        {
            transform.localScale = scale * (1f - Mathf.Pow(1f - t, 3f) * 0.8f + 0.15f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        transform.localScale = scale;
    }
}
