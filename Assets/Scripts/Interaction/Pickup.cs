using UnityEngine;

public class Pickup : Interactable
{
    public string itemId;
    public bool isLocked = false;

    Rigidbody rb;
    Collider col;
    Transform startParent;
    Vector3 startPosition;
    Quaternion startRotation;
    bool startKinematic;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();
        startParent = transform.parent;
        startPosition = transform.position;
        startRotation = transform.rotation;
        startKinematic = rb.isKinematic;
    }

    public override string GetPrompt()
    {
        if (isLocked)
        {
            return "";
        }
        return prompt;
    }

    public override void Interact(PlayerInteractor player)
    {
        if (!isLocked)
        {
            player.Hold(this);
        }
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
        transform.SetParent(null);
        rb.isKinematic = true;
        col.enabled = true;
        isLocked = true;

        transform.position = slot.position;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        Bounds bounds = GetBounds();
        float moveX = slot.position.x - bounds.center.x;
        float moveY = slot.position.y - bounds.min.y;
        float moveZ = slot.position.z - bounds.center.z;
        transform.position = transform.position + new Vector3(moveX, moveY, moveZ);
    }

    public Vector3 GetHomePosition()
    {
        return startPosition;
    }

    public void ReturnHome()
    {
        gameObject.SetActive(true);
        transform.SetParent(startParent);
        transform.position = startPosition;
        transform.rotation = startRotation;
        rb.isKinematic = startKinematic;
        col.enabled = true;
    }

    Bounds GetBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }
}
