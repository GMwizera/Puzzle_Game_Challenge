using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance;
    static bool introSeen = false;

    public int totalLayers = 5;
    public TMP_Text progressText;
    public GameObject winPanel;
    public GameObject losePanel;
    public TMP_Text loseReasonText;
    public Door diningDoor;
    public AudioSource audioSource;
    public AudioClip layerClip;
    public AudioClip cheerClip;
    public AudioClip loseClip;
    public Color progressFlashColor = new Color(0.55f, 1f, 0.55f);

    public GameObject introPanel;
    public TMP_Text objectiveText;
    public string[] layerGoals =
    {
        "Gather what the recipe calls for",
        "Prepare the cassava leaves",
        "Fire up the stove",
        "Cook: the order matters",
        "Serve the dish",
    };
    public string finalGoal = "Dinner is ready. Head to the dining room";

    public int completed = 0;

    bool waitingToStart = false;
    Color progressStartColor;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        completed = 0;
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        progressStartColor = progressText.color;
        UpdateProgress();

        if (introPanel != null && !introSeen)
        {
            introPanel.SetActive(true);
            waitingToStart = true;
            Time.timeScale = 0f;
        }
        else
        {
            if (introPanel != null)
            {
                introPanel.SetActive(false);
            }
            Time.timeScale = 1f;
        }
    }

    void Update()
    {
        if (!waitingToStart)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            waitingToStart = false;
            introSeen = true;
            StartCoroutine(StartGameNextFrame());
        }
    }

    IEnumerator StartGameNextFrame()
    {
        yield return null;
        introPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public bool IsPlaying()
    {
        return Time.timeScale > 0f;
    }

    public bool AllDone()
    {
        return completed >= totalLayers;
    }

    public bool IsActive(int layerIndex)
    {
        return IsPlaying() && layerIndex == completed;
    }

    public void CompleteLayer(int layerIndex)
    {
        if (!IsActive(layerIndex))
        {
            return;
        }

        completed = completed + 1;
        UpdateProgress();
        StartCoroutine(FlashProgress());

        if (AllDone())
        {
            diningDoor.Open();
            audioSource.PlayOneShot(cheerClip);
        }
        else
        {
            audioSource.PlayOneShot(layerClip);
        }
    }

    public void Win()
    {
        if (!AllDone() || !IsPlaying())
        {
            return;
        }
        EndGame(winPanel);
    }

    public void Lose(string reason)
    {
        if (!IsPlaying())
        {
            return;
        }
        loseReasonText.text = reason;
        audioSource.PlayOneShot(loseClip);
        EndGame(losePanel);
    }

    void EndGame(GameObject panel)
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(ShowPanel(panel));
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateProgress()
    {
        progressText.text = "Puzzle Progress: " + completed + " / " + totalLayers;

        if (objectiveText == null)
        {
            return;
        }

        if (AllDone())
        {
            objectiveText.text = finalGoal;
        }
        else if (completed < layerGoals.Length)
        {
            objectiveText.text = layerGoals[completed];
        }
        else
        {
            objectiveText.text = "";
        }
    }

    IEnumerator ShowPanel(GameObject panel)
    {
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = panel.AddComponent<CanvasGroup>();
        }

        panel.SetActive(true);

        float duration = 0.35f;
        float timer = 0f;
        while (timer < duration)
        {
            timer = timer + Time.unscaledDeltaTime;
            float amount = timer / duration;
            group.alpha = amount;
            float scale = Mathf.Lerp(0.92f, 1f, amount);
            panel.transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        group.alpha = 1f;
        panel.transform.localScale = Vector3.one;
    }

    IEnumerator FlashProgress()
    {
        float duration = 0.6f;
        float timer = 0f;
        while (timer < duration)
        {
            timer = timer + Time.deltaTime;
            float pulse = Mathf.Sin(timer / duration * Mathf.PI);
            float scale = 1f + 0.15f * pulse;
            progressText.transform.localScale = new Vector3(scale, scale, scale);
            progressText.color = Color.Lerp(progressStartColor, progressFlashColor, pulse);
            yield return null;
        }

        progressText.transform.localScale = Vector3.one;
        progressText.color = progressStartColor;
    }
}
