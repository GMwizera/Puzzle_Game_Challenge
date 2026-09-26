using UnityEngine;

// Simple first-person movement: WASD to move, mouse to look.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float gravity = -9.81f;

    CharacterController controller;
    float pitch;
    float verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // Look
        transform.Rotate(0f, Input.GetAxis("Mouse X") * mouseSensitivity, 0f);
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Move
        Vector3 move = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((move * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }
}
