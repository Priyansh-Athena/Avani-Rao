using UnityEngine;
using UnityEngine.Events;

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

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    /// <summary>
    /// Called automatically by CardboardReticlePointer
    /// when the gaze enters this object's collider.
    /// </summary>
    public void OnPointerEnter()
    {
        if (!isInteractable)
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
        if (!isInteractable)
        {
            return;
        }

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

        if (!isInteractable)
        {
            transform.localScale = originalScale;
        }
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
    }
}