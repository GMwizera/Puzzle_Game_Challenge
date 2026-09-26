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

    public int Completed { get; private set; }
    public bool AllComplete => Completed >= totalLayers;

    void Awake() => Instance = this;

    void Start()
    {
        Time.timeScale = 1f;
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        UpdateProgress();
    }

    // A puzzle only responds when it is the current layer and the game is running.
    public bool IsActive(int layerIndex) => Time.timeScale > 0f && layerIndex == Completed;

    public void CompleteLayer(int layerIndex)
    {
        if (!IsActive(layerIndex)) return;

        Completed++;
        UpdateProgress();

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
        if (!AllComplete || Time.timeScale == 0f) return;
        EndGame(winPanel);
    }

    public void Lose(string reason)
    {
        if (Time.timeScale == 0f) return;
        loseReasonText.text = reason;
        audioSource.PlayOneShot(loseClip);
        EndGame(losePanel);
    }

    void EndGame(GameObject panel)
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Hook this to the Restart buttons on both panels.
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateProgress() => progressText.text = $"Puzzle Progress: {Completed} / {totalLayers}";
}
