using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInteractor : MonoBehaviour
{
    public Camera cam;
    public Transform holdPoint;
    public float range = 3f;
    public TMP_Text promptText;
    public LayerMask interactMask = ~0;
    public float putBackDistance = 2f;

    public Graphic crosshair;
    public Color crosshairIdle = new Color(1f, 1f, 1f, 0.6f);
    public Color crosshairActive = new Color(1f, 0.82f, 0.35f, 1f);
    public float crosshairActiveScale = 1.6f;
    public Color highlightGlow = new Color(0.35f, 0.28f, 0.12f);

    Pickup held;
    GameObject highlighted;
    List<Material> glowMaterials = new List<Material>();
    List<Color> oldColors = new List<Color>();

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            SetHighlight(null);
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore;
        if (held != null)
        {
            triggers = QueryTriggerInteraction.Collide;
        }

        Interactable target = null;
        PlacementZone zone = null;
        SequencePuzzle pot = null;

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, range, interactMask, triggers))
        {
            target = hit.collider.GetComponentInParent<Interactable>();
            zone = hit.collider.GetComponentInParent<PlacementZone>();
            pot = hit.collider.GetComponentInParent<SequencePuzzle>();
        }

        string prompt = "";
        GameObject focus = null;

        if (held != null)
        {
            if (pot != null)
            {
                prompt = "Add to pot";
                focus = pot.gameObject;
            }
            else if (zone != null && PuzzleManager.Instance.IsActive(zone.layerIndex))
            {
                prompt = "Place";
                focus = zone.gameObject;
            }
            else if (CanPutBack())
            {
                prompt = "Put back";
            }
            else
            {
                prompt = "Drop";
            }
        }
        else if (target != null)
        {
            prompt = target.GetPrompt();
            if (prompt != "")
            {
                focus = target.gameObject;
            }
        }

        if (prompt == "")
        {
            promptText.text = "";
        }
        else
        {
            promptText.text = "<b>[E]</b>  " + prompt;
        }

        SetHighlight(focus);
        UpdateCrosshair(focus != null);

        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            if (held != null)
            {
                bool placed = false;
                if (pot != null)
                {
                    placed = pot.TryPlace(held);
                }
                else if (zone != null)
                {
                    placed = zone.TryPlace(held);
                }

                if (placed)
                {
                    held = null;
                }
                else if (CanPutBack())
                {
                    PutBack();
                }
                else
                {
                    Drop();
                }
            }
            else if (target != null)
            {
                target.Interact(this);
            }
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

    bool CanPutBack()
    {
        float distance = Vector3.Distance(transform.position, held.GetHomePosition());
        return distance < putBackDistance;
    }

    void PutBack()
    {
        held.ReturnHome();
        held = null;
    }

    void SetHighlight(GameObject target)
    {
        if (target == highlighted)
        {
            return;
        }

        for (int i = 0; i < glowMaterials.Count; i++)
        {
            if (glowMaterials[i] != null)
            {
                glowMaterials[i].SetColor("_EmissionColor", oldColors[i]);
            }
        }
        glowMaterials.Clear();
        oldColors.Clear();

        highlighted = target;
        if (target == null)
        {
            return;
        }

        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    Color oldColor = mat.GetColor("_EmissionColor");
                    glowMaterials.Add(mat);
                    oldColors.Add(oldColor);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", oldColor + highlightGlow);
                }
            }
        }
    }

    void UpdateCrosshair(bool active)
    {
        if (crosshair == null)
        {
            return;
        }

        Color targetColor = crosshairIdle;
        float targetScale = 1f;
        if (active)
        {
            targetColor = crosshairActive;
            targetScale = crosshairActiveScale;
        }

        float speed = Time.deltaTime * 15f;
        crosshair.color = Color.Lerp(crosshair.color, targetColor, speed);
        float scale = Mathf.Lerp(crosshair.transform.localScale.x, targetScale, speed);
        crosshair.transform.localScale = new Vector3(scale, scale, scale);
    }
}
