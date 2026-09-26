using System.Collections;
using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public float startSeconds = 360f;
    public float warningSeconds = 60f;
    public TMP_Text timerText;
    public Color normalColor = Color.white;
    public Color warningColor = Color.red;

    float remaining;
    bool flashing = false;

    void Start()
    {
        remaining = startSeconds;
    }

    void Update()
    {
        if (PuzzleManager.Instance.AllDone())
        {
            return;
        }

        remaining = remaining - Time.deltaTime;
        if (remaining <= 0f)
        {
            remaining = 0f;
            PuzzleManager.Instance.Lose("The isombe burned!");
        }

        int minutes = (int)remaining / 60;
        int seconds = (int)remaining % 60;
        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");

        if (flashing)
        {
            return;
        }

        if (remaining < warningSeconds)
        {
            timerText.color = warningColor;
            float pulse = 1f + 0.08f * Mathf.Abs(Mathf.Sin(remaining * Mathf.PI));
            timerText.transform.localScale = new Vector3(pulse, pulse, pulse);
        }
        else
        {
            timerText.color = normalColor;
            timerText.transform.localScale = Vector3.one;
        }
    }

    public void Penalize(float seconds)
    {
        remaining = remaining - seconds;
        if (seconds > 0f)
        {
            StartCoroutine(Flash());
        }
    }

    IEnumerator Flash()
    {
        flashing = true;
        Vector3 startPosition = timerText.transform.localPosition;
        timerText.color = warningColor;

        float duration = 0.5f;
        float timer = 0f;
        while (timer < duration)
        {
            timer = timer + Time.deltaTime;
            float strength = 1f - timer / duration;
            Vector3 shake = Random.insideUnitCircle * 6f * strength;
            timerText.transform.localPosition = startPosition + shake;
            float scale = 1f + 0.25f * strength;
            timerText.transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        timerText.transform.localPosition = startPosition;
        timerText.transform.localScale = Vector3.one;
        flashing = false;
    }
}
