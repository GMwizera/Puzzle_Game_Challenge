using UnityEngine;

// Trigger box just past the door. Walking through it after all layers wins the game.
[RequireComponent(typeof(Collider))]
public class ExitTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            PuzzleManager.Instance.Win();
    }
}
