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
    [SerializeField] private StageCategory stageCategory = StageCategory.Standard;
    [SerializeField] private HelpPageType helpPageToUnlock = HelpPageType.None;
    [SerializeField] private HelpPageType helpPageToShowEveryTime = HelpPageType.None;
    [SerializeField] private string authorNote = string.Empty;
    [SerializeField] private List<EditorPieceStock> editorPieceStocks = new List<EditorPieceStock>();

    private readonly List<EditorPlacedPiece> solutionPieces = new List<EditorPlacedPiece>();
    private readonly List<EditorPlacedPiece> fixedPieces = new List<EditorPlacedPiece>();
    private readonly HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> generatedCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> loadedBoardCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> exactCoverCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> removedCells = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, int> coverCounts = new Dictionary<Vector2Int, int>();
    private readonly Dictionary<Vector2Int, int> loadedRequiredCoverCounts = new Dictionary<Vector2Int, int>();
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
        DrawPieceStocks();
        DrawGrid();
        DrawPreview();

        EditorGUILayout.EndScrollView();
    }

    private void DrawStageDataSettings()
    {
        EditorGUILayout.LabelField("StageData", EditorStyles.boldLabel);
        targetStageData = (StageData)EditorGUILayout.ObjectField("Target StageData", targetStageData, typeof(StageData), false);

        EditorGUILayout.BeginHorizontal();

        using (new EditorGUI.DisabledScope(targetStageData == null))
        {
            if (GUILayout.Button("Load From StageData"))
            {
                LoadFromStageDataWithConfirmation();
            }

            if (GUILayout.Button("Apply To StageData"))
            {
                ApplyToStageData();
            }
        }

        if (GUILayout.Button("Save As New StageData"))
        {
            SaveAsNewStageData();
        }

        EditorGUILayout.EndHorizontal();

        stageCategory = (StageCategory)EditorGUILayout.EnumPopup("Stage Category", stageCategory);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Help", EditorStyles.boldLabel);
        helpPageToUnlock = (HelpPageType)EditorGUILayout.EnumPopup("Help Page To Unlock", helpPageToUnlock);
        helpPageToShowEveryTime = (HelpPageType)EditorGUILayout.EnumPopup("Help Page To Show Every Time", helpPageToShowEveryTime);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Author Note", EditorStyles.boldLabel);
        authorNote = EditorGUILayout.TextArea(authorNote, GUILayout.MinHeight(60f));

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
            solutionPieces.Clear();
            fixedPieces.Clear();
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
            loadedRequiredCoverCounts.Clear();
            RecalculatePreview();
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Clear Loaded Board Cells"))
        {
            ClearLoadedBoardCells();
        }

        EditorGUILayout.Space();
    }

    private void DrawPieceStocks()
    {
        EditorGUILayout.LabelField("Piece Stocks", EditorStyles.boldLabel);

        for (int i = 0; i < editorPieceStocks.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            editorPieceStocks[i].pieceData = (PieceData)EditorGUILayout.ObjectField(editorPieceStocks[i].pieceData, typeof(PieceData), false);
            editorPieceStocks[i].count = Mathf.Max(0, EditorGUILayout.IntField(editorPieceStocks[i].count, GUILayout.Width(60f)));

            if (GUILayout.Button("Remove", GUILayout.Width(70f)))
            {
                editorPieceStocks.RemoveAt(i);
                RecalculatePreview();
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Piece Stock"))
        {
            editorPieceStocks.Add(new EditorPieceStock
            {
                pieceData = selectedPiece,
                count = 1
            });
        }

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
        EditorGUILayout.HelpBox("Dark X: inactive board cell, red X: manually removed board cell, Z: active board cell, number: exact cover cell, N:/F: normal/fixed piece, green: coverage preview.", MessageType.Info);
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

        List<EditorPlacedPiece> targetPieces = isFixed ? fixedPieces : solutionPieces;
        targetPieces.Add(new EditorPlacedPiece
        {
            pieceData = selectedPiece,
            position = position
        });
    }

    private void DeletePiece(Vector2Int position)
    {
        EditorPlacedPiece placedPiece = GetPlacedPieceAt(position);

        if (placedPiece != null)
        {
            solutionPieces.Remove(placedPiece);
            fixedPieces.Remove(placedPiece);
        }
    }

    private void ToggleExactCoverCell(Vector2Int position)
    {
        if (removedCells.Contains(position))
        {
            Debug.LogWarning($"Removed cell {position} cannot be an exact cover cell.");
            return;
        }

        if (!activeCells.Contains(position)
            && (!coverCounts.TryGetValue(position, out int coverCount) || coverCount <= 0)
            && !loadedRequiredCoverCounts.ContainsKey(position))
        {
            Debug.LogWarning($"Cell {position} is not an active board cell and has no coverage preview.");
            return;
        }

        if (!exactCoverCells.Add(position))
        {
            exactCoverCells.Remove(position);
            loadedRequiredCoverCounts.Remove(position);
            return;
        }

        loadedBoardCells.Add(position);

        if (!loadedRequiredCoverCounts.ContainsKey(position)
            && coverCounts.TryGetValue(position, out int currentCoverCount)
            && currentCoverCount >= 1
            && currentCoverCount <= 5)
        {
            loadedRequiredCoverCounts[position] = currentCoverCount;
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

        activeCells.Remove(position);
        exactCoverCells.Remove(position);
        loadedRequiredCoverCounts.Remove(position);
    }

    private void RecalculatePreview()
    {
        coverCounts.Clear();
        generatedCells.Clear();

        AddCoverage(solutionPieces);
        AddCoverage(fixedPieces);
        RebuildActiveCells();
        RemoveInvalidExactCoverCells();

        previewBoardText = BuildBoardText(logWarnings: false);
    }

    private void AddCoverage(List<EditorPlacedPiece> pieces)
    {
        foreach (EditorPlacedPiece placedPiece in pieces)
        {
            if (placedPiece.pieceData == null)
            {
                continue;
            }

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

                generatedCells.Add(coveredPosition);
            }
        }
    }

    private void RebuildActiveCells()
    {
        activeCells.Clear();

        foreach (Vector2Int position in loadedBoardCells)
        {
            if (IsInsideGrid(position) && !removedCells.Contains(position))
            {
                activeCells.Add(position);
            }
        }

        foreach (Vector2Int position in generatedCells)
        {
            if (IsInsideGrid(position) && !removedCells.Contains(position))
            {
                activeCells.Add(position);
            }
        }
    }

    private void RemoveInvalidExactCoverCells()
    {
        List<Vector2Int> invalidPositions = new List<Vector2Int>();

        foreach (Vector2Int position in exactCoverCells)
        {
            if (!activeCells.Contains(position))
            {
                invalidPositions.Add(position);
            }
        }

        foreach (Vector2Int position in invalidPositions)
        {
            exactCoverCells.Remove(position);
            loadedRequiredCoverCounts.Remove(position);
        }
    }

    private void ClearLoadedBoardCells()
    {
        loadedBoardCells.Clear();
        exactCoverCells.Clear();
        loadedRequiredCoverCounts.Clear();
        RecalculatePreview();
        Repaint();
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

        bool hasLoadedRequiredCoverCount = loadedRequiredCoverCounts.TryGetValue(position, out int loadedRequiredCoverCount);
        bool isActive = activeCells.Contains(position);

        if (!isActive && !hasLoadedRequiredCoverCount)
        {
            if (logWarnings && exactCoverCells.Contains(position))
            {
                Debug.LogWarning($"Exact cover cell {position} is not an active board cell. It will be exported as X.");
            }

            return 'X';
        }

        if (!exactCoverCells.Contains(position))
        {
            return 'Z';
        }

        int requiredCoverCount = hasLoadedRequiredCoverCount ? loadedRequiredCoverCount : 1;

        if (requiredCoverCount >= 1 && requiredCoverCount <= 5)
        {
            return (char)('0' + requiredCoverCount);
        }

        if (logWarnings)
        {
            Debug.LogWarning($"Exact cover cell {position} has RequiredCoverCount={requiredCoverCount}. It will be exported as Z.");
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
        List<FixedPieceData> fixedPieceData = BuildFixedPieces();

        Undo.RecordObject(targetStageData, "Apply Stage Data");
        WriteToStageData(targetStageData, boardText, pieceStocks, fixedPieceData);
        Debug.Log($"[STAGE EDITOR] Applied StageData: {targetStageData.name}");
    }

    private List<StagePieceStock> BuildPieceStocks()
    {
        if (editorPieceStocks.Count > 0)
        {
            List<StagePieceStock> explicitPieceStocks = new List<StagePieceStock>();

            foreach (EditorPieceStock editorPieceStock in editorPieceStocks)
            {
                if (editorPieceStock == null || editorPieceStock.pieceData == null || editorPieceStock.count <= 0)
                {
                    continue;
                }

                explicitPieceStocks.Add(new StagePieceStock
                {
                    pieceData = editorPieceStock.pieceData,
                    count = editorPieceStock.count
                });
            }

            return explicitPieceStocks;
        }

        Dictionary<PieceData, int> stockCounts = new Dictionary<PieceData, int>();

        foreach (EditorPlacedPiece placedPiece in solutionPieces)
        {
            if (placedPiece.pieceData == null)
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
        List<FixedPieceData> fixedPieceData = new List<FixedPieceData>();

        foreach (EditorPlacedPiece placedPiece in fixedPieces)
        {
            if (placedPiece.pieceData == null)
            {
                continue;
            }

            fixedPieceData.Add(new FixedPieceData
            {
                pieceData = placedPiece.pieceData,
                position = placedPiece.position
            });
        }

        return fixedPieceData;
    }

    private string BuildAuthorNote()
    {
        if (!string.IsNullOrEmpty(authorNote))
        {
            return authorNote;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Created with Answer Based Stage Editor.");
        builder.AppendLine("Answer placements:");

        foreach (EditorPlacedPiece placedPiece in solutionPieces)
        {
            builder.AppendLine($"{placedPiece.pieceData.pieceType} at {placedPiece.position}");
        }

        foreach (EditorPlacedPiece placedPiece in fixedPieces)
        {
            builder.AppendLine($"{placedPiece.pieceData.pieceType} at {placedPiece.position} [Fixed]");
        }

        return builder.ToString();
    }

    private void LoadFromStageDataWithConfirmation()
    {
        if (targetStageData == null)
        {
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Load From StageData",
            "\u73fe\u5728\u306e\u7de8\u96c6\u5185\u5bb9\u3092\u7834\u68c4\u3057\u3066 StageData \u3092\u8aad\u307f\u8fbc\u307f\u307e\u3059\u3002\u3088\u308d\u3057\u3044\u3067\u3059\u304b\uff1f",
            "Load",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        LoadFromStageData(targetStageData);
    }

    private void LoadFromStageData(StageData stageData)
    {
        if (stageData == null)
        {
            return;
        }

        solutionPieces.Clear();
        fixedPieces.Clear();
        activeCells.Clear();
        generatedCells.Clear();
        loadedBoardCells.Clear();
        exactCoverCells.Clear();
        removedCells.Clear();
        coverCounts.Clear();
        loadedRequiredCoverCounts.Clear();
        editorPieceStocks.Clear();

        string[] rows = GetBoardRows(stageData.boardText);
        gridHeight = Mathf.Max(1, rows.Length);
        gridWidth = Mathf.Max(1, GetMaxRowLength(rows));

        for (int rowIndex = 0; rowIndex < gridHeight; rowIndex++)
        {
            string row = rowIndex < rows.Length ? rows[rowIndex] : string.Empty;
            int y = gridHeight - 1 - rowIndex;

            for (int x = 0; x < gridWidth; x++)
            {
                char boardChar = x < row.Length ? row[x] : 'X';
                Vector2Int position = new Vector2Int(x, y);
                LoadBoardCell(position, boardChar);
            }
        }

        if (stageData.pieceStocks != null)
        {
            foreach (StagePieceStock stock in stageData.pieceStocks)
            {
                if (stock == null)
                {
                    continue;
                }

                editorPieceStocks.Add(new EditorPieceStock
                {
                    pieceData = stock.pieceData,
                    count = stock.count
                });
            }
        }

        if (stageData.fixedPieces != null)
        {
            foreach (FixedPieceData fixedPiece in stageData.fixedPieces)
            {
                if (fixedPiece == null || fixedPiece.pieceData == null || !IsInsideGrid(fixedPiece.position))
                {
                    continue;
                }

                removedCells.Remove(fixedPiece.position);
                fixedPieces.Add(new EditorPlacedPiece
                {
                    pieceData = fixedPiece.pieceData,
                    position = fixedPiece.position
                });
            }
        }

        stageCategory = stageData.stageCategory;
        helpPageToUnlock = stageData.helpPageToUnlock;
        helpPageToShowEveryTime = stageData.helpPageToShowEveryTime;
        authorNote = stageData.authorNote;
        RecalculatePreview();
        Repaint();

        Debug.Log($"[STAGE EDITOR] Loaded StageData: {stageData.name}");
        Debug.Log($"[STAGE EDITOR] board size={gridWidth}x{gridHeight}");
        Debug.Log($"[STAGE EDITOR] fixedPieces={(stageData.fixedPieces != null ? stageData.fixedPieces.Count : 0)}");
        Debug.Log($"[STAGE EDITOR] pieceStocks={(stageData.pieceStocks != null ? stageData.pieceStocks.Count : 0)}");
    }

    private void LoadBoardCell(Vector2Int position, char boardChar)
    {
        if (IsInactiveBoardChar(boardChar))
        {
            return;
        }

        loadedBoardCells.Add(position);

        if (boardChar >= '1' && boardChar <= '5')
        {
            exactCoverCells.Add(position);
            loadedRequiredCoverCounts[position] = boardChar - '0';
        }
    }

    private void SaveAsNewStageData()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save As New StageData",
            targetStageData != null ? $"{targetStageData.name}_copy" : "NewStageData",
            "asset",
            "Save the current editor state as a new StageData asset.");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        StageData newStageData = CreateInstance<StageData>();
        AssetDatabase.CreateAsset(newStageData, path);
        WriteToStageData(newStageData, BuildBoardText(logWarnings: true), BuildPieceStocks(), BuildFixedPieces());
        AssetDatabase.SaveAssets();
        targetStageData = newStageData;
        EditorGUIUtility.PingObject(newStageData);
        Debug.Log($"[STAGE EDITOR] Saved new StageData: {newStageData.name}");
    }

    private void WriteToStageData(StageData stageData, string boardText, List<StagePieceStock> pieceStocks, List<FixedPieceData> fixedPieces)
    {
        if (string.IsNullOrEmpty(stageData.stageName))
        {
            stageData.stageName = stageData.name;
        }

        stageData.stageCategory = stageCategory;
        stageData.boardText = boardText;
        stageData.pieceStocks = pieceStocks;
        stageData.fixedPieces = fixedPieces;
        stageData.helpPageToUnlock = helpPageToUnlock;
        stageData.helpPageToShowEveryTime = helpPageToShowEveryTime;
        stageData.authorNote = BuildAuthorNote();
        EditorUtility.SetDirty(stageData);
        AssetDatabase.SaveAssets();
    }

    private static string[] GetBoardRows(string boardText)
    {
        if (string.IsNullOrEmpty(boardText))
        {
            return new[] { "X" };
        }

        return boardText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    private static int GetMaxRowLength(string[] rows)
    {
        int maxLength = 0;

        foreach (string row in rows)
        {
            if (row != null && row.Length > maxLength)
            {
                maxLength = row.Length;
            }
        }

        return maxLength;
    }

    private static bool IsInactiveBoardChar(char boardChar)
    {
        return boardChar == 'X'
            || boardChar == 'x'
            || boardChar == '.'
            || boardChar == ' ';
    }

    private string GetCellLabel(Vector2Int position)
    {
        EditorPlacedPiece placedPiece = GetPlacedPieceAt(position);
        string cellLabel = GetBoardTextChar(position, logWarnings: false).ToString();

        if (removedCells.Contains(position))
        {
            cellLabel = "X";
        }
        if (placedPiece == null)
        {
            return cellLabel;
        }

        string prefix = fixedPieces.Contains(placedPiece) ? "F" : "N";
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

        if (activeCells.Contains(position))
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
        EditorPlacedPiece solutionPiece = solutionPieces.Find(placedPiece => placedPiece.position == position);
        if (solutionPiece != null)
        {
            return solutionPiece;
        }

        return fixedPieces.Find(placedPiece => placedPiece.position == position);
    }

    private void RemoveOutOfBoundsState()
    {
        solutionPieces.RemoveAll(placedPiece => !IsInsideGrid(placedPiece.position));
        fixedPieces.RemoveAll(placedPiece => !IsInsideGrid(placedPiece.position));
        activeCells.RemoveWhere(position => !IsInsideGrid(position));
        generatedCells.RemoveWhere(position => !IsInsideGrid(position));
        loadedBoardCells.RemoveWhere(position => !IsInsideGrid(position));
        exactCoverCells.RemoveWhere(position => !IsInsideGrid(position));
        removedCells.RemoveWhere(position => !IsInsideGrid(position));

        List<Vector2Int> outOfBoundsRequiredCoverCounts = new List<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, int> requiredCoverCount in loadedRequiredCoverCounts)
        {
            if (!IsInsideGrid(requiredCoverCount.Key))
            {
                outOfBoundsRequiredCoverCounts.Add(requiredCoverCount.Key);
            }
        }

        foreach (Vector2Int position in outOfBoundsRequiredCoverCounts)
        {
            loadedRequiredCoverCounts.Remove(position);
        }
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
    }

    [System.Serializable]
    private class EditorPieceStock
    {
        public PieceData pieceData;
        public int count;
    }
}
