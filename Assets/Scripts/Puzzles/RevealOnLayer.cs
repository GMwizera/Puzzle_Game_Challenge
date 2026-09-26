using System.Collections;
using UnityEngine;

public class RevealOnLayer : MonoBehaviour
{
    public int revealAfterLayer = 3;
    public AudioSource audioSource;
    public AudioClip revealClip;

    Renderer[] renderers;
    Collider[] colliders;
    Pickup pickup;
    bool shown = false;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        pickup = GetComponent<Pickup>();
        SetVisible(false);
    }

    void Update()
    {
        if (shown)
        {
            return;
        }

        if (PuzzleManager.Instance.completed > revealAfterLayer)
        {
            shown = true;
            SetVisible(true);
            if (audioSource != null && revealClip != null)
            {
                audioSource.PlayOneShot(revealClip);
            }
            StartCoroutine(Pop());
        }
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer rend in renderers)
        {
            rend.enabled = visible;
        }
        foreach (Collider col in colliders)
        {
            col.enabled = visible;
        }
        if (pickup != null)
        {
            pickup.isLocked = !visible;
        }
    }

    IEnumerator Pop()
    {
        Vector3 startScale = transform.localScale;
        float duration = 0.4f;
        float timer = 0f;

        while (timer < duration)
        {
            timer = timer + Time.deltaTime;
            float amount = timer / duration;
            float scale = Mathf.Lerp(0.2f, 1f, amount) + 0.15f * Mathf.Sin(amount * Mathf.PI);
            transform.localScale = startScale * scale;
            yield return null;
        }

        transform.localScale = startScale;
    }
}
