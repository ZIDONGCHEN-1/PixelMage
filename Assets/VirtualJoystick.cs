using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI")]
    public RectTransform background;   // 外圈
    public RectTransform handle;       // 内圈

    [Header("Settings")]
    public float handleRange = 60f;    // 内圈可移动半径（像素）

    private Canvas canvas;
    private Camera uiCamera;
    private Vector2 inputVector = Vector2.zero;

    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;
    public Vector2 Direction => inputVector;
    public bool HasInput => inputVector.sqrMagnitude > 0.01f;

    void Awake()
    {
        if (background == null)
            background = GetComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                uiCamera,
                out localPoint))
        {
            Vector2 radius = background.sizeDelta * 0.5f;

            // 按背景尺寸归一化
            float x = localPoint.x / radius.x;
            float y = localPoint.y / radius.y;

            inputVector = new Vector2(x, y);

            // 限制在圆形范围内
            if (inputVector.magnitude > 1f)
                inputVector = inputVector.normalized;

            handle.anchoredPosition = inputVector * handleRange;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    public void ResetJoystick()
    {
        inputVector = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
}