using UnityEngine;

// An object the player can carry: ingredients, decoys, prep bowls, the dish.
[RequireComponent(typeof(Rigidbody))]
public class Pickup : Interactable
{
    public string itemId;

    Rigidbody rb;
    Collider col;
    Transform homeParent;
    Vector3 homePosition;
    Quaternion homeRotation;
    bool homeKinematic;

    public bool Locked { get; private set; }
    public override string Prompt => Locked ? "" : base.Prompt;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();
        homeParent = transform.parent;
        homePosition = transform.position;
        homeRotation = transform.rotation;
        homeKinematic = rb.isKinematic;
    }

    public override void Interact(PlayerInteractor player)
    {
        if (!Locked) player.Hold(this);
    }

    public void SetLocked(bool locked) => Locked = locked;

    public void AttachTo(Transform point)
    {
        rb.isKinematic = true;
        col.enabled = false;
        transform.SetParent(point);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Release()
    {
        transform.SetParent(null);
        rb.isKinematic = false;
        col.enabled = true;
    }

    // Sits the item upright on a slot at its real size. It is not parented to the slot,
    // because slots usually live inside scaled furniture and would stretch the item.
    public void SnapTo(Transform slot)
    {
        transform.SetParent(null);
        rb.isKinematic = true;
        col.enabled = true;
        Locked = true;

        transform.SetPositionAndRotation(slot.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        Bounds b = WorldBounds();
        transform.position += new Vector3(slot.position.x - b.center.x, slot.position.y - b.min.y, slot.position.z - b.center.z);
    }

    // Puts the item back where the level designer placed it.
    public void ReturnHome()
    {
        gameObject.SetActive(true);
        transform.SetParent(homeParent);
        transform.SetPositionAndRotation(homePosition, homeRotation);
        rb.isKinematic = homeKinematic;
        col.enabled = true;
    }

    Bounds WorldBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(transform.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
