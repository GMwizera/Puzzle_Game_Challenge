using UnityEngine;

public class DebugSkip : MonoBehaviour
{
    public bool enableDebugKeys = true;
    public GameTimer timer;

    void Update()
    {
        if (!Application.isEditor || !enableDebugKeys)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            PuzzleManager.Instance.CompleteLayer(PuzzleManager.Instance.completed);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            PuzzleManager.Instance.Restart();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            timer.Penalize(-60f);
        }
    }
}
