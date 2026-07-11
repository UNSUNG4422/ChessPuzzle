using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float hoverOffsetY = 4f;
    [SerializeField] private float hoverScale = 1.04f;
    [SerializeField] private float pressedScale = 0.98f;
    [SerializeField] private float transitionDuration = 0.1f;

    private RectTransform cachedRectTransform;
    private Vector2 initialAnchoredPosition;
    private Vector3 initialScale;
    private bool isPointerOver;
    private bool isPointerDown;
    private bool initialized;

    private RectTransform Target
    {
        get
        {
            if (target != null)
            {
                return target;
            }

            if (cachedRectTransform == null)
            {
                cachedRectTransform = transform as RectTransform;
            }

            return cachedRectTransform;
        }
    }

    private void Awake()
    {
        CacheInitialTransform();
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            CacheInitialTransform();
        }

        isPointerOver = false;
        isPointerDown = false;
        ApplyStateImmediate();
    }

    private void OnDisable()
    {
        isPointerOver = false;
        isPointerDown = false;
        ApplyStateImmediate();
    }

    private void Update()
    {
        RectTransform effectTarget = Target;
        if (effectTarget == null)
        {
            return;
        }

        Vector2 targetPosition = GetTargetAnchoredPosition();
        Vector3 targetScale = GetTargetScale();
        float duration = Mathf.Max(0.001f, transitionDuration);
        float t = 1f - Mathf.Exp(-Time.unscaledDeltaTime / duration);

        effectTarget.anchoredPosition = Vector2.Lerp(effectTarget.anchoredPosition, targetPosition, t);
        effectTarget.localScale = Vector3.Lerp(effectTarget.localScale, targetScale, t);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        isPointerDown = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        isPointerOver = eventData != null && eventData.pointerEnter != null
            && (eventData.pointerEnter == gameObject || eventData.pointerEnter.transform.IsChildOf(transform));
    }

    private void CacheInitialTransform()
    {
        RectTransform effectTarget = Target;
        if (effectTarget == null)
        {
            return;
        }

        initialAnchoredPosition = effectTarget.anchoredPosition;
        initialScale = effectTarget.localScale;
        initialized = true;
    }

    private void ApplyStateImmediate()
    {
        RectTransform effectTarget = Target;
        if (effectTarget == null || !initialized)
        {
            return;
        }

        effectTarget.anchoredPosition = initialAnchoredPosition;
        effectTarget.localScale = initialScale;
    }

    private Vector2 GetTargetAnchoredPosition()
    {
        return isPointerOver
            ? initialAnchoredPosition + new Vector2(0f, hoverOffsetY)
            : initialAnchoredPosition;
    }

    private Vector3 GetTargetScale()
    {
        if (isPointerDown)
        {
            return initialScale * pressedScale;
        }

        return isPointerOver
            ? initialScale * hoverScale
            : initialScale;
    }
}
