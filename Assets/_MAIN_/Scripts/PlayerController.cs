using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Transform cameraTransform;
    public AudioSource footSteps;
    private Rigidbody rb;

    public string gameScene, hall1Scene, hall2Scene;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody component on the same GameObject.");
        }
    }

    private void Update()
    {
        // Get joystick input
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");

        // Get camera forward and right vectors, flattened to the XZ plane
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        // Movement direction
        Vector3 moveDir = camForward * verticalInput + camRight * horizontalInput;

        // Move player using Rigidbody
        if (rb != null)
        {
            Vector3 newPosition = rb.position + moveDir * moveSpeed * Time.deltaTime;
            rb.MovePosition(newPosition);
        }
    }

    public void OnPointerEnter()
    {
        // ...existing code...
    }

    public void OnPointerExit()
    {
        // ...existing code...
    }

    public void OnPointerClick()
    {
        // ...existing code...
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.name.Equals("_Exit"))
        {
#if UNITY_EDITOR 
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        else if (other.gameObject.name.Equals(gameScene))
        {
            SceneManager.LoadScene(gameScene);
        }
        else if (other.gameObject.name.Equals(hall1Scene))
        {
            SceneManager.LoadScene(hall1Scene);
        }
        else if (other.gameObject.name.Equals(hall2Scene))
        {
            SceneManager.LoadScene(hall2Scene);
        }
    }
}
