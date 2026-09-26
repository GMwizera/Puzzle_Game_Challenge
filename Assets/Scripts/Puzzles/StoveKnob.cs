using UnityEngine;

public class StoveKnob : Interactable
{
    public int layerIndex = 2;
    public Color[] flameColors = { Color.red, new Color(1f, 0.5f, 0f), Color.yellow, Color.blue };
    public int correctIndex = 3;
    public Light flameLight;
    public Renderer flameRenderer;
    public AudioSource audioSource;
    public AudioClip clickClip;

    int current = -1;

    void Start()
    {
        flameLight.enabled = false;
        flameRenderer.enabled = false;
    }

    public bool IsOnCorrectColour()
    {
        return current == correctIndex;
    }

    public override void Interact(PlayerInteractor player)
    {
        if (!PuzzleManager.Instance.IsActive(layerIndex))
        {
            return;
        }

        current = current + 1;
        if (current >= flameColors.Length)
        {
            current = 0;
        }

        transform.Rotate(0f, 0f, -45f);
        audioSource.PlayOneShot(clickClip);

        Color color = flameColors[current];
        flameLight.enabled = true;
        flameLight.color = color;
        flameRenderer.enabled = true;
        flameRenderer.material.color = color;
        flameRenderer.material.SetColor("_EmissionColor", color * 3f);
    }
}
