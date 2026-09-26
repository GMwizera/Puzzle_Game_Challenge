using UnityEngine;
using UnityEngine.UI;

public class ProgressPips : MonoBehaviour
{
    public Sprite dotSprite;
    public Sprite ringSprite;
    public float size = 18f;
    public float spacing = 12f;
    public Color doneColor = new Color(0.55f, 1f, 0.55f);
    public Color currentColor = new Color(1f, 0.82f, 0.35f);
    public Color pendingColor = new Color(1f, 1f, 1f, 0.3f);

    Image[] pips;

    void Start()
    {
        int count = PuzzleManager.Instance.totalLayers;
        pips = new Image[count];
        float totalWidth = count * size + (count - 1) * spacing;

        for (int i = 0; i < count; i++)
        {
            GameObject pip = new GameObject("Pip" + i, typeof(RectTransform));
            pip.transform.SetParent(transform, false);

            Image image = pip.AddComponent<Image>();
            image.raycastTarget = false;

            RectTransform rect = pip.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);
            float x = -totalWidth / 2f + size / 2f + i * (size + spacing);
            rect.anchoredPosition = new Vector2(x, 0f);

            pips[i] = image;
        }
    }

    void Update()
    {
        int completed = PuzzleManager.Instance.completed;

        for (int i = 0; i < pips.Length; i++)
        {
            Image pip = pips[i];
            float scale = 1f;

            if (i < completed)
            {
                pip.sprite = dotSprite;
                pip.color = doneColor;
            }
            else if (i == completed)
            {
                pip.sprite = ringSprite;
                pip.color = currentColor;
                scale = 1f + 0.2f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
            }
            else
            {
                pip.sprite = ringSprite;
                pip.color = pendingColor;
            }

            pip.transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
