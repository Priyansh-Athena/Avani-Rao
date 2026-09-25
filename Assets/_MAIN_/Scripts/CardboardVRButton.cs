using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class CardboardVRButton : MonoBehaviour
{
    [Header("Button Events")]
    [SerializeField] private UnityEvent onClick;
    [SerializeField] private UnityEvent onPointerEnter;
    [SerializeField] private UnityEvent onPointerExit;

    [Header("Optional Visual Feedback")]
    [SerializeField] private bool useHoverScale = true;

    [Range(1f, 1.5f)]
    [SerializeField] private float hoverScaleMultiplier = 1.08f;

    private Vector3 originalScale;
    private bool isInteractable = true;
    private bool isHovered;
    private Button uiButton;
    private int lastClickFrame = -1;

    [Header("Controller Input")]
    [SerializeField] private KeyCode primaryControllerButton = KeyCode.Joystick1Button0;
    [SerializeField] private KeyCode alternateControllerButton = KeyCode.Joystick1Button14;
    [Tooltip("Enable for controllers that send a left mouse click.")]
    [SerializeField] private bool acceptMouseClick = true;

    private bool CanClick()
    {
        return isActiveAndEnabled && isInteractable &&
            (uiButton == null || (uiButton.isActiveAndEnabled && uiButton.IsInteractable()));
    }

    private void LateUpdate()
    {
        // CardboardReticlePointer updates the gaze target in Update.
        if (!isHovered || !CanClick()) return;

        if (Input.GetKeyDown(primaryControllerButton) ||
            Input.GetKeyDown(alternateControllerButton) ||
            (acceptMouseClick && Input.GetMouseButtonDown(0)))
        {
            OnPointerClick();
        }
    }

    private void Awake()
    {
        originalScale = transform.localScale;
        uiButton = GetComponent<Button>();
    }

    /// <summary>
    /// Called automatically by CardboardReticlePointer
    /// when the gaze enters this object's collider.
    /// </summary>
    public void OnPointerEnter()
    {
        isHovered = true;
        if (!CanClick())
        {
            return;
        }

        if (useHoverScale)
        {
            transform.localScale =
                originalScale * hoverScaleMultiplier;
        }

        onPointerEnter?.Invoke();
    }

    /// <summary>
    /// Called automatically by CardboardReticlePointer
    /// when the gaze leaves this object's collider.
    /// </summary>
    public void OnPointerExit()
    {
        isHovered = false;
        if (useHoverScale)
        {
            transform.localScale = originalScale;
        }

        onPointerExit?.Invoke();
    }

    /// <summary>
    /// Called automatically by CardboardReticlePointer
    /// when the Cardboard trigger is pressed while looking at this object.
    /// </summary>
    public void OnPointerClick()
    {
        if (!CanClick())
        {
            return;
        }

        if (lastClickFrame == Time.frameCount) return;
        lastClickFrame = Time.frameCount;
        onClick?.Invoke();
    }

    /// <summary>
    /// Can also be called manually from another UnityEvent.
    /// </summary>
    public void ClickButton()
    {
        OnPointerClick();
    }

    public void SetInteractable(bool value)
    {
        isInteractable = value;

        if (!CanClick())
        {
            transform.localScale = originalScale;
        }
    }

    private void OnDisable()
    {
        isHovered = false;
        transform.localScale = originalScale;
    }
}