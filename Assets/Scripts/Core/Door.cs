using UnityEngine;

// Put this on an empty "Hinge" object, with the door mesh as its child.
public class Door : MonoBehaviour
{
    [SerializeField] float openAngle = 100f;
    [SerializeField] float speed = 2f;

    Quaternion openRotation;
    bool opening;

    void Start() => openRotation = transform.localRotation * Quaternion.Euler(0f, openAngle, 0f);

    public void Open() => opening = true;

    void Update()
    {
        if (opening)
            transform.localRotation = Quaternion.Slerp(transform.localRotation, openRotation, speed * Time.deltaTime);
    }
}
