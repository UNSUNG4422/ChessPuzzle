using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance;

    [SerializeField] private List<CellView> cells = new List<CellView>();
    [SerializeField] private List<StagePieceStock> pieceStocks = new List<StagePieceStock>();
    [SerializeField] private Transform placedPieceRoot;
    [SerializeField] private GameObject placedPiecePrefab;
    [SerializeField] private TextMeshProUGUI clearText;
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private GameObject clearDisplayObject;
    [SerializeField] private GameObject nextStageButton;
    [SerializeField] private bool coverageVisible = true;
    [SerializeField] private TextMeshProUGUI coverageToggleLabel;
    [SerializeField] private Image coverageToggleButtonImage;
    [SerializeField] private Color coverageButtonOnColor = new Color(0.3f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color coverageButtonOffColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private StageLoader stageLoader;
    [SerializeField] private PieceData selectedPiece;
    [SerializeField] private AudioClip selectPieceSE;
    [SerializeField, Range(0f, 2f)] private float selectPieceSEVolume = 1f;
    [SerializeField] private AudioClip placePieceSE;
    [SerializeField, Range(0f, 2f)] private float placePieceSEVolume = 1f;
    [SerializeField] private AudioClip removePieceSE;
    [SerializeField, Range(0f, 2f)] private float removePieceSEVolume = 1f;
    [SerializeField] private AudioClip undoSE;
    [SerializeField, Range(0f, 2f)] private float undoSEVolume = 1f;
    [SerializeField] private AudioClip resetSE;
    [SerializeField, Range(0f, 2f)] private float resetSEVolume = 1f;
    [SerializeField] private AudioClip clearSE;
    [SerializeField, Range(0f, 2f)] private float clearSEVolume = 1f;

    private readonly Dictionary<Vector2Int, CellView> cellMap = new Dictionary<Vector2Int, CellView>();
    private readonly Dictionary<PieceData, int> remainingPieces = new Dictionary<PieceData, int>();
    private readonly List<PlacedPieceRecord> placedPieces = new List<PlacedPieceRecord>();
    private readonly List<CellView> previewCells = new List<CellView>();
    private readonly List<PuzzleActionRecord> actionHistory = new List<PuzzleActionRecord>();
    private bool isCoverageTemporarilyInverted;

    private enum PuzzleActionType
    {
        Place,
        Remove
    }

    private class PlacedPieceRecord
    {
        public PieceData pieceData;
        public CellView cell;
        public GameObject placedObject;
    }

    private class PuzzleActionRecord
    {
        public PuzzleActionType actionType;
        public PieceData pieceData;
        public CellView cell;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PuzzleManager instances found. Disabling this one.");
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        BuildCellMap();
        InitializeRemainingPieces();

        SetClearDisplayVisible(false);
        SetNextStageButtonVisible(false);
        RefreshCoverageVisibility();
        RefreshCoverageToggleButtonView();
        RefreshAllPieceButtons();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SetCoverageTemporarilyInverted(true);
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            SetCoverageTemporarilyInverted(false);
        }
    }

    public void ToggleCoverageVisible()
    {
        SetCoverageVisible(!coverageVisible);
    }

    public void SetCoverageVisible(bool visible)
    {
        coverageVisible = visible;
        RefreshCoverageVisibility();
        RefreshCoverageToggleButtonView();
    }

    public void SelectPiece(PieceData pieceData)
    {
        if (pieceData == null)
        {
            Debug.LogWarning("Cannot select a null PieceData.");
            return;
        }

        if (!CanUsePiece(pieceData))
        {
            Debug.LogWarning($"{pieceData.displayName} has no remaining stock.");
            RefreshAllPieceButtons();
            return;
        }

        selectedPiece = pieceData;
        PlaySE(selectPieceSE, selectPieceSEVolume);
        RefreshAllPieceButtons();
    }

    public bool CanUsePiece(PieceData pieceData)
    {
        return GetRemainingCount(pieceData) > 0;
    }

    public int GetRemainingCount(PieceData pieceData)
    {
        if (pieceData == null)
        {
            return 0;
        }

        return remainingPieces.TryGetValue(pieceData, out int count) ? count : 0;
    }

    public bool TryConsumePiece(PieceData pieceData)
    {
        if (!CanUsePiece(pieceData))
        {
            return false;
        }

        remainingPieces[pieceData]--;

        if (selectedPiece == pieceData && remainingPieces[pieceData] <= 0)
        {
            selectedPiece = null;
        }

        RefreshAllPieceButtons();
        return true;
    }

    public void OnCellLeftClicked(CellView cell)
    {
        ClearPreview();

        if (cell == null)
        {
            Debug.LogWarning("Clicked CellView is null.");
            return;
        }

        if (selectedPiece == null)
        {
            Debug.LogWarning("No piece is selected.");
            return;
        }

        if (!CanUsePiece(selectedPiece))
        {
            Debug.LogWarning($"{selectedPiece.displayName} has no remaining stock.");
            selectedPiece = null;
            RefreshAllPieceButtons();
            return;
        }

        if (!cell.IsActive)
        {
            Debug.LogWarning("Cannot place a piece on an inactive cell.");
            return;
        }

        if (cell.HasPiece)
        {
            Debug.LogWarning("Cannot place a piece on a cell that already has a piece.");
            return;
        }

        PieceData pieceToPlace = selectedPiece;
        PlacedPieceRecord placedRecord = PlacePieceInternal(cell, pieceToPlace);

        if (!TryConsumePiece(pieceToPlace))
        {
            Debug.LogWarning($"{pieceToPlace.displayName} could not be consumed after placement.");
            RemovePlacedPieceInternal(placedRecord);
            RecalculateCoverage();
            return;
        }

        actionHistory.Add(new PuzzleActionRecord
        {
            actionType = PuzzleActionType.Place,
            pieceData = pieceToPlace,
            cell = cell
        });

        UpdateCoverage(cell.GridPosition, pieceToPlace.pieceType);
        PlaySE(placePieceSE, placePieceSEVolume);
        CheckClear();
    }

    public void OnCellClicked(CellView cell)
    {
        OnCellLeftClicked(cell);
    }

    public void OnCellRightClicked(CellView cell)
    {
        ClearPreview();
        TryRemovePieceAt(cell);
    }

    public void OnCellHoverEnter(CellView cell)
    {
        ShowPreview(cell);
    }

    public void OnCellHoverExit(CellView cell)
    {
        ClearPreview();
    }

    public void UndoLastMove()
    {
        ClearPreview();

        if (actionHistory.Count == 0)
        {
            Debug.Log("No moves to undo.");
            return;
        }

        PuzzleActionRecord lastAction = actionHistory[actionHistory.Count - 1];
        actionHistory.RemoveAt(actionHistory.Count - 1);

        if (lastAction.actionType == PuzzleActionType.Place)
        {
            PlacedPieceRecord placedRecord = FindPlacedPieceRecord(lastAction.cell);

            if (placedRecord == null)
            {
                Debug.LogWarning("Could not undo place because the placed piece record was not found.");
            }
            else
            {
                RemovePlacedPieceInternal(placedRecord);
                RestorePieceStock(lastAction.pieceData);
            }
        }
        else if (lastAction.actionType == PuzzleActionType.Remove)
        {
            if (lastAction.cell == null || !lastAction.cell.IsActive || lastAction.cell.HasPiece)
            {
                Debug.LogWarning("Could not undo remove because the target cell is unavailable.");
            }
            else
            {
                PlacePieceInternal(lastAction.cell, lastAction.pieceData);
                ConsumePieceStock(lastAction.pieceData);
            }
        }

        RecalculateCoverage();

        SetClearDisplayVisible(false);
        SetNextStageButtonVisible(false);
        PlaySE(undoSE, undoSEVolume);
        RefreshAllPieceButtons();
    }

    public void ResetPuzzle()
    {
        ClearPreview();

        foreach (CellView cell in cells)
        {
            if (cell == null)
            {
                continue;
            }

            cell.SetCoverCount(0);
            cell.SetPiece(false);
        }

        ClearPlacedPieces();
        actionHistory.Clear();
        InitializeRemainingPieces();
        selectedPiece = null;

        SetClearDisplayVisible(false);
        SetNextStageButtonVisible(false);
        PlaySE(resetSE, resetSEVolume);
        RefreshAllPieceButtons();
    }

    public void SetCells(List<CellView> newCells)
    {
        ClearPreview();

        if (newCells == null)
        {
            Debug.LogWarning("SetCells was called with null.");
            cells = new List<CellView>();
        }
        else
        {
            cells = newCells;
        }

        ClearPlacedPieces();
        actionHistory.Clear();
        BuildCellMap();
        RefreshCoverageVisibility();
        RefreshCoverageToggleButtonView();
    }

    public void LoadPieceStocks(List<StagePieceStock> newPieceStocks)
    {
        ClearPreview();
        ClearPlacedPieces();
        actionHistory.Clear();

        pieceStocks = newPieceStocks != null
            ? new List<StagePieceStock>(newPieceStocks)
            : new List<StagePieceStock>();

        InitializeRemainingPieces();
        selectedPiece = null;

        SetClearDisplayVisible(false);
        SetNextStageButtonVisible(false);
        RefreshAllPieceButtons();
    }

    public void LoadStage(StageData stageData)
    {
        if (stageData == null)
        {
            Debug.LogWarning("Cannot load a null StageData.");
            return;
        }

        LoadPieceStocks(stageData.pieceStocks);
    }

    public void HideClearDisplay()
    {
        SetClearDisplayVisible(false);
        SetNextStageButtonVisible(false);
    }

    private void BuildCellMap()
    {
        cellMap.Clear();

        foreach (CellView cell in cells)
        {
            if (cell == null)
            {
                Debug.LogWarning("The cells list contains a null entry.");
                continue;
            }

            if (cellMap.ContainsKey(cell.GridPosition))
            {
                Debug.LogWarning($"GridPosition {cell.GridPosition} is duplicated.");
                continue;
            }

            cellMap.Add(cell.GridPosition, cell);
        }
    }

    private void InitializeRemainingPieces()
    {
        remainingPieces.Clear();

        foreach (StagePieceStock stock in pieceStocks)
        {
            if (stock == null)
            {
                Debug.LogWarning("pieceStocks contains a null entry.");
                continue;
            }

            if (stock.pieceData == null)
            {
                Debug.LogWarning("A StagePieceStock entry has no PieceData.");
                continue;
            }

            int count = Mathf.Max(0, stock.count);

            if (remainingPieces.ContainsKey(stock.pieceData))
            {
                remainingPieces[stock.pieceData] += count;
            }
            else
            {
                remainingPieces.Add(stock.pieceData, count);
            }
        }
    }

    private GameObject PlacePiece(CellView cell, PieceData pieceData)
    {
        cell.SetPiece(true);

        if (placedPiecePrefab == null)
        {
            Debug.LogWarning("placedPiecePrefab is not assigned. Only the cell state was updated.");
            return null;
        }

        Transform parent = placedPieceRoot != null ? placedPieceRoot : transform;
        GameObject placedPiece = Instantiate(placedPiecePrefab, cell.transform.position, Quaternion.identity, parent);
        placedPiece.name = $"Placed_{pieceData.pieceType}_{cell.GridPosition.x}_{cell.GridPosition.y}";

        SpriteRenderer spriteRenderer = placedPiece.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = placedPiece.GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return placedPiece;
        }

        if (pieceData.icon != null)
        {
            spriteRenderer.sprite = pieceData.icon;
            spriteRenderer.color = Color.white;
        }
        else
        {
            spriteRenderer.color = Color.yellow;
        }

        return placedPiece;
    }

    private PlacedPieceRecord PlacePieceInternal(CellView cell, PieceData pieceData)
    {
        GameObject placedObject = PlacePiece(cell, pieceData);
        PlacedPieceRecord record = new PlacedPieceRecord
        {
            pieceData = pieceData,
            cell = cell,
            placedObject = placedObject
        };

        placedPieces.Add(record);
        return record;
    }

    private void RemovePlacedPieceInternal(PlacedPieceRecord record)
    {
        if (record == null)
        {
            return;
        }

        if (record.placedObject != null)
        {
            DestroyPlacedObject(record.placedObject);
        }

        if (record.cell != null)
        {
            record.cell.SetPiece(false);
        }

        placedPieces.Remove(record);
    }

    private void ClearPlacedPieces()
    {
        if (placedPieceRoot != null)
        {
            for (int i = placedPieceRoot.childCount - 1; i >= 0; i--)
            {
                DestroyPlacedObject(placedPieceRoot.GetChild(i).gameObject);
            }
        }
        else
        {
            foreach (PlacedPieceRecord placedPiece in placedPieces)
            {
                if (placedPiece != null && placedPiece.placedObject != null)
                {
                    DestroyPlacedObject(placedPiece.placedObject);
                }
            }
        }

        placedPieces.Clear();
    }

    private void TryRemovePieceAt(CellView cell)
    {
        if (cell == null)
        {
            return;
        }

        if (!cell.HasPiece)
        {
            return;
        }

        PlacedPieceRecord record = FindPlacedPieceRecord(cell);

        if (record == null)
        {
            Debug.LogWarning($"No placed piece record found at {cell.GridPosition}.");
            return;
        }

        PieceData removedPieceData = record.pieceData;
        CellView removedCell = record.cell;
        RemovePlacedPieceInternal(record);
        RestorePieceStock(record.pieceData);
        actionHistory.Add(new PuzzleActionRecord
        {
            actionType = PuzzleActionType.Remove,
            pieceData = removedPieceData,
            cell = removedCell
        });

        RecalculateCoverage();

        SetClearDisplayVisible(false);
        SetNextStageButtonVisible(false);
        PlaySE(removePieceSE, removePieceSEVolume);
        RefreshAllPieceButtons();
    }

    private PlacedPieceRecord FindPlacedPieceRecord(CellView cell)
    {
        foreach (PlacedPieceRecord placedPiece in placedPieces)
        {
            if (placedPiece != null && placedPiece.cell == cell)
            {
                return placedPiece;
            }
        }

        return null;
    }

    private void DestroyPlacedObject(GameObject placedObject)
    {
        if (placedObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(placedObject);
        }
        else
        {
            DestroyImmediate(placedObject);
        }
    }

    private void UpdateCoverage(Vector2Int origin, PieceType pieceType)
    {
        List<Vector2Int> coveredPositions = CoverageCalculator.GetCoveredPositions(pieceType, origin, cellMap);

        foreach (Vector2Int position in coveredPositions)
        {
            if (cellMap.TryGetValue(position, out CellView coveredCell) && coveredCell != null)
            {
                coveredCell.AddCoverCount();
            }
        }
    }

    private void ShowPreview(CellView originCell)
    {
        ClearPreview();

        if (selectedPiece == null
            || originCell == null
            || !originCell.IsActive
            || originCell.HasPiece)
        {
            return;
        }

        List<Vector2Int> previewPositions = CoverageCalculator.GetCoveredPositions(
            selectedPiece.pieceType,
            originCell.GridPosition,
            cellMap
        );

        foreach (Vector2Int position in previewPositions)
        {
            if (cellMap.TryGetValue(position, out CellView previewCell) && previewCell != null)
            {
                previewCell.SetPreview(true);
                previewCells.Add(previewCell);
            }
        }
    }

    private void ClearPreview()
    {
        foreach (CellView previewCell in previewCells)
        {
            if (previewCell != null)
            {
                previewCell.SetPreview(false);
            }
        }

        previewCells.Clear();
    }

    private void RecalculateCoverage()
    {
        foreach (CellView cell in cells)
        {
            if (cell != null)
            {
                cell.SetCoverCount(0);
            }
        }

        foreach (PlacedPieceRecord placedPiece in placedPieces)
        {
            if (placedPiece == null || placedPiece.pieceData == null || placedPiece.cell == null)
            {
                continue;
            }

            UpdateCoverage(placedPiece.cell.GridPosition, placedPiece.pieceData.pieceType);
        }

        RefreshCoverageVisibility();
    }

    private void RefreshCoverageVisibility()
    {
        bool actualCoverageVisible = GetActualCoverageVisible();

        foreach (CellView cell in cells)
        {
            if (cell != null)
            {
                cell.SetCoverageVisible(actualCoverageVisible);
            }
        }
    }

    private void SetCoverageTemporarilyInverted(bool inverted)
    {
        if (isCoverageTemporarilyInverted == inverted)
        {
            return;
        }

        isCoverageTemporarilyInverted = inverted;
        RefreshCoverageVisibility();
    }

    private bool GetActualCoverageVisible()
    {
        return coverageVisible ^ isCoverageTemporarilyInverted;
    }

    private void RefreshCoverageToggleButtonView()
    {
        if (coverageToggleLabel != null)
        {
            coverageToggleLabel.text = coverageVisible ? "Coverage ON" : "Coverage OFF";
        }

        if (coverageToggleButtonImage != null)
        {
            coverageToggleButtonImage.color = coverageVisible
                ? coverageButtonOnColor
                : coverageButtonOffColor;
        }
    }

    private void RestorePieceStock(PieceData pieceData)
    {
        if (pieceData == null)
        {
            return;
        }

        if (remainingPieces.ContainsKey(pieceData))
        {
            remainingPieces[pieceData]++;
        }
        else
        {
            remainingPieces.Add(pieceData, 1);
        }
    }

    private void ConsumePieceStock(PieceData pieceData)
    {
        if (pieceData == null)
        {
            return;
        }

        if (!remainingPieces.ContainsKey(pieceData))
        {
            remainingPieces.Add(pieceData, 0);
            return;
        }

        remainingPieces[pieceData] = Mathf.Max(0, remainingPieces[pieceData] - 1);

        if (selectedPiece == pieceData && remainingPieces[pieceData] <= 0)
        {
            selectedPiece = null;
        }
    }

    private void RefreshAllPieceButtons()
    {
        PieceButton[] pieceButtons = FindObjectsByType<PieceButton>(FindObjectsSortMode.None);

        foreach (PieceButton pieceButton in pieceButtons)
        {
            if (pieceButton != null)
            {
                pieceButton.RefreshView();
            }
        }

        RefreshPieceButtonSelection(pieceButtons);
    }

    private void RefreshPieceButtonSelection()
    {
        PieceButton[] pieceButtons = FindObjectsByType<PieceButton>(FindObjectsSortMode.None);
        RefreshPieceButtonSelection(pieceButtons);
    }

    private void RefreshPieceButtonSelection(PieceButton[] pieceButtons)
    {
        foreach (PieceButton pieceButton in pieceButtons)
        {
            if (pieceButton == null)
            {
                continue;
            }

            bool isSelected = selectedPiece != null
                && pieceButton.PieceData == selectedPiece
                && CanUsePiece(selectedPiece);
            pieceButton.SetSelectedVisual(isSelected);
        }
    }

    private void CheckClear()
    {
        foreach (CellView cell in cells)
        {
            if (cell == null || !cell.IsActive)
            {
                continue;
            }

            if (!cell.IsClearConditionSatisfied())
            {
                return;
            }
        }

        SetClearDisplayVisible(true);
        SetNextStageButtonVisible(true);
        PlaySE(clearSE, clearSEVolume);
        UnlockNextStage();
    }

    private void UnlockNextStage()
    {
        StageLoader targetStageLoader = stageLoader != null
            ? stageLoader
            : FindFirstObjectByType<StageLoader>();

        if (targetStageLoader != null)
        {
            targetStageLoader.UnlockNextStage();
        }
    }

    private void SetClearDisplayVisible(bool visible)
    {
        if (clearPanel != null)
        {
            clearPanel.SetActive(visible);
            return;
        }

        if (clearText != null)
        {
            clearText.gameObject.SetActive(visible);
            return;
        }

        if (clearDisplayObject != null)
        {
            clearDisplayObject.SetActive(visible);
        }
        else if (visible)
        {
            Debug.Log("Clear!");
        }
    }

    private void SetNextStageButtonVisible(bool visible)
    {
        if (nextStageButton != null)
        {
            nextStageButton.SetActive(visible);
        }
    }

    private void PlaySE(AudioClip clip, float volumeScale = 1f)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(clip, volumeScale);
        }
    }

}
