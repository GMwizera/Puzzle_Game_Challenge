using UnityEngine;

public class OpenOnUse : Interactable
{
    public Door door;
    public string closePrompt = "Close fridge";

    public override string GetPrompt()
    {
        if (door.IsOpen())
        {
            return closePrompt;
        }
        return prompt;
    }

    public override void Interact(PlayerInteractor player)
    {
        if (door.IsOpen())
        {
            door.Close();
        }
        else
        {
            door.Open();
        }
    }
}
