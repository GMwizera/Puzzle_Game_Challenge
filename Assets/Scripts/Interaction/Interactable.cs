using UnityEngine;

// Base class for anything the player can use.
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] string prompt = "Use";

    public virtual string Prompt => prompt;

    public abstract void Interact(PlayerInteractor player);
}
