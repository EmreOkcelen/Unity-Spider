using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class CameraController : MonoBehaviour
{
    public Transform playerRoot; // usually the Player transform
    public Transform cameraTransform; // usually the child camera

    public float mouseSensitivity = 1.8f;
    public float pitchMin = -40f;
    public float pitchMax = 60f;

    public bool lockCursor = false; // default false -> cursor visible

    private float yaw;
    private float pitch;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        // Cursor handling: sadece lockCursor true ise kilitle ve gizle
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Update()
    {
        // sadece sað tuþ basýlýyken kamera dönebilsin
        if (Input.GetMouseButton(1)) // 1 = sað mouse tuþu
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
