using TMPro;
using UnityEngine;

public class CellView : MonoBehaviour
{
    public Vector2Int GridPosition;
    public bool IsActive = true;
    public bool IsCovered;
    public bool HasPiece;
    public int CoverCount;
    public int RequiredCoverCount = 1;
    public bool RequiresExactCover;

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Overlays")]
    [SerializeField] private GameObject coverageOverlay;
    [SerializeField] private GameObject pieceOverlay;
    [SerializeField] private GameObject previewOverlay;
    [SerializeField] private GameObject exactCoverVisualRoot;
    [SerializeField] private GameObject exactCoverFrame;
    [SerializeField] private GameObject exactCoverTextRoot;
    [SerializeField] private TMP_Text exactCoverTextOutline;
    [SerializeField] private TMP_Text exactCoverText;
    [SerializeField] private TMP_FontAsset exactCoverFontAsset;
    [SerializeField] private Material exactCoverMaterialPreset;

    [Header("Base Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;

    [Header("Coverage")]
    [SerializeField] private Color coverageOverlayColor = new Color(0f, 1f, 0f, 0.25f);
    [SerializeField] private Color exactCoverFrameDefaultColor = Color.yellow;
    [SerializeField] private Color exactCoverFrameColor1 = Color.cyan;
    [SerializeField] private Color exactCoverFrameColor2 = Color.blue;
    [SerializeField] private Color exactCoverFrameColor3 = Color.red;
    [SerializeField] private Color exactCoverFrameColor4 = new Color(0.6f, 0.2f, 1f, 1f);
    [SerializeField] private Color exactCoverFrameColor5 = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color exactCoverTextColor = Color.white;
    [SerializeField] private Color exactCoverCompleteTextColor = new Color(0.8f, 1f, 0.8f, 1f);
    [SerializeField] private Color exactCoverOverTextColor = Color.red;
    [SerializeField] private float exactCoverTextMainFontSize = 4.5f;

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
            SetCoverCount(0);
            HasPiece = false;
        }

        UpdateVisual();
    }

    public void SetCovered(bool covered)
    {
        SetCoverCount(covered ? 1 : 0);
    }

    public void SetCoverCount(int coverCount)
    {
        CoverCount = Mathf.Max(0, coverCount);
        IsCovered = CoverCount > 0;
        UpdateVisual();
    }

    public void AddCoverCount(int amount = 1)
    {
        SetCoverCount(CoverCount + amount);
    }

    public void SetExactCoverRequirement(bool requiresExactCover, int requiredCoverCount)
    {
        RequiresExactCover = requiresExactCover;
        RequiredCoverCount = Mathf.Max(1, requiredCoverCount);
        UpdateVisual();
    }

    public bool IsClearConditionSatisfied()
    {
        if (!IsActive)
        {
            return true;
        }

        return RequiresExactCover
            ? CoverCount == RequiredCoverCount
            : CoverCount >= 1;
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

        UpdateExactCoverVisual();
    }

    private void UpdateExactCoverVisual()
    {
        bool showExactCover = IsActive && RequiresExactCover;
        int remainingCoverCount = RequiredCoverCount - CoverCount;
        string remainingCoverCountText = remainingCoverCount.ToString();

        if (showExactCover)
        {
            EnsureExactCoverVisuals();
        }

        if (exactCoverVisualRoot != null)
        {
            exactCoverVisualRoot.SetActive(showExactCover);
        }

        if (exactCoverFrame != null)
        {
            exactCoverFrame.SetActive(showExactCover);
            ApplyOverlayColor(exactCoverFrame, GetExactCoverFrameColor());
        }

        if (exactCoverText != null)
        {
            exactCoverText.gameObject.SetActive(showExactCover);
            exactCoverText.text = remainingCoverCountText;
            exactCoverText.color = GetExactCoverTextColor(remainingCoverCount);
            ApplyExactCoverTextStyle(exactCoverText, exactCoverTextMainFontSize, 9);
        }

        if (exactCoverTextOutline != null)
        {
            exactCoverTextOutline.gameObject.SetActive(false);
        }

    }

    private Color GetExactCoverTextColor(int remainingCoverCount)
    {
        if (remainingCoverCount < 0)
        {
            return exactCoverOverTextColor;
        }

        if (remainingCoverCount == 0)
        {
            return exactCoverCompleteTextColor;
        }

        return exactCoverTextColor;
    }

    private Color GetExactCoverFrameColor()
    {
        switch (RequiredCoverCount)
        {
            case 1:
                return exactCoverFrameColor1;
            case 2:
                return exactCoverFrameColor2;
            case 3:
                return exactCoverFrameColor3;
            case 4:
                return exactCoverFrameColor4;
            case 5:
                return exactCoverFrameColor5;
            default:
                return exactCoverFrameDefaultColor;
        }
    }

    private void ApplyExactCoverTextStyle(TMP_Text text, float fontSize, int sortingOrder)
    {
        text.fontSize = fontSize;
        text.fontStyle |= FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.horizontalAlignment = HorizontalAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;

        text.transform.localPosition = Vector3.zero;
        text.transform.localRotation = Quaternion.identity;
        text.transform.localScale = Vector3.one;

        MeshRenderer textRenderer = text.GetComponent<MeshRenderer>();

        if (textRenderer != null)
        {
            textRenderer.sortingOrder = sortingOrder;
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

        LineRenderer lineRenderer = overlayObject.GetComponent<LineRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }

    private void EnsureExactCoverVisuals()
    {
        if (exactCoverVisualRoot == null)
        {
            exactCoverVisualRoot = exactCoverTextRoot != null
                ? exactCoverTextRoot
                : CreateDefaultExactCoverVisualRoot();
        }

        if (exactCoverFrame == null)
        {
            exactCoverFrame = CreateDefaultExactCoverFrame();
        }

        if (exactCoverText == null)
        {
            exactCoverText = CreateDefaultExactCoverText("ExactCoverText", exactCoverTextMainFontSize, 9);
        }

        ConfigureExactCoverVisualHierarchy();
    }

    private GameObject CreateDefaultExactCoverFrame()
    {
        GameObject frameObject = new GameObject("ExactCoverFrame");
        frameObject.transform.SetParent(exactCoverVisualRoot != null ? exactCoverVisualRoot.transform : transform, false);
        frameObject.transform.localPosition = Vector3.zero;
        frameObject.transform.localRotation = Quaternion.identity;
        frameObject.transform.localScale = Vector3.one;

        LineRenderer lineRenderer = frameObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.positionCount = 5;
        lineRenderer.startWidth = 0.07f;
        lineRenderer.endWidth = 0.07f;
        lineRenderer.sortingOrder = 7;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        ApplyExactCoverFrameShape(lineRenderer);

        return frameObject;
    }

    private GameObject CreateDefaultExactCoverVisualRoot()
    {
        GameObject rootObject = new GameObject("ExactCoverVisualRoot");
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        exactCoverTextRoot = rootObject;
        return rootObject;
    }

    private TMP_Text CreateDefaultExactCoverText(string objectName, float fontSize, int sortingOrder)
    {
        GameObject textObject = new GameObject(objectName);
        Transform parent = exactCoverVisualRoot != null ? exactCoverVisualRoot.transform : transform;
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = Vector3.zero;
        textObject.transform.localScale = Vector3.one;

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.horizontalAlignment = HorizontalAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = false;
        text.text = (RequiredCoverCount - CoverCount).ToString();

        RectTransform rectTransform = text.rectTransform;
        rectTransform.sizeDelta = new Vector2(1f, 1f);

        MeshRenderer textRenderer = text.GetComponent<MeshRenderer>();

        if (textRenderer != null)
        {
            textRenderer.sortingOrder = sortingOrder;
        }

        ApplyExactCoverTextAssets(text);
        return text;
    }

    private void ConfigureExactCoverVisualHierarchy()
    {
        Transform rootTransform = exactCoverVisualRoot != null ? exactCoverVisualRoot.transform : null;

        ConfigureExactCoverTransform(rootTransform, transform);
        GameObject previousTextRoot = exactCoverTextRoot;
        exactCoverTextRoot = exactCoverVisualRoot;

        if (exactCoverVisualRoot != null)
        {
            exactCoverVisualRoot.name = "ExactCoverVisualRoot";
        }

        ConfigureExactCoverTransform(exactCoverFrame != null ? exactCoverFrame.transform : null, rootTransform);
        ConfigureExactCoverTransform(exactCoverText != null ? exactCoverText.transform : null, rootTransform);

        if (exactCoverText != null)
        {
            ApplyExactCoverTextAssets(exactCoverText);
        }

        if (exactCoverTextOutline != null)
        {
            exactCoverTextOutline.gameObject.SetActive(false);
        }

        if (previousTextRoot != null && previousTextRoot != exactCoverVisualRoot)
        {
            previousTextRoot.SetActive(false);
        }

        LineRenderer lineRenderer = exactCoverFrame != null ? exactCoverFrame.GetComponent<LineRenderer>() : null;

        if (lineRenderer != null)
        {
            ApplyExactCoverFrameShape(lineRenderer);
        }
    }

    private void ConfigureExactCoverTransform(Transform targetTransform, Transform parent)
    {
        if (targetTransform == null || parent == null)
        {
            return;
        }

        if (targetTransform.parent != parent)
        {
            targetTransform.SetParent(parent, false);
        }

        targetTransform.localPosition = Vector3.zero;
        targetTransform.localRotation = Quaternion.identity;
        targetTransform.localScale = Vector3.one;
    }

    private void ApplyExactCoverFrameShape(LineRenderer lineRenderer)
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.positionCount = 5;
        lineRenderer.sortingOrder = 7;
        lineRenderer.SetPosition(0, new Vector3(-0.48f, -0.48f, 0f));
        lineRenderer.SetPosition(1, new Vector3(-0.48f, 0.48f, 0f));
        lineRenderer.SetPosition(2, new Vector3(0.48f, 0.48f, 0f));
        lineRenderer.SetPosition(3, new Vector3(0.48f, -0.48f, 0f));
        lineRenderer.SetPosition(4, new Vector3(-0.48f, -0.48f, 0f));
    }

    private void ApplyExactCoverTextAssets(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        if (exactCoverFontAsset != null)
        {
            text.font = exactCoverFontAsset;
        }

        if (exactCoverMaterialPreset != null)
        {
            text.fontSharedMaterial = exactCoverMaterialPreset;
        }
    }
}
