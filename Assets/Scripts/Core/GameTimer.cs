using UnityEngine;
using TMPro;

// Lose condition: the meal burns when time runs out.
public class GameTimer : MonoBehaviour
{
    [SerializeField] float startSeconds = 360f;
    [SerializeField] TMP_Text timerText;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color warningColor = Color.red;

    float remaining;

    void Start() => remaining = startSeconds;

    void Update()
    {
        if (PuzzleManager.Instance.AllComplete) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            remaining = 0f;
            PuzzleManager.Instance.Lose("The isombe burned!");
        }

        int minutes = (int)remaining / 60;
        int seconds = (int)remaining % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
        timerText.color = remaining < 60f ? warningColor : normalColor;
    }

    public void Penalize(float seconds) => remaining -= seconds;
}
