using UnityEngine;

public class Door : MonoBehaviour
{
    public float openAngle = 100f;
    public float speed = 2f;

    Quaternion closedRotation;
    Quaternion openRotation;
    bool isOpen = false;

    void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    public void Open()
    {
        isOpen = true;
    }

    public void Close()
    {
        isOpen = false;
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    void Update()
    {
        Quaternion target = closedRotation;
        if (isOpen)
        {
            target = openRotation;
        }

        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, speed * Time.deltaTime);
    }
}
