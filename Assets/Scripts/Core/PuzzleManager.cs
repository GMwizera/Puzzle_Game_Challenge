using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Tracks the 5 layers, updates the progress UI, and handles win/lose.
// Layers must be solved in order: 0 Gather, 1 Pound, 2 Stove, 3 Cook, 4 Serve.
public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    [SerializeField] int totalLayers = 5;
    [SerializeField] TMP_Text progressText;
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;
    [SerializeField] TMP_Text loseReasonText;
    [SerializeField] Door diningDoor;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip layerClip, cheerClip, loseClip;
    [SerializeField] Color progressFlashColor = new Color(0.55f, 1f, 0.55f);

    [Header("Guidance")]
    [SerializeField] GameObject introPanel;
    [SerializeField] TMP_Text objectiveText;
    // What each step is about, never how to solve it. The room's clues do that.
    [SerializeField] string[] layerGoals =
    {
        "Gather what the recipe calls for",
        "Prepare the cassava leaves",
        "Fire up the stove",
        "Cook: the order matters",
        "Serve the dish",
    };
    [SerializeField] string finalGoal = "Dinner is ready. Head to the dining room";

    // Raised with the index of the layer that was just solved. Clues and HUD listen to this.
    public event Action<int> LayerCompleted;

    public int Completed { get; private set; }
    public int TotalLayers => totalLayers;
    public bool AllComplete => Completed >= totalLayers;
    public bool IsPlaying => Time.timeScale > 0f;

    Color progressBaseColor;
    bool waitingToStart;
    static bool introSeen; // Restart skips the intro card

    void Awake() => Instance = this;

    void Start()
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        progressBaseColor = progressText.color;
        UpdateProgress();

        // The intro card holds the game (and the clock) until the player is ready.
        waitingToStart = introPanel != null && !introSeen;
        if (introPanel != null) introPanel.SetActive(waitingToStart);
        Time.timeScale = waitingToStart ? 0f : 1f;
    }

    void Update()
    {
        if (!waitingToStart) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            waitingToStart = false;
            introSeen = true;
            StartCoroutine(BeginAfterFrame());
        }
    }

    // Waits one frame so the key that closed the intro is not also used as an interaction.
    IEnumerator BeginAfterFrame()
    {
        yield return null;
        introPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // A puzzle only responds when it is the current layer and the game is running.
    public bool IsActive(int layerIndex) => IsPlaying && layerIndex == Completed;

    public void CompleteLayer(int layerIndex)
    {
        if (!IsActive(layerIndex)) return;

        Completed++;
        UpdateProgress();
        StartCoroutine(FlashProgress());
        LayerCompleted?.Invoke(layerIndex);

        if (AllComplete)
        {
            diningDoor.Open();
            audioSource.PlayOneShot(cheerClip);
        }
        else
        {
            audioSource.PlayOneShot(layerClip);
        }
    }

    // Called by ExitTrigger when the player walks into the dining room.
    public void Win()
    {
        if (!AllComplete || !IsPlaying) return;
        EndGame(winPanel);
    }

    public void Lose(string reason)
    {
        if (!IsPlaying) return;
        loseReasonText.text = reason;
        audioSource.PlayOneShot(loseClip);
        EndGame(losePanel);
    }

    void EndGame(GameObject panel)
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(RevealPanel(panel));
    }

    // Hook this to the Restart buttons on both panels.
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateProgress()
    {
        progressText.text = $"Puzzle Progress: {Completed} / {totalLayers}";

        if (objectiveText != null)
            objectiveText.text = AllComplete ? finalGoal
                : Completed < layerGoals.Length ? layerGoals[Completed] : "";
    }

    // Fades and scales the end panel in. Uses unscaled time because the game is paused.
    IEnumerator RevealPanel(GameObject panel)
    {
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();

        panel.SetActive(true);
        Transform t = panel.transform;

        for (float time = 0f; time < 1f; time += Time.unscaledDeltaTime / 0.35f)
        {
            float eased = 1f - Mathf.Pow(1f - time, 3f);
            group.alpha = eased;
            t.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);
            yield return null;
        }

        group.alpha = 1f;
        t.localScale = Vector3.one;
    }

    // Brief colour and scale pop on the progress counter when a layer is solved.
    IEnumerator FlashProgress()
    {
        Transform t = progressText.transform;

        for (float time = 0f; time < 1f; time += Time.deltaTime / 0.6f)
        {
            float pulse = Mathf.Sin(time * Mathf.PI);
            t.localScale = Vector3.one * (1f + 0.15f * pulse);
            progressText.color = Color.Lerp(progressBaseColor, progressFlashColor, pulse);
            yield return null;
        }

        t.localScale = Vector3.one;
        progressText.color = progressBaseColor;
    }
}
