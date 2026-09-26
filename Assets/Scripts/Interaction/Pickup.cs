using UnityEngine;

// An object the player can carry: ingredients, decoys, the plate.
[RequireComponent(typeof(Rigidbody))]
public class Pickup : Interactable
{
    public string itemId;

    Rigidbody rb;
    Collider col;

    public bool Locked { get; private set; }
    public override string Prompt => Locked ? "" : base.Prompt;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();
    }

    public override void Interact(PlayerInteractor player)
    {
        if (!Locked) player.Hold(this);
    }

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

    public void SnapTo(Transform slot)
    {
        transform.SetParent(slot);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        rb.isKinematic = true;
        col.enabled = true;
        Locked = true;
    }
}
