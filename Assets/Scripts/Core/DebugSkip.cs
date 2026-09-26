using UnityEngine;

// Editor testing helper. Not part of the game, and compiled out of release builds.
//   N  complete the current layer
//   R  restart the scene
//   T  add 60 seconds to the clock
public class DebugSkip : MonoBehaviour
{
    [SerializeField] bool enableDebugKeys = true;
    [SerializeField] GameTimer timer;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void Update()
    {
        if (!enableDebugKeys) return;

        if (Input.GetKeyDown(KeyCode.N))
        {
            PuzzleManager pm = PuzzleManager.Instance;
            Debug.Log($"[debug] skipping layer {pm.Completed}");
            pm.CompleteLayer(pm.Completed);
        }

        if (Input.GetKeyDown(KeyCode.R))
            PuzzleManager.Instance.Restart();

        if (Input.GetKeyDown(KeyCode.T) && timer != null)
            timer.Penalize(-60f);
    }
#endif
}
