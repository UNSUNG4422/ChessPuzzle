using UnityEngine;

public class CellView : MonoBehaviour
{
    public Vector2Int GridPosition;
    public bool IsActive = true;
    public bool IsCovered;
    public bool HasPiece;

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Overlays")]
    [SerializeField] private GameObject coverageOverlay;
    [SerializeField] private GameObject pieceOverlay;
    [SerializeField] private GameObject previewOverlay;

    [Header("Base Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;

    [Header("Coverage")]
    [SerializeField] private Color coverageOverlayColor = new Color(0f, 1f, 0f, 0.25f);

    private bool coverageVisible = true;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        UpdateVisual();
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        UpdateVisual();
    }

    private void OnMouseDown()
    {
        if (PuzzleManager.Instance == null)
        {
            Debug.LogWarning("PuzzleManager was not found in the scene.");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            PuzzleManager.Instance.OnCellLeftClicked(this);
        }
    }

    private void OnMouseOver()
    {
        if (PuzzleManager.Instance == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            PuzzleManager.Instance.OnCellRightClicked(this);
        }
    }

    private void OnMouseEnter()
    {
        if (PuzzleManager.Instance != null)
        {
            PuzzleManager.Instance.OnCellHoverEnter(this);
        }
    }

    private void OnMouseExit()
    {
        if (PuzzleManager.Instance != null)
        {
            PuzzleManager.Instance.OnCellHoverExit(this);
        }
    }

    public void SetCellColor(Color color)
    {
        normalColor = color;
        UpdateVisual();
    }

    public void SetActiveState(bool active)
    {
        IsActive = active;

        if (!IsActive)
        {
            IsCovered = false;
            HasPiece = false;
        }

        UpdateVisual();
    }

    public void SetCovered(bool covered)
    {
        IsCovered = covered;
        UpdateVisual();
    }

    public void SetPiece(bool hasPiece)
    {
        HasPiece = hasPiece;
        UpdateVisual();
    }

    public void SetCoverageVisible(bool visible)
    {
        coverageVisible = visible;
        UpdateVisual();
    }

    public void SetPreview(bool preview)
    {
        if (previewOverlay != null)
        {
            previewOverlay.SetActive(preview);
        }
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = IsActive ? normalColor : inactiveColor;
        }

        if (coverageOverlay != null)
        {
            ApplyOverlayColor(coverageOverlay, coverageOverlayColor);
            coverageOverlay.SetActive(IsActive && IsCovered && coverageVisible && !HasPiece);
        }

        if (pieceOverlay != null)
        {
            pieceOverlay.SetActive(IsActive && HasPiece);
        }

        if (previewOverlay != null && (!IsActive || HasPiece))
        {
            previewOverlay.SetActive(false);
        }
    }

    private void ApplyOverlayColor(GameObject overlayObject, Color color)
    {
        SpriteRenderer overlayRenderer = overlayObject.GetComponent<SpriteRenderer>();

        if (overlayRenderer != null)
        {
            overlayRenderer.color = color;
        }

        SpriteRenderer[] childRenderers = overlayObject.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer childRenderer in childRenderers)
        {
            if (childRenderer != null)
            {
                childRenderer.color = color;
            }
        }
    }
}
