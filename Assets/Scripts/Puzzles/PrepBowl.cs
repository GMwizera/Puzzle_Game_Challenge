using System.Collections;
using UnityEngine;

// A prep bowl for the cooking step. It starts grey, empty and cannot be picked up.
// It fills with its ingredient's colour when that ingredient reaches the counter,
// or, for the cassava leaves, when the pounding layer is finished.
// This links every earlier layer to the cooking layer.
[RequireComponent(typeof(Pickup))]
public class PrepBowl : MonoBehaviour
{
    const int FillOnPlaced = -1;

    [Tooltip("-1: fill when this ingredient is placed on the counter. Otherwise: fill when this layer is solved.")]
    [SerializeField] int fillOnLayer = FillOnPlaced;
    [SerializeField] Color emptyColor = new Color(0.45f, 0.45f, 0.45f);
    [SerializeField] string contentMaterialPrefix = "M_Bowl";

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int LegacyColor = Shader.PropertyToID("_Color");

    Pickup pickup;
    Renderer[] renderers;
    Color fullColor = Color.white;
    MaterialPropertyBlock block;
    bool filled;

    public bool Filled => filled;

    void Awake()
    {
        pickup = GetComponent<Pickup>();
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();

        foreach (Renderer r in renderers)
            foreach (Material m in r.sharedMaterials)
                if (m != null && m.name.StartsWith(contentMaterialPrefix))
                    fullColor = m.HasProperty(BaseColor) ? m.GetColor(BaseColor) : m.color;

        pickup.SetLocked(true);
        SetContentColor(emptyColor);
    }

    void Start()
    {
        PlacementZone.ItemPlaced += OnItemPlaced;
        PuzzleManager.Instance.LayerCompleted += OnLayerCompleted;
    }

    void OnDestroy()
    {
        PlacementZone.ItemPlaced -= OnItemPlaced;
        if (PuzzleManager.Instance != null) PuzzleManager.Instance.LayerCompleted -= OnLayerCompleted;
    }

    void OnItemPlaced(Pickup item)
    {
        if (fillOnLayer == FillOnPlaced && item != pickup && item.itemId == pickup.itemId) Fill();
    }

    void OnLayerCompleted(int layer)
    {
        if (layer == fillOnLayer) Fill();
    }

    void Fill()
    {
        if (filled) return;
        filled = true;
        pickup.SetLocked(false);
        if (isActiveAndEnabled) StartCoroutine(FillAnimation());
        else SetContentColor(fullColor);
    }

    IEnumerator FillAnimation()
    {
        Vector3 scale = transform.localScale;

        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
        {
            SetContentColor(Color.Lerp(emptyColor, fullColor, t));
            transform.localScale = scale * (1f + 0.2f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        SetContentColor(fullColor);
        transform.localScale = scale;
    }

    // Tints only the bowl's content material, leaving any other materials alone.
    void SetContentColor(Color c)
    {
        block.SetColor(BaseColor, c);
        block.SetColor(LegacyColor, c);

        foreach (Renderer r in renderers)
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && mats[i].name.StartsWith(contentMaterialPrefix))
                    r.SetPropertyBlock(block, i);
        }
    }
}
