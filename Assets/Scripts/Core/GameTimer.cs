using System.Collections;
using UnityEngine;
using TMPro;

// Lose condition: the meal burns when time runs out.
// Wrong answers call Penalize, which shakes the clock so the player sees what it cost.
public class GameTimer : MonoBehaviour
{
    [SerializeField] float startSeconds = 360f;
    [SerializeField] float warningSeconds = 60f;
    [SerializeField] TMP_Text timerText;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color warningColor = Color.red;

    float remaining;
    bool penaltyFlashing;

    public float Remaining => remaining;

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

        if (penaltyFlashing) return;

        // In the last minute the clock turns red and beats once a second.
        bool warning = remaining < warningSeconds;
        timerText.color = warning ? warningColor : normalColor;
        float beat = warning ? 1f + 0.08f * Mathf.Abs(Mathf.Sin(remaining * Mathf.PI)) : 1f;
        timerText.transform.localScale = Vector3.one * beat;
    }

    public void Penalize(float seconds)
    {
        remaining -= seconds;
        if (seconds > 0f && isActiveAndEnabled) StartCoroutine(PenaltyFlash());
    }

    IEnumerator PenaltyFlash()
    {
        penaltyFlashing = true;
        Transform t = timerText.transform;
        Vector3 origin = t.localPosition;

        for (float time = 0f; time < 1f; time += Time.deltaTime / 0.5f)
        {
            float strength = 1f - time;
            t.localPosition = origin + (Vector3)Random.insideUnitCircle * 6f * strength;
            t.localScale = Vector3.one * (1f + 0.25f * strength);
            timerText.color = warningColor;
            yield return null;
        }

        t.localPosition = origin;
        t.localScale = Vector3.one;
        penaltyFlashing = false;
    }
}
