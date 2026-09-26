using UnityEngine;

// Layer 2, step 1: each turn of the knob cycles the flame colour.
// The Igniter checks the colour; the blue flame icon on the wall is the clue.
public class StoveKnob : Interactable
{
    [SerializeField] int layerIndex = 2;
    [SerializeField] Color[] flameColors = { Color.red, new Color(1f, 0.5f, 0f), Color.yellow, Color.blue };
    [SerializeField] int correctIndex = 3;
    [SerializeField] Light flameLight;
    [SerializeField] Renderer flameRenderer;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip clickClip;

    int current = -1;

    public bool OnCorrectColour => current == correctIndex;

    void Start()
    {
        flameLight.enabled = false;
        flameRenderer.enabled = false;
    }

    public override void Interact(PlayerInteractor player)
    {
        if (!PuzzleManager.Instance.IsActive(layerIndex)) return;

        current = (current + 1) % flameColors.Length;
        transform.Rotate(0f, 0f, -45f); // change the axis if your knob model turns differently
        audioSource.PlayOneShot(clickClip);

        Color c = flameColors[current];
        flameLight.enabled = true;
        flameLight.color = c;
        flameRenderer.enabled = true;
        flameRenderer.material.color = c;
        flameRenderer.material.SetColor("_EmissionColor", c * 3f);
    }
}
