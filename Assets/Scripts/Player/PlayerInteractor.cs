using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Raycasts from the screen centre. E or left click interacts, picks up, places or drops.
// The target glows and the crosshair grows so the player always knows what they are aiming at.
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] Transform holdPoint;
    [SerializeField] float range = 3f;
    [SerializeField] TMP_Text promptText;
    [SerializeField] LayerMask interactMask = ~0;

    [Header("Feedback")]
    [SerializeField] Graphic crosshair;
    [SerializeField] Color crosshairIdle = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] Color crosshairActive = new Color(1f, 0.82f, 0.35f, 1f);
    [SerializeField] float crosshairActiveScale = 1.6f;
    [SerializeField] Color highlightGlow = new Color(0.35f, 0.28f, 0.12f);

    Pickup held;
    HoverHighlight highlight;

    void Awake() => highlight = new HoverHighlight(highlightGlow);

    void OnDisable() => highlight.Clear();

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            highlight.Clear();
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Interactable target = null;
        IDropTarget zone = null;

        // Drop zones are triggers. They only matter while carrying something; otherwise they would
        // block the objects behind them (e.g. the prep bowls behind the counter spots).
        QueryTriggerInteraction triggers = held != null ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        if (Physics.Raycast(ray, out RaycastHit hit, range, interactMask, triggers))
        {
            target = hit.collider.GetComponentInParent<Interactable>();
            zone = hit.collider.GetComponentInParent<IDropTarget>();
        }

        // Only highlight things the player can act on right now.
        string prompt;
        GameObject focus;
        if (held != null)
        {
            prompt = zone != null ? zone.DropPrompt : "Drop";
            focus = zone != null ? ((Component)zone).gameObject : null;
        }
        else
        {
            prompt = target != null ? target.Prompt : "";
            focus = string.IsNullOrEmpty(prompt) ? null : target.gameObject;
        }

        promptText.text = string.IsNullOrEmpty(prompt) ? "" : $"<b>[E]</b>  {prompt}";
        highlight.Set(focus);
        UpdateCrosshair(focus != null);

        if (!Input.GetKeyDown(KeyCode.E) && !Input.GetMouseButtonDown(0)) return;

        if (held != null)
        {
            if (zone != null && zone.TryPlace(held)) held = null;
            else Drop();
        }
        else if (target != null)
        {
            target.Interact(this);
        }
    }

    public void Hold(Pickup item)
    {
        held = item;
        item.AttachTo(holdPoint);
    }

    void Drop()
    {
        held.Release();
        held = null;
    }

    void UpdateCrosshair(bool active)
    {
        if (crosshair == null) return;

        float k = 1f - Mathf.Exp(-15f * Time.deltaTime);
        crosshair.color = Color.Lerp(crosshair.color, active ? crosshairActive : crosshairIdle, k);
        float scale = Mathf.Lerp(crosshair.transform.localScale.x, active ? crosshairActiveScale : 1f, k);
        crosshair.transform.localScale = Vector3.one * scale;
    }
}
