using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class AnswerBasedStageEditorWindow : EditorWindow
{
    private const int CellSize = 48;

    [SerializeField] private StageData targetStageData;
    [SerializeField] private int gridWidth = 8;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private PieceData selectedPiece;
    [SerializeField] private EditMode editMode = EditMode.PlaceNormalPiece;
    [SerializeField] private HelpPageType helpPageToUnlock = HelpPageType.None;
    [SerializeField] private HelpPageType helpPageToShowEveryTime = HelpPageType.None;

    private readonly List<EditorPlacedPiece> placedPieces = new List<EditorPlacedPiece>();
    private readonly HashSet<Vector2Int> exactCoverCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> removedCells = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, int> coverCounts = new Dictionary<Vector2Int, int>();
    private Vector2 scrollPosition;
    private string previewBoardText = string.Empty;

    private enum EditMode
    {
        PlaceNormalPiece,
        PlaceFixedPiece,
        DeletePiece,
        ToggleExactCoverCell,
        ToggleRemovedCell
    }

    [MenuItem("Tools/ChessPuzzle/Answer Based Stage Editor")]
    public static void Open()
    {
        GetWindow<AnswerBasedStageEditorWindow>("Answer Based Stage Editor");
    }

    private void OnEnable()
    {
        RecalculatePreview();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawStageDataSettings();
        DrawGridSettings();
        DrawGrid();
        DrawPreview();

        EditorGUILayout.EndScrollView();
    }

    private void DrawStageDataSettings()
    {
        EditorGUILayout.LabelField("StageData", EditorStyles.boldLabel);
        targetStageData = (StageData)EditorGUILayout.ObjectField("Target StageData", targetStageData, typeof(StageData), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Help", EditorStyles.boldLabel);
        helpPageToUnlock = (HelpPageType)EditorGUILayout.EnumPopup("Help Page To Unlock", helpPageToUnlock);
        helpPageToShowEveryTime = (HelpPageType)EditorGUILayout.EnumPopup("Help Page To Show Every Time", helpPageToShowEveryTime);

        EditorGUILayout.Space();
    }

    private void DrawGridSettings()
    {
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        gridWidth = EditorGUILayout.IntField("Width", gridWidth);
        gridHeight = EditorGUILayout.IntField("Height", gridHeight);

        if (EditorGUI.EndChangeCheck())
        {
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);
            RemoveOutOfBoundsState();
            RecalculatePreview();
        }

        selectedPiece = (PieceData)EditorGUILayout.ObjectField("PieceData", selectedPiece, typeof(PieceData), false);
        editMode = (EditMode)EditorGUILayout.EnumPopup("Edit Mode", editMode);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear Pieces"))
        {
            placedPieces.Clear();
            RecalculatePreview();
        }

        if (GUILayout.Button("Clear Removed Cells"))
        {
            removedCells.Clear();
            RecalculatePreview();
        }

        if (GUILayout.Button("Clear Exact Cover"))
        {
            exactCoverCells.Clear();
            RecalculatePreview();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void DrawGrid()
    {
        EditorGUILayout.LabelField("Answer Grid", EditorStyles.boldLabel);

        for (int y = gridHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = GetCellColor(position);

                if (GUILayout.Button(GetCellLabel(position), GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                {
                    HandleCellClick(position);
                }

                GUI.backgroundColor = previousColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Dark X: inactive or uncovered, red X: manually removed, Z: active cell, number: exact cover cell, N:/F: normal/fixed piece.", MessageType.Info);
    }

    private void DrawPreview()
    {
        EditorGUILayout.LabelField("Preview BoardText", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(previewBoardText, GUILayout.MinHeight(120f));

        using (new EditorGUI.DisabledScope(targetStageData == null))
        {
            if (GUILayout.Button("Apply To StageData", GUILayout.Height(32f)))
            {
                ApplyToStageData();
            }
        }
    }

    private void HandleCellClick(Vector2Int position)
    {
        switch (editMode)
        {
            case EditMode.PlaceNormalPiece:
                PlacePiece(position, false);
                break;
            case EditMode.PlaceFixedPiece:
                PlacePiece(position, true);
                break;
            case EditMode.DeletePiece:
                DeletePiece(position);
                break;
            case EditMode.ToggleExactCoverCell:
                ToggleExactCoverCell(position);
                break;
            case EditMode.ToggleRemovedCell:
                ToggleRemovedCell(position);
                break;
        }

        RecalculatePreview();
    }

    private void PlacePiece(Vector2Int position, bool isFixed)
    {
        if (selectedPiece == null)
        {
            Debug.LogWarning("Select a PieceData before placing a piece.");
            return;
        }

        if (removedCells.Contains(position))
        {
            Debug.LogWarning($"Cannot place a piece on removed cell {position}.");
            return;
        }

        if (GetPlacedPieceAt(position) != null)
        {
            Debug.LogWarning($"A piece already exists at {position}.");
            return;
        }

        placedPieces.Add(new EditorPlacedPiece
        {
            pieceData = selectedPiece,
            position = position,
            isFixed = isFixed
        });
    }

    private void DeletePiece(Vector2Int position)
    {
        EditorPlacedPiece placedPiece = GetPlacedPieceAt(position);

        if (placedPiece != null)
        {
            placedPieces.Remove(placedPiece);
        }
    }

    private void ToggleExactCoverCell(Vector2Int position)
    {
        if (removedCells.Contains(position))
        {
            Debug.LogWarning($"Removed cell {position} cannot be an exact cover cell.");
            return;
        }

        if (!coverCounts.TryGetValue(position, out int coverCount) || coverCount <= 0)
        {
            Debug.LogWarning($"Cell {position} has no coverage and cannot be an exact cover cell.");
            return;
        }

        if (!exactCoverCells.Add(position))
        {
            exactCoverCells.Remove(position);
        }
    }

    private void ToggleRemovedCell(Vector2Int position)
    {
        if (GetPlacedPieceAt(position) != null)
        {
            Debug.LogWarning($"Cannot remove cell {position} because a piece is placed there.");
            return;
        }

        if (!removedCells.Add(position))
        {
            removedCells.Remove(position);
            return;
        }

        exactCoverCells.Remove(position);
    }

    private void RecalculatePreview()
    {
        coverCounts.Clear();

        foreach (EditorPlacedPiece placedPiece in placedPieces)
        {
            foreach (Vector2Int coveredPosition in GetCoveredPositions(placedPiece.pieceData.pieceType, placedPiece.position))
            {
                if (coverCounts.ContainsKey(coveredPosition))
                {
                    coverCounts[coveredPosition]++;
                }
                else
                {
                    coverCounts.Add(coveredPosition, 1);
                }
            }
        }

        previewBoardText = BuildBoardText(logWarnings: false);
    }

    private List<Vector2Int> GetCoveredPositions(PieceType pieceType, Vector2Int origin)
    {
        List<Vector2Int> results = new List<Vector2Int>();

        TryAddPosition(origin, results);

        switch (pieceType)
        {
            case PieceType.Pawn:
                TryAddPosition(origin + Vector2Int.up, results);
                break;
            case PieceType.Knight:
                AddStepMoves(origin, KnightDirections, results);
                break;
            case PieceType.Bishop:
                AddLineMoves(origin, DiagonalDirections, results);
                break;
            case PieceType.Rook:
                AddLineMoves(origin, OrthogonalDirections, results);
                break;
            case PieceType.Queen:
                AddLineMoves(origin, OrthogonalDirections, results);
                AddLineMoves(origin, DiagonalDirections, results);
                break;
            case PieceType.King:
                AddStepMoves(origin, OrthogonalDirections, results);
                AddStepMoves(origin, DiagonalDirections, results);
                break;
        }

        return results;
    }

    private void AddStepMoves(Vector2Int origin, Vector2Int[] directions, List<Vector2Int> results)
    {
        foreach (Vector2Int direction in directions)
        {
            TryAddPosition(origin + direction, results);
        }
    }

    private void AddLineMoves(Vector2Int origin, Vector2Int[] directions, List<Vector2Int> results)
    {
        foreach (Vector2Int direction in directions)
        {
            Vector2Int position = origin + direction;

            while (IsValidCell(position))
            {
                AddUnique(position, results);
                position += direction;
            }
        }
    }

    private void TryAddPosition(Vector2Int position, List<Vector2Int> results)
    {
        if (IsValidCell(position))
        {
            AddUnique(position, results);
        }
    }

    private bool IsValidCell(Vector2Int position)
    {
        return position.x >= 0
            && position.x < gridWidth
            && position.y >= 0
            && position.y < gridHeight
            && !removedCells.Contains(position);
    }

    private void AddUnique(Vector2Int position, List<Vector2Int> results)
    {
        if (!results.Contains(position))
        {
            results.Add(position);
        }
    }

    private string BuildBoardText(bool logWarnings)
    {
        StringBuilder builder = new StringBuilder();

        for (int y = gridHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                builder.Append(GetBoardTextChar(position, logWarnings));
            }

            if (y > 0)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private char GetBoardTextChar(Vector2Int position, bool logWarnings)
    {
        if (removedCells.Contains(position))
        {
            return 'X';
        }

        bool isCovered = coverCounts.TryGetValue(position, out int coverCount) && coverCount > 0;

        if (!isCovered)
        {
            if (logWarnings && exactCoverCells.Contains(position))
            {
                Debug.LogWarning($"Exact cover cell {position} has CoverCount=0. It will be exported as X.");
            }

            return 'X';
        }

        if (!exactCoverCells.Contains(position))
        {
            return 'Z';
        }

        if (coverCount >= 1 && coverCount <= 5)
        {
            return (char)('0' + coverCount);
        }

        if (logWarnings)
        {
            Debug.LogWarning($"Exact cover cell {position} has CoverCount={coverCount}. It will be exported as Z.");
        }

        return 'Z';
    }

    private void ApplyToStageData()
    {
        if (targetStageData == null)
        {
            return;
        }

        string boardText = BuildBoardText(logWarnings: true);
        List<StagePieceStock> pieceStocks = BuildPieceStocks();
        List<FixedPieceData> fixedPieces = BuildFixedPieces();

        Undo.RecordObject(targetStageData, "Apply Answer Based StageData");
        targetStageData.boardText = boardText;
        targetStageData.pieceStocks = pieceStocks;
        targetStageData.fixedPieces = fixedPieces;
        targetStageData.helpPageToUnlock = helpPageToUnlock;
        targetStageData.helpPageToShowEveryTime = helpPageToShowEveryTime;
        targetStageData.authorNote = BuildAuthorNote();
        EditorUtility.SetDirty(targetStageData);
        AssetDatabase.SaveAssets();
    }

    private List<StagePieceStock> BuildPieceStocks()
    {
        Dictionary<PieceData, int> stockCounts = new Dictionary<PieceData, int>();

        foreach (EditorPlacedPiece placedPiece in placedPieces)
        {
            if (placedPiece.isFixed || placedPiece.pieceData == null)
            {
                continue;
            }

            if (stockCounts.ContainsKey(placedPiece.pieceData))
            {
                stockCounts[placedPiece.pieceData]++;
            }
            else
            {
                stockCounts.Add(placedPiece.pieceData, 1);
            }
        }

        List<StagePieceStock> pieceStocks = new List<StagePieceStock>();

        foreach (KeyValuePair<PieceData, int> stockCount in stockCounts)
        {
            pieceStocks.Add(new StagePieceStock
            {
                pieceData = stockCount.Key,
                count = stockCount.Value
            });
        }

        return pieceStocks;
    }

    private List<FixedPieceData> BuildFixedPieces()
    {
        List<FixedPieceData> fixedPieces = new List<FixedPieceData>();

        foreach (EditorPlacedPiece placedPiece in placedPieces)
        {
            if (!placedPiece.isFixed || placedPiece.pieceData == null)
            {
                continue;
            }

            fixedPieces.Add(new FixedPieceData
            {
                pieceData = placedPiece.pieceData,
                position = placedPiece.position
            });
        }

        return fixedPieces;
    }

    private string BuildAuthorNote()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Created with Answer Based Stage Editor.");
        builder.AppendLine("Answer placements:");

        foreach (EditorPlacedPiece placedPiece in placedPieces)
        {
            string fixedLabel = placedPiece.isFixed ? " [Fixed]" : string.Empty;
            builder.AppendLine($"{placedPiece.pieceData.pieceType} at {placedPiece.position}{fixedLabel}");
        }

        return builder.ToString();
    }

    private string GetCellLabel(Vector2Int position)
    {
        EditorPlacedPiece placedPiece = GetPlacedPieceAt(position);
        string cellLabel = GetBoardTextChar(position, logWarnings: false).ToString();

        if (removedCells.Contains(position))
        {
            cellLabel = "X";
        }
        else if (exactCoverCells.Contains(position) && coverCounts.TryGetValue(position, out int coverCount) && coverCount > 0)
        {
            cellLabel = coverCount <= 5 ? coverCount.ToString() : "Z";
        }

        if (placedPiece == null)
        {
            return cellLabel;
        }

        string prefix = placedPiece.isFixed ? "F" : "N";
        return $"{prefix}:{GetPieceLabel(placedPiece.pieceData)}\n{cellLabel}";
    }

    private Color GetCellColor(Vector2Int position)
    {
        if (removedCells.Contains(position))
        {
            return new Color(0.8f, 0.25f, 0.25f, 1f);
        }

        if (GetPlacedPieceAt(position) != null)
        {
            return new Color(0.9f, 0.9f, 1f, 1f);
        }

        if (exactCoverCells.Contains(position))
        {
            return new Color(0.55f, 0.75f, 1f, 1f);
        }

        if (coverCounts.TryGetValue(position, out int coverCount) && coverCount > 0)
        {
            return new Color(0.65f, 0.9f, 0.65f, 1f);
        }

        return new Color(0.35f, 0.35f, 0.35f, 1f);
    }

    private string GetPieceLabel(PieceData pieceData)
    {
        if (pieceData == null)
        {
            return "?";
        }

        return pieceData.pieceType.ToString().Substring(0, 1);
    }

    private EditorPlacedPiece GetPlacedPieceAt(Vector2Int position)
    {
        return placedPieces.Find(placedPiece => placedPiece.position == position);
    }

    private void RemoveOutOfBoundsState()
    {
        placedPieces.RemoveAll(placedPiece => !IsInsideGrid(placedPiece.position));
        exactCoverCells.RemoveWhere(position => !IsInsideGrid(position));
        removedCells.RemoveWhere(position => !IsInsideGrid(position));
    }

    private bool IsInsideGrid(Vector2Int position)
    {
        return position.x >= 0
            && position.x < gridWidth
            && position.y >= 0
            && position.y < gridHeight;
    }

    private static readonly Vector2Int[] OrthogonalDirections =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private static readonly Vector2Int[] DiagonalDirections =
    {
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

    private static readonly Vector2Int[] KnightDirections =
    {
        new Vector2Int(1, 2),
        new Vector2Int(2, 1),
        new Vector2Int(2, -1),
        new Vector2Int(1, -2),
        new Vector2Int(-1, -2),
        new Vector2Int(-2, -1),
        new Vector2Int(-2, 1),
        new Vector2Int(-1, 2)
    };

    private class EditorPlacedPiece
    {
        public PieceData pieceData;
        public Vector2Int position;
        public bool isFixed;
    }
}
