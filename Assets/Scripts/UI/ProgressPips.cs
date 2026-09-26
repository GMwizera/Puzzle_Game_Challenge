using UnityEngine;
using UnityEngine.UI;

// A row of dots under the progress text: filled for solved layers, a pulsing ring for the current one.
// Builds its own child images at start, so it only needs a RectTransform and two sprites.
[RequireComponent(typeof(RectTransform))]
public class ProgressPips : MonoBehaviour
{
    [SerializeField] Sprite dotSprite;
    [SerializeField] Sprite ringSprite;
    [SerializeField] float size = 18f;
    [SerializeField] float spacing = 12f;
    [SerializeField] Color doneColor = new Color(0.55f, 1f, 0.55f);
    [SerializeField] Color currentColor = new Color(1f, 0.82f, 0.35f);
    [SerializeField] Color pendingColor = new Color(1f, 1f, 1f, 0.3f);

    Image[] pips;

    void Start()
    {
        int count = PuzzleManager.Instance.TotalLayers;
        pips = new Image[count];
        float width = count * size + (count - 1) * spacing;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Pip{i}", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(-width / 2f + size / 2f + i * (size + spacing), 0f);
            pips[i] = go.GetComponent<Image>();
            pips[i].raycastTarget = false;
        }
    }

    void Update()
    {
        int completed = PuzzleManager.Instance.Completed;

        for (int i = 0; i < pips.Length; i++)
        {
            Image pip = pips[i];
            bool current = i == completed;

            pip.sprite = i < completed ? dotSprite : ringSprite;
            pip.color = i < completed ? doneColor : current ? currentColor : pendingColor;

            float pulse = current ? 1f + 0.2f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) : 1f;
            pip.transform.localScale = Vector3.one * pulse;
        }
    }
}
