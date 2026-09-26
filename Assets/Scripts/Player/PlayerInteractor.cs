using TMPro;
using UnityEngine;

// Raycasts from the screen centre. E or left click interacts, picks up, places or drops.
public class PlayerInteractor : MonoBehaviour
{
   [SerializeField] Camera cam;
[SerializeField] Transform holdPoint;
[SerializeField] float range = 3f;
[SerializeField] TMP_Text promptText;
[SerializeField] LayerMask interactMask = ~0;

    Pickup held;

    void Update()
    {
        if (Time.timeScale == 0f) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Interactable target = null;
        PlacementZone zone = null;

        // if (Physics.Raycast(ray, out RaycastHit hit, range, interactMask, QueryTriggerInteraction.Collide))
        // {
        //     target = hit.collider.GetComponentInParent<Interactable>();
        //     zone = hit.collider.GetComponentInParent<PlacementZone>();
        // }
        if (Physics.Raycast(ray, out RaycastHit hit, range, interactMask, QueryTriggerInteraction.Collide))
{
    target = hit.collider.GetComponentInParent<Interactable>();
    zone = hit.collider.GetComponentInParent<PlacementZone>();

    if (Input.GetKeyDown(KeyCode.E))
        Debug.Log($"HIT {hit.collider.name} | layer {LayerMask.LayerToName(hit.collider.gameObject.layer)} | zone {(zone == null ? "null" : zone.name)}");
}
else if (Input.GetKeyDown(KeyCode.E))
{
    Debug.Log("HIT nothing");
}

        if (held != null) promptText.text = zone != null ? "Place" : "Drop";
        else promptText.text = target != null ? target.Prompt : "";

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
}
