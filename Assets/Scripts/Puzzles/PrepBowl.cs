using System.Collections;
using UnityEngine;

public class PrepBowl : MonoBehaviour
{
    public int fillOnLayer = -1;
    public Color emptyColor = new Color(0.45f, 0.45f, 0.45f);
    public string contentMaterialPrefix = "M_Bowl";

    Pickup pickup;
    PlacementZone counter;
    Material content;
    Color fullColor;
    bool filled = false;

    void Start()
    {
        pickup = GetComponent<Pickup>();
        pickup.isLocked = true;

        GameObject counterObject = GameObject.Find("CounterZone");
        if (counterObject != null)
        {
            counter = counterObject.GetComponent<PlacementZone>();
        }

        Renderer rend = GetComponentInChildren<Renderer>();
        foreach (Material mat in rend.materials)
        {
            if (mat.name.StartsWith(contentMaterialPrefix))
            {
                content = mat;
            }
        }

        if (content != null)
        {
            fullColor = content.color;
            content.color = emptyColor;
        }
    }

    void Update()
    {
        if (filled)
        {
            return;
        }

        if (fillOnLayer == -1)
        {
            if (counter != null && counter.HasItem(pickup.itemId))
            {
                Fill();
            }
        }
        else if (PuzzleManager.Instance.completed > fillOnLayer)
        {
            Fill();
        }
    }

    public bool IsFilled()
    {
        return filled;
    }

    void Fill()
    {
        filled = true;
        pickup.isLocked = false;
        StartCoroutine(FillAnimation());
    }

    IEnumerator FillAnimation()
    {
        Vector3 startScale = transform.localScale;
        float duration = 0.6f;
        float timer = 0f;

        while (timer < duration)
        {
            timer = timer + Time.deltaTime;
            float amount = timer / duration;
            if (content != null)
            {
                content.color = Color.Lerp(emptyColor, fullColor, amount);
            }
            float pop = 1f + 0.2f * Mathf.Sin(amount * Mathf.PI);
            transform.localScale = startScale * pop;
            yield return null;
        }

        if (content != null)
        {
            content.color = fullColor;
        }
        transform.localScale = startScale;
    }
}
