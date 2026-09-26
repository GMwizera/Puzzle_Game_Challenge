using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string prompt = "Use";

    public virtual string GetPrompt()
    {
        return prompt;
    }

    public virtual void Interact(PlayerInteractor player)
    {
    }
}
