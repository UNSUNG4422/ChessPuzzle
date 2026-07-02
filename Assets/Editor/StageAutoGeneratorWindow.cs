using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class StageAutoGeneratorWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/Asset/StageData";

    [SerializeField] private DefaultAsset outputFolder;
    [SerializeField] private string stageNamePrefix = "AutoStage";
    [SerializeField] private int generateCount = 1;
    [SerializeField] private int boardWidth = 8;
    [SerializeField] private int boardHeight = 8;
    [SerializeField] private int pieceCount = 4;
    [SerializeField] private int randomSeed = 12345;
    [SerializeField] private bool includeExactCoverCells = true;
    [SerializeField, Range(0f, 1f)] private float exactCoverCellRate = 0.2f;
    [SerializeField, Range(1, 5)] private int maxExactCoverNumber = 3;
    [SerializeField] private bool includeFixedPieces;
    [SerializeField] private int fixedPieceCount;
    [SerializeField] private HelpPageType helpPageToUnlock = HelpPageType.None;
    [SerializeField] private HelpPageType helpPageToShowEveryTime = HelpPageType.None;
    [SerializeField] private bool registerToStageLoader = true;
    [SerializeField] private StageLoader targetStageLoader;
    [SerializeField] private List<PieceData> candidatePieces = new List<PieceData>();

    private Vector2 scrollPosition;

    [MenuItem("Tools/ChessPuzzle/Stage Auto Generator")]
    public static void Open()
    {
        GetWindow<StageAutoGeneratorWindow>("Stage Auto Generator");
    }

    private void OnEnable()
    {
        if (outputFolder == null)
        {
            outputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultOutputFolder);
        }

        if (candidatePieces.Count == 0)
        {
            LoadAllPieceDataAssets();
        }

        if (targetStageLoader == null)
        {
            targetStageLoader = UnityEngine.Object.FindFirstObjectByType<StageLoader>();
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);
        stageNamePrefix = EditorGUILayout.TextField("Stage Name Prefix", stageNamePrefix);
        generateCount = EditorGUILayout.IntField("Generate Count", generateCount);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Board", EditorStyles.boldLabel);
        boardWidth = EditorGUILayout.IntField("Board Width", boardWidth);
        boardHeight = EditorGUILayout.IntField("Board Height", boardHeight);
        pieceCount = EditorGUILayout.IntField("Piece Count", pieceCount);
        randomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Exact Cover Cells", EditorStyles.boldLabel);
        includeExactCoverCells = EditorGUILayout.Toggle("Include Exact Cover Cells", includeExactCoverCells);

        using (new EditorGUI.DisabledScope(!includeExactCoverCells))
        {
            exactCoverCellRate = EditorGUILayout.Slider("Exact Cover Cell Rate", exactCoverCellRate, 0f, 1f);
            maxExactCoverNumber = EditorGUILayout.IntSlider("Max Exact Cover Number", maxExactCoverNumber, 1, 5);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fixed Pieces", EditorStyles.boldLabel);
        includeFixedPieces = EditorGUILayout.Toggle("Include Fixed Pieces", includeFixedPieces);

        using (new EditorGUI.DisabledScope(!includeFixedPieces))
        {
            fixedPieceCount = EditorGUILayout.IntField("Fixed Piece Count", fixedPieceCount);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Help", EditorStyles.boldLabel);
        helpPageToUnlock = (HelpPageType)EditorGUILayout.EnumPopup("Help Page To Unlock", helpPageToUnlock);
        helpPageToShowEveryTime = (HelpPageType)EditorGUILayout.EnumPopup("Help Page To Show Every Time", helpPageToShowEveryTime);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Candidate Pieces", EditorStyles.boldLabel);
        DrawCandidatePieces();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("StageLoader", EditorStyles.boldLabel);
        registerToStageLoader = EditorGUILayout.Toggle("Register After Generate", registerToStageLoader);

        using (new EditorGUI.DisabledScope(!registerToStageLoader))
        {
            targetStageLoader = (StageLoader)EditorGUILayout.ObjectField("Target StageLoader", targetStageLoader, typeof(StageLoader), true);
        }

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!CanGenerate()))
        {
            if (GUILayout.Button("Generate StageData", GUILayout.Height(32f)))
            {
                GenerateStageDataAssets();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCandidatePieces()
    {
        for (int i = 0; i < candidatePieces.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            candidatePieces[i] = (PieceData)EditorGUILayout.ObjectField(candidatePieces[i], typeof(PieceData), false);

            if (GUILayout.Button("-", GUILayout.Width(24f)))
            {
                candidatePieces.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Candidate"))
        {
            candidatePieces.Add(null);
        }

        if (GUILayout.Button("Load All PieceData"))
        {
            LoadAllPieceDataAssets();
        }

        EditorGUILayout.EndHorizontal();
    }

    private bool CanGenerate()
    {
        int clampedFixedPieceCount = includeFixedPieces ? Mathf.Max(0, fixedPieceCount) : 0;

        return GetOutputFolderPath() != null
            && !string.IsNullOrWhiteSpace(stageNamePrefix)
            && generateCount > 0
            && boardWidth > 0
            && boardHeight > 0
            && pieceCount > 0
            && pieceCount <= boardWidth * boardHeight
            && clampedFixedPieceCount <= pieceCount
            && GetValidCandidatePieces().Count > 0;
    }

    private void GenerateStageDataAssets()
    {
        string folderPath = GetOutputFolderPath();

        if (folderPath == null)
        {
            EditorUtility.DisplayDialog("Stage Auto Generator", "Select an output folder under Assets.", "OK");
            return;
        }

        EnsureFolderExists(folderPath);

        List<PieceData> validCandidates = GetValidCandidatePieces();
        List<StageData> generatedStages = new List<StageData>();

        try
        {
            for (int i = 0; i < generateCount; i++)
            {
                StageData stageData = GenerateSingleStage(validCandidates, randomSeed + i, i + 1);
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{stageData.stageName}.asset");

                AssetDatabase.CreateAsset(stageData, assetPath);
                generatedStages.Add(stageData);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (registerToStageLoader)
            {
                RegisterStagesToStageLoader(generatedStages);
            }

            EditorUtility.DisplayDialog(
                "Stage Auto Generator",
                $"Generated {generatedStages.Count} StageData asset(s).",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Stage Auto Generator", exception.Message, "OK");
        }
    }

    private StageData GenerateSingleStage(List<PieceData> validCandidates, int seed, int serialNumber)
    {
        System.Random random = new System.Random(seed);
        Dictionary<Vector2Int, CellView> cellMap = CreateTemporaryCellMap(boardWidth, boardHeight);

        try
        {
            List<GeneratedPiece> placements = GenerateAnswerPlacements(validCandidates, random);
            MarkFixedPieces(placements, random);
            Dictionary<Vector2Int, int> coverCounts = CalculateCoverCounts(placements, cellMap);

            StageData stageData = CreateInstance<StageData>();
            stageData.stageName = $"{stageNamePrefix}_{serialNumber:000}";
            stageData.boardText = BuildBoardText(coverCounts, random);
            stageData.pieceStocks = BuildPieceStocks(placements);
            stageData.fixedPieces = BuildFixedPieces(placements);
            stageData.helpPageToUnlock = helpPageToUnlock;
            stageData.helpPageToShowEveryTime = helpPageToShowEveryTime;
            stageData.authorNote = BuildAuthorNote(seed, placements);

            return stageData;
        }
        finally
        {
            DestroyTemporaryCellMap(cellMap);
        }
    }

    private Dictionary<Vector2Int, CellView> CreateTemporaryCellMap(int width, int height)
    {
        Dictionary<Vector2Int, CellView> cellMap = new Dictionary<Vector2Int, CellView>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject cellObject = new GameObject($"TempCell_{x}_{y}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                CellView cellView = cellObject.AddComponent<CellView>();
                cellView.GridPosition = new Vector2Int(x, y);
                cellView.IsActive = true;
                cellMap.Add(cellView.GridPosition, cellView);
            }
        }

        return cellMap;
    }

    private List<GeneratedPiece> GenerateAnswerPlacements(List<PieceData> validCandidates, System.Random random)
    {
        List<GeneratedPiece> placements = new List<GeneratedPiece>();
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

        for (int i = 0; i < pieceCount; i++)
        {
            Vector2Int position = GetUnusedPosition(random, occupiedPositions);
            PieceData pieceData = validCandidates[random.Next(validCandidates.Count)];

            occupiedPositions.Add(position);
            placements.Add(new GeneratedPiece(pieceData, position));
        }

        return placements;
    }

    private void MarkFixedPieces(List<GeneratedPiece> placements, System.Random random)
    {
        if (!includeFixedPieces || fixedPieceCount <= 0 || placements.Count == 0)
        {
            return;
        }

        List<int> indices = new List<int>();

        for (int i = 0; i < placements.Count; i++)
        {
            indices.Add(i);
        }

        Shuffle(indices, random);

        int count = Mathf.Min(Mathf.Max(0, fixedPieceCount), placements.Count);

        for (int i = 0; i < count; i++)
        {
            placements[indices[i]].IsFixed = true;
        }
    }

    private Vector2Int GetUnusedPosition(System.Random random, HashSet<Vector2Int> occupiedPositions)
    {
        int maxAttempts = boardWidth * boardHeight * 2;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2Int position = new Vector2Int(random.Next(boardWidth), random.Next(boardHeight));

            if (!occupiedPositions.Contains(position))
            {
                return position;
            }
        }

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (!occupiedPositions.Contains(position))
                {
                    return position;
                }
            }
        }

        throw new InvalidOperationException("No empty cell is available for piece placement.");
    }

    private Dictionary<Vector2Int, int> CalculateCoverCounts(
        List<GeneratedPiece> placements,
        Dictionary<Vector2Int, CellView> cellMap
    )
    {
        Dictionary<Vector2Int, int> coverCounts = new Dictionary<Vector2Int, int>();

        foreach (GeneratedPiece placement in placements)
        {
            List<Vector2Int> positions = CoverageCalculator.GetCoveredPositions(
                placement.PieceData.pieceType,
                placement.Position,
                cellMap
            );

            foreach (Vector2Int position in positions)
            {
                if (coverCounts.ContainsKey(position))
                {
                    coverCounts[position]++;
                }
                else
                {
                    coverCounts.Add(position, 1);
                }
            }
        }

        return coverCounts;
    }

    private string BuildBoardText(Dictionary<Vector2Int, int> coverCounts, System.Random random)
    {
        HashSet<Vector2Int> exactCoverPositions = ChooseExactCoverPositions(coverCounts, random);
        StringBuilder builder = new StringBuilder();

        for (int y = boardHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (!coverCounts.TryGetValue(position, out int coverCount) || coverCount <= 0)
                {
                    builder.Append('X');
                }
                else if (exactCoverPositions.Contains(position))
                {
                    builder.Append((char)('0' + coverCount));
                }
                else
                {
                    builder.Append('Z');
                }
            }

            if (y > 0)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private HashSet<Vector2Int> ChooseExactCoverPositions(Dictionary<Vector2Int, int> coverCounts, System.Random random)
    {
        HashSet<Vector2Int> exactCoverPositions = new HashSet<Vector2Int>();

        if (!includeExactCoverCells || exactCoverCellRate <= 0f)
        {
            return exactCoverPositions;
        }

        List<Vector2Int> activePositions = new List<Vector2Int>();
        List<Vector2Int> exactCoverCandidates = new List<Vector2Int>();
        int maxCoverNumber = Mathf.Clamp(maxExactCoverNumber, 1, 5);

        foreach (KeyValuePair<Vector2Int, int> coverCount in coverCounts)
        {
            if (coverCount.Value <= 0)
            {
                continue;
            }

            activePositions.Add(coverCount.Key);

            if (coverCount.Value <= maxCoverNumber)
            {
                exactCoverCandidates.Add(coverCount.Key);
            }
        }

        int targetExactCoverCount = Mathf.RoundToInt(activePositions.Count * Mathf.Clamp01(exactCoverCellRate));
        targetExactCoverCount = Mathf.Min(targetExactCoverCount, exactCoverCandidates.Count);

        Shuffle(exactCoverCandidates, random);

        for (int i = 0; i < targetExactCoverCount; i++)
        {
            exactCoverPositions.Add(exactCoverCandidates[i]);
        }

        return exactCoverPositions;
    }

    private void Shuffle<T>(List<T> items, System.Random random)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            T current = items[i];
            items[i] = items[swapIndex];
            items[swapIndex] = current;
        }
    }

    private List<StagePieceStock> BuildPieceStocks(List<GeneratedPiece> placements)
    {
        Dictionary<PieceData, int> stockCounts = new Dictionary<PieceData, int>();

        foreach (GeneratedPiece placement in placements)
        {
            if (placement.IsFixed)
            {
                continue;
            }

            if (stockCounts.ContainsKey(placement.PieceData))
            {
                stockCounts[placement.PieceData]++;
            }
            else
            {
                stockCounts.Add(placement.PieceData, 1);
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

    private List<FixedPieceData> BuildFixedPieces(List<GeneratedPiece> placements)
    {
        List<FixedPieceData> fixedPieces = new List<FixedPieceData>();

        foreach (GeneratedPiece placement in placements)
        {
            if (!placement.IsFixed)
            {
                continue;
            }

            fixedPieces.Add(new FixedPieceData
            {
                pieceData = placement.PieceData,
                position = placement.Position
            });
        }

        return fixedPieces;
    }

    private string BuildAuthorNote(int seed, List<GeneratedPiece> placements)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Auto generated stage.");
        builder.AppendLine($"Seed: {seed}");
        builder.AppendLine("Answer placements:");

        foreach (GeneratedPiece placement in placements)
        {
            string fixedLabel = placement.IsFixed ? " [Fixed]" : string.Empty;
            builder.AppendLine($"{placement.PieceData.pieceType} at {placement.Position}{fixedLabel}");
        }

        return builder.ToString();
    }

    private void DestroyTemporaryCellMap(Dictionary<Vector2Int, CellView> cellMap)
    {
        foreach (CellView cellView in cellMap.Values)
        {
            if (cellView != null)
            {
                DestroyImmediate(cellView.gameObject);
            }
        }
    }

    private void RegisterStagesToStageLoader(List<StageData> generatedStages)
    {
        StageLoader stageLoader = targetStageLoader != null
            ? targetStageLoader
            : UnityEngine.Object.FindFirstObjectByType<StageLoader>();

        if (stageLoader == null)
        {
            Debug.LogWarning("StageLoader was not found. Generated stages were not registered automatically.");
            return;
        }

        SerializedObject serializedStageLoader = new SerializedObject(stageLoader);
        SerializedProperty stagesProperty = serializedStageLoader.FindProperty("stages");

        if (stagesProperty == null || !stagesProperty.isArray)
        {
            Debug.LogWarning("StageLoader.stages was not found. Generated stages were not registered automatically.");
            return;
        }

        Undo.RecordObject(stageLoader, "Register Generated StageData");

        foreach (StageData generatedStage in generatedStages)
        {
            if (ContainsStage(stagesProperty, generatedStage))
            {
                continue;
            }

            int index = stagesProperty.arraySize;
            stagesProperty.InsertArrayElementAtIndex(index);
            stagesProperty.GetArrayElementAtIndex(index).objectReferenceValue = generatedStage;
        }

        serializedStageLoader.ApplyModifiedProperties();
        EditorUtility.SetDirty(stageLoader);

        if (stageLoader.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(stageLoader.gameObject.scene);
        }
    }

    private bool ContainsStage(SerializedProperty stagesProperty, StageData stageData)
    {
        for (int i = 0; i < stagesProperty.arraySize; i++)
        {
            if (stagesProperty.GetArrayElementAtIndex(i).objectReferenceValue == stageData)
            {
                return true;
            }
        }

        return false;
    }

    private List<PieceData> GetValidCandidatePieces()
    {
        List<PieceData> validPieces = new List<PieceData>();

        foreach (PieceData pieceData in candidatePieces)
        {
            if (pieceData != null && !validPieces.Contains(pieceData))
            {
                validPieces.Add(pieceData);
            }
        }

        return validPieces;
    }

    private void LoadAllPieceDataAssets()
    {
        candidatePieces.Clear();

        string[] guids = AssetDatabase.FindAssets("t:PieceData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PieceData pieceData = AssetDatabase.LoadAssetAtPath<PieceData>(path);

            if (pieceData != null)
            {
                candidatePieces.Add(pieceData);
            }
        }
    }

    private string GetOutputFolderPath()
    {
        if (outputFolder == null)
        {
            return null;
        }

        string folderPath = AssetDatabase.GetAssetPath(outputFolder);

        if (string.IsNullOrEmpty(folderPath) || !folderPath.StartsWith("Assets"))
        {
            return null;
        }

        return AssetDatabase.IsValidFolder(folderPath) ? folderPath : null;
    }

    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }

    private class GeneratedPiece
    {
        public GeneratedPiece(PieceData pieceData, Vector2Int position)
        {
            PieceData = pieceData;
            Position = position;
        }

        public PieceData PieceData { get; }
        public Vector2Int Position { get; }
        public bool IsFixed { get; set; }
    }
}
