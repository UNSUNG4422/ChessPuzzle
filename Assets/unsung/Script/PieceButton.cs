using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PieceButton : MonoBehaviour
{
    [SerializeField] private PieceData pieceData;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = Color.yellow;
    [SerializeField] private Color disabledButtonColor = Color.gray;

    private Button button;

    public PieceData PieceData => pieceData;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
        RefreshView();
    }

    private void Start()
    {
        RefreshView();
    }

    private void OnValidate()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        RefreshView();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    public void RefreshView()
    {
        int remainingCount = GetRemainingCount();
        string displayName = GetDisplayName();

        if (iconImage != null)
        {
            iconImage.sprite = pieceData != null ? pieceData.icon : null;
            iconImage.enabled = pieceData != null && pieceData.icon != null;
        }

        if (nameText != null)
        {
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.text = $"{displayName}\nx{remainingCount}";
        }

        if (button != null)
        {
            button.interactable = pieceData != null && remainingCount > 0;
        }

        SetSelectedVisual(false);
    }

    public void SetSelectedVisual(bool selected)
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (backgroundImage == null)
        {
            return;
        }

        if (pieceData != null && GetRemainingCount() <= 0)
        {
            backgroundImage.color = disabledButtonColor;
            return;
        }

        backgroundImage.color = selected ? selectedButtonColor : normalButtonColor;
    }

    private void OnButtonClicked()
    {
        if (pieceData == null)
        {
            Debug.LogWarning($"{name} has no PieceData assigned.");
            RefreshView();
            return;
        }

        if (PuzzleManager.Instance == null)
        {
            Debug.LogWarning("PuzzleManager was not found in the scene.");
            return;
        }

        if (!PuzzleManager.Instance.CanUsePiece(pieceData))
        {
            Debug.LogWarning($"{GetDisplayName()} has no remaining stock.");
            RefreshView();
            return;
        }

        PuzzleManager.Instance.SelectPiece(pieceData);
    }

    private int GetRemainingCount()
    {
        if (pieceData == null || PuzzleManager.Instance == null)
        {
            return 0;
        }

        return PuzzleManager.Instance.GetRemainingCount(pieceData);
    }

    private string GetDisplayName()
    {
        if (pieceData == null)
        {
            return "None";
        }

        if (!string.IsNullOrEmpty(pieceData.displayName))
        {
            return pieceData.displayName;
        }

        return pieceData.pieceType.ToString();
    }
}
