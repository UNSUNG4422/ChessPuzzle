using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class StageAutoGeneratorWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/Asset/StageData";
    private const int MaxGenerationAttempts = 80;

    [SerializeField] private DefaultAsset outputFolder;
    [SerializeField] private string stageNamePrefix = "AutoStage";
    [SerializeField] private GenerationMode generationMode = GenerationMode.ManualPieceSet;
    [SerializeField] private int generateCount = 1;
    [SerializeField] private int randomSeed = 12345;
    [SerializeField] private bool useRandomSeed;

    [SerializeField] private int boardWidth = 8;
    [SerializeField] private int boardHeight = 8;
    [SerializeField] private int pieceCount = 4;
    [SerializeField] private int minPieceCount = 3;
    [SerializeField] private int preferredMinPieceCount = 4;
    [SerializeField] private int maxPieceCount = 5;
    [SerializeField] private bool includeExactCoverCells = true;
    [SerializeField, Range(0f, 1f)] private float exactCoverCellRate = 0.2f;
    [SerializeField, Range(1, 5)] private int maxExactCoverNumber = 3;
    [SerializeField] private bool includeFixedPieces;
    [SerializeField] private int fixedPieceCount;
    [SerializeField, Range(0f, 0.75f)] private float removedCellRate;

    [SerializeField] private int minWidth = 5;
    [SerializeField] private int maxWidth = 10;
    [SerializeField] private int minHeight = 5;
    [SerializeField] private int maxHeight = 10;
    [SerializeField, Range(1, 5)] private int minDifficulty = 1;
    [SerializeField, Range(1, 5)] private int maxDifficulty = 5;
    [SerializeField] private bool allowExactCoverCells = true;
    [SerializeField] private bool allowFixedPieces = true;
    [SerializeField] private bool allowQueen = true;
    [SerializeField] private List<GenerationProfile> allowedProfiles = new List<GenerationProfile>();

    [SerializeField] private StageCategory stageCategory = StageCategory.Standard;
    [SerializeField] private HelpPageType helpPageToUnlock = HelpPageType.None;
    [SerializeField] private HelpPageType helpPageToShowEveryTime = HelpPageType.None;
    [SerializeField] private bool registerToStageLoader = true;
    [SerializeField] private StageLoader targetStageLoader;
    [SerializeField] private List<PieceData> candidatePieces = new List<PieceData>();
    [SerializeField] private int candidateCount = 100;
    [SerializeField] private int desiredOutputCount = 10;
    [SerializeField] private int maxSolutions = 100;
    [SerializeField] private bool requireAllPieces = true;
    [SerializeField] private int maxAcceptedSolutions = 20;
    [SerializeField] private bool enableExactGeneration;
    [SerializeField, Range(0f, 1f)] private float exactGenerationCellRate = 0.45f;
    [SerializeField] private int minExactCellCount = 3;
    [SerializeField, Range(1, 5)] private int exactGenerationMaxExactNumber = 5;
    [SerializeField] private bool preferHigherExactNumbers = true;
    [SerializeField] private bool enableAdvancedExactFixedGeneration;
    [SerializeField] private bool enableFixedPieceGeneration = true;
    [SerializeField] private int minFixedPieceCount = 1;
    [SerializeField] private int maxFixedPieceCount = 3;
    [SerializeField] private int minTotalSolutionPieceCount = 7;
    [SerializeField] private int maxTotalSolutionPieceCount = 10;
    [SerializeField, Range(0f, 1f)] private float advancedExactCellRate = 0.55f;
    [SerializeField] private int minAdvancedExactCellCount = 8;

    private Vector2 scrollPosition;

    private enum GenerationMode
    {
        ManualPieceSet,
        RandomProfile
    }

    public enum GenerationProfile
    {
        PawnIntro,
        KnightPuzzle,
        LinePieces,
        DiagonalPieces,
        MixedSmall,
        QueenPower,
        SparseBoard,
        ManySmallPieces,
        ExactCoverIntro,
        FixedPieceIntro,
        AdvancedMixed
    }

    public enum BoardShapeBias
    {
        Balanced,
        Wide,
        Tall,
        Sparse,
        Dense,
        Diagonal,
        CrossLike,
        IslandLike
    }

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

        if (allowedProfiles.Count == 0)
        {
            allowedProfiles.AddRange((GenerationProfile[])Enum.GetValues(typeof(GenerationProfile)));
        }

        if (targetStageLoader == null)
        {
            targetStageLoader = UnityEngine.Object.FindFirstObjectByType<StageLoader>();
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawOutputSettings();
        DrawGenerationSettings();
        DrawSharedStageSettings();
        DrawCandidatePieces();
        DrawStageLoaderSettings();
        DrawSolverSettings();
        DrawGenerateButton();

        EditorGUILayout.EndScrollView();
    }

    private void DrawOutputSettings()
    {
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);
        stageNamePrefix = EditorGUILayout.TextField("Stage Name Prefix", stageNamePrefix);
        generationMode = (GenerationMode)EditorGUILayout.EnumPopup("Generation Mode", generationMode);
        generateCount = EditorGUILayout.IntField("Number Of Stages", generateCount);
        useRandomSeed = EditorGUILayout.Toggle("Use Random Seed", useRandomSeed);

        using (new EditorGUI.DisabledScope(useRandomSeed))
        {
            randomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);
        }
    }

    private void DrawGenerationSettings()
    {
        EditorGUILayout.Space();

        if (generationMode == GenerationMode.ManualPieceSet)
        {
            DrawManualSettings();
        }
        else
        {
            DrawRandomProfileSettings();
        }
    }

    private void DrawManualSettings()
    {
        EditorGUILayout.LabelField("Manual Piece Set", EditorStyles.boldLabel);
        boardWidth = EditorGUILayout.IntField("Board Width", boardWidth);
        boardHeight = EditorGUILayout.IntField("Board Height", boardHeight);
        pieceCount = EditorGUILayout.IntField("Piece Count", pieceCount);
        minPieceCount = EditorGUILayout.IntField("Min Piece Count", minPieceCount);
        preferredMinPieceCount = EditorGUILayout.IntField("Preferred Min Piece Count", preferredMinPieceCount);
        maxPieceCount = EditorGUILayout.IntField("Max Piece Count", maxPieceCount);

        EditorGUILayout.Space();
        includeExactCoverCells = EditorGUILayout.Toggle("Include Exact Cover Cells", includeExactCoverCells);

        using (new EditorGUI.DisabledScope(!includeExactCoverCells))
        {
            exactCoverCellRate = EditorGUILayout.Slider("Exact Cover Cell Rate", exactCoverCellRate, 0f, 1f);
            maxExactCoverNumber = EditorGUILayout.IntSlider("Max Exact Cover Number", maxExactCoverNumber, 1, 5);
        }

        includeFixedPieces = EditorGUILayout.Toggle("Include Fixed Pieces", includeFixedPieces);

        using (new EditorGUI.DisabledScope(!includeFixedPieces))
        {
            fixedPieceCount = EditorGUILayout.IntField("Fixed Piece Count", fixedPieceCount);
        }

        removedCellRate = EditorGUILayout.Slider("Removed Cell Rate", removedCellRate, 0f, 0.75f);
    }

    private void DrawRandomProfileSettings()
    {
        EditorGUILayout.LabelField("Random Profile", EditorStyles.boldLabel);
        minWidth = EditorGUILayout.IntField("Min Width", minWidth);
        maxWidth = EditorGUILayout.IntField("Max Width", maxWidth);
        minHeight = EditorGUILayout.IntField("Min Height", minHeight);
        maxHeight = EditorGUILayout.IntField("Max Height", maxHeight);
        minPieceCount = EditorGUILayout.IntField("Min Piece Count", minPieceCount);
        preferredMinPieceCount = EditorGUILayout.IntField("Preferred Min Piece Count", preferredMinPieceCount);
        maxPieceCount = EditorGUILayout.IntField("Max Piece Count", maxPieceCount);
        minDifficulty = EditorGUILayout.IntSlider("Min Difficulty", minDifficulty, 1, 5);
        maxDifficulty = EditorGUILayout.IntSlider("Max Difficulty", maxDifficulty, 1, 5);
        allowExactCoverCells = EditorGUILayout.Toggle("Allow Exact Cover Cells", allowExactCoverCells);
        allowFixedPieces = EditorGUILayout.Toggle("Allow Fixed Pieces", allowFixedPieces);
        allowQueen = EditorGUILayout.Toggle("Allow Queen", allowQueen);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Allowed Profiles", EditorStyles.boldLabel);

        foreach (GenerationProfile profile in (GenerationProfile[])Enum.GetValues(typeof(GenerationProfile)))
        {
            bool enabled = allowedProfiles.Contains(profile);
            bool nextEnabled = EditorGUILayout.Toggle(profile.ToString(), enabled);

            if (nextEnabled && !enabled)
            {
                allowedProfiles.Add(profile);
            }
            else if (!nextEnabled && enabled)
            {
                allowedProfiles.Remove(profile);
            }
        }
    }

    private void DrawSharedStageSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("StageData Fields", EditorStyles.boldLabel);
        stageCategory = (StageCategory)EditorGUILayout.EnumPopup("Stage Category", stageCategory);
        helpPageToUnlock = (HelpPageType)EditorGUILayout.EnumPopup("Help Page To Unlock", helpPageToUnlock);
        helpPageToShowEveryTime = (HelpPageType)EditorGUILayout.EnumPopup("Help Page To Show Every Time", helpPageToShowEveryTime);
    }

    private void DrawCandidatePieces()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Candidate Pieces", EditorStyles.boldLabel);

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

    private void DrawStageLoaderSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("StageLoader", EditorStyles.boldLabel);
        registerToStageLoader = EditorGUILayout.Toggle("Register After Generate", registerToStageLoader);

        using (new EditorGUI.DisabledScope(!registerToStageLoader))
        {
            targetStageLoader = (StageLoader)EditorGUILayout.ObjectField("Target StageLoader", targetStageLoader, typeof(StageLoader), true);
        }
    }

    private void DrawSolverSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Generate + Solve Evaluation", EditorStyles.boldLabel);
        candidateCount = EditorGUILayout.IntField("Candidate Count", candidateCount);
        desiredOutputCount = EditorGUILayout.IntField("Desired Output Count", desiredOutputCount);
        maxSolutions = EditorGUILayout.IntField("Max Solutions", maxSolutions);
        requireAllPieces = EditorGUILayout.Toggle("Require All Pieces", requireAllPieces);
        maxAcceptedSolutions = EditorGUILayout.IntField("Max Accepted Solutions", maxAcceptedSolutions);
        enableExactGeneration = EditorGUILayout.Toggle("Enable Exact Generation", enableExactGeneration);

        using (new EditorGUI.DisabledScope(!enableExactGeneration))
        {
            exactGenerationCellRate = EditorGUILayout.Slider("Exact Cell Rate", exactGenerationCellRate, 0f, 1f);
            minExactCellCount = EditorGUILayout.IntField("Min Exact Cell Count", minExactCellCount);
            exactGenerationMaxExactNumber = EditorGUILayout.IntSlider("Max Exact Number", exactGenerationMaxExactNumber, 1, 5);
            preferHigherExactNumbers = EditorGUILayout.Toggle("Prefer Higher Exact Numbers", preferHigherExactNumbers);
        }

        enableAdvancedExactFixedGeneration = EditorGUILayout.Toggle("Advanced Exact Fixed Generation", enableAdvancedExactFixedGeneration);

        using (new EditorGUI.DisabledScope(!enableAdvancedExactFixedGeneration))
        {
            enableFixedPieceGeneration = EditorGUILayout.Toggle("Enable Fixed Piece Generation", enableFixedPieceGeneration);
            minFixedPieceCount = EditorGUILayout.IntField("Min Fixed Piece Count", minFixedPieceCount);
            maxFixedPieceCount = EditorGUILayout.IntField("Max Fixed Piece Count", maxFixedPieceCount);
            minTotalSolutionPieceCount = EditorGUILayout.IntField("Min Total Solution Piece Count", minTotalSolutionPieceCount);
            maxTotalSolutionPieceCount = EditorGUILayout.IntField("Max Total Solution Piece Count", maxTotalSolutionPieceCount);
            advancedExactCellRate = EditorGUILayout.Slider("Advanced Exact Cell Rate", advancedExactCellRate, 0f, 1f);
            minAdvancedExactCellCount = EditorGUILayout.IntField("Min Advanced Exact Cell Count", minAdvancedExactCellCount);
        }

        EditorGUILayout.HelpBox("Generate And Solve uses Standard growth generation by default. Enable Exact Generation to add exact cover cells. Advanced Exact Fixed Generation adds fixed pieces and stricter exact scoring.", MessageType.Info);
    }

    private void DrawGenerateButton()
    {
        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!CanGenerate()))
        {
            string label = generationMode == GenerationMode.RandomProfile ? "Generate Batch" : "Generate StageData";

            if (GUILayout.Button(label, GUILayout.Height(32f)))
            {
                GenerateStageDataAssets();
            }

            if (GUILayout.Button("Generate And Solve", GUILayout.Height(32f)))
            {
                GenerateAndSolveStageDataAssets();
            }
        }
    }

    private bool CanGenerate()
    {
        int clampedFixedPieceCount = includeFixedPieces ? Mathf.Max(0, fixedPieceCount) : 0;
        bool hasValidManualBoard = boardWidth > 0
            && boardHeight > 0
            && pieceCount > 0
            && pieceCount <= boardWidth * boardHeight
            && clampedFixedPieceCount <= pieceCount;
        bool hasValidRandomProfile = minWidth > 0
            && maxWidth >= minWidth
            && minHeight > 0
            && maxHeight >= minHeight
            && minDifficulty <= maxDifficulty
            && allowedProfiles.Count > 0;

        return GetOutputFolderPath() != null
            && !string.IsNullOrWhiteSpace(stageNamePrefix)
            && generateCount > 0
            && GetValidCandidatePieces().Count > 0
            && (generationMode == GenerationMode.ManualPieceSet ? hasValidManualBoard : hasValidRandomProfile);
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
        int baseSeed = useRandomSeed ? Environment.TickCount : randomSeed;
        System.Random batchRandom = new System.Random(baseSeed);
        GenerationProfile? previousProfile = null;
        string previousPieceSignature = string.Empty;
        string previousBoardText = string.Empty;

        try
        {
            for (int i = 0; i < generateCount; i++)
            {
                StageData stageData = null;

                for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
                {
                    int seed = batchRandom.Next();
                    StageGenerationSettings settings = generationMode == GenerationMode.ManualPieceSet
                        ? CreateManualSettings(seed)
                        : CreateRandomProfileSettings(validCandidates, batchRandom, previousProfile, previousPieceSignature, seed);

                    stageData = GenerateSingleStage(validCandidates, settings, i + 1);

                    if (stageData == null
                        || !IsValidGeneratedStage(stageData, settings)
                        || IsTooSimilarToPrevious(stageData, settings, previousProfile, previousPieceSignature, previousBoardText))
                    {
                        if (stageData != null)
                        {
                            DestroyImmediate(stageData);
                        }

                        stageData = null;
                        continue;
                    }

                    previousProfile = settings.profile;
                    previousPieceSignature = settings.GetPieceSignature();
                    previousBoardText = stageData.boardText;
                    break;
                }

                if (stageData == null)
                {
                    throw new InvalidOperationException($"Failed to generate a valid stage for serial {i + 1}.");
                }

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
                $"Generated {generatedStages.Count} StageData asset(s). Seed: {baseSeed}",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Stage Auto Generator", exception.Message, "OK");
        }
    }

    private void GenerateAndSolveStageDataAssets()
    {
        string folderPath = GetOutputFolderPath();

        if (folderPath == null)
        {
            EditorUtility.DisplayDialog("Stage Auto Generator", "Select an output folder under Assets.", "OK");
            return;
        }

        EnsureFolderExists(folderPath);

        List<PieceData> validCandidates = GetValidCandidatePieces();
        List<EvaluatedStageCandidate> acceptedCandidates = new List<EvaluatedStageCandidate>();
        int baseSeed = useRandomSeed ? Environment.TickCount : randomSeed;
        System.Random batchRandom = new System.Random(baseSeed);
        GenerationProfile? previousProfile = null;
        string previousPieceSignature = string.Empty;
        string previousBoardText = string.Empty;
        int generated = 0;
        int solvable = 0;
        int requiresAllPiecesCount = 0;

        try
        {
            for (int i = 0; i < Mathf.Max(1, candidateCount); i++)
            {
                int seed = batchRandom.Next();
                StageGenerationSettings settings = generationMode == GenerationMode.ManualPieceSet
                    ? CreateManualSettings(seed)
                    : CreateRandomProfileSettings(validCandidates, batchRandom, previousProfile, previousPieceSignature, seed);
                bool useExactGeneration = enableExactGeneration || enableAdvancedExactFixedGeneration;
                if (enableAdvancedExactFixedGeneration)
                {
                    ForceAdvancedExactFixedSolverSettings(settings, batchRandom);
                }
                else if (enableExactGeneration)
                {
                    ForceExactSolverSettings(settings);
                }
                else
                {
                    ForceStandardSolverSettings(settings);
                }

                StageData stageData = useExactGeneration
                    ? GenerateExactCoverStage(validCandidates, settings, i + 1)
                    : GenerateGrowingStandardStage(validCandidates, settings, i + 1);
                generated++;

                if (stageData == null || !IsValidGeneratedStage(stageData, settings))
                {
                    DestroyStageCandidate(stageData);
                    continue;
                }

                stageData.stageCategory = enableAdvancedExactFixedGeneration
                    ? StageCategory.Advanced
                    : enableExactGeneration
                        ? StageCategory.ExactCover
                        : StageCategory.Standard;

                SolverResult solverResult = StageSolver.Solve(stageData, Mathf.Max(1, maxSolutions));
                SolverResult noExactSolverResult = useExactGeneration
                    ? StageSolver.Solve(CreateNoExactClone(stageData), Mathf.Max(1, maxSolutions))
                    : null;

                if (solverResult.isSolvable)
                {
                    solvable++;
                }

                if (solverResult.requiresAllPieces)
                {
                    requiresAllPiecesCount++;
                }

                StageEvaluation evaluation = useExactGeneration
                    ? StageEvaluator.EvaluateExact(
                        stageData,
                        solverResult,
                        noExactSolverResult,
                        requireAllPieces,
                        Mathf.Max(1, maxAcceptedSolutions),
                        enableAdvancedExactFixedGeneration ? Mathf.Max(1, minTotalSolutionPieceCount - Mathf.Max(0, maxFixedPieceCount)) : minPieceCount,
                        enableAdvancedExactFixedGeneration ? minAdvancedExactCellCount : minExactCellCount,
                        exactGenerationMaxExactNumber,
                        enableAdvancedExactFixedGeneration,
                        minFixedPieceCount,
                        minTotalSolutionPieceCount
                    )
                    : StageEvaluator.Evaluate(
                        stageData,
                        solverResult,
                        requireAllPieces,
                        Mathf.Max(1, maxAcceptedSolutions),
                        minPieceCount
                    );

                if (!evaluation.accepted)
                {
                    if (!string.IsNullOrEmpty(evaluation.rejectReason))
                    {
                        Debug.Log($"[GEN DEBUG] rejected: {evaluation.rejectReason}");
                    }

                    DestroyStageCandidate(stageData);
                    continue;
                }

                acceptedCandidates.Add(new EvaluatedStageCandidate
                {
                    stageData = stageData,
                    settings = settings,
                    solverResult = solverResult,
                    evaluation = evaluation,
                    score = evaluation.score
                });

                Debug.Log(
                    $"[GEN DEBUG] accepted: pieces={evaluation.totalNormalPieceCount}, solutions={solverResult.solutionCount}, score={evaluation.score}"
                );

                if (useExactGeneration)
                {
                    string noExactSolutionsText = noExactSolverResult != null && noExactSolverResult.solutionCount >= Mathf.Max(1, maxSolutions)
                        ? $"{noExactSolverResult.solutionCount}+"
                        : noExactSolverResult?.solutionCount.ToString();
                    Debug.Log(
                        $"[{(enableAdvancedExactFixedGeneration ? "ADV EXACT FIXED GEN" : "EXACT GEN")}] noExact solutions={noExactSolutionsText}, exact solutions={solverResult.solutionCount}, "
                        + $"exactCells={evaluation.exactCellCount}, normalPieces={evaluation.totalNormalPieceCount}, fixedPieces={evaluation.fixedPieceCount}, accepted"
                    );
                }
                Debug.Log(
                    $"[GEN EVAL] solutionCount={solverResult.solutionCount}, minPiecesUsed={solverResult.minPiecesUsed}, "
                    + $"requiresAllPieces={solverResult.requiresAllPieces}, constrainedCellCount={evaluation.constrainedCellCount}, "
                    + $"minCoverCandidateCount={evaluation.minCoverCandidateCount}, shapeHintScore={evaluation.shapeHintScore}, "
                    + $"finalScore={evaluation.score}"
                );

                previousProfile = settings.profile;
                previousPieceSignature = settings.GetPieceSignature();
                previousBoardText = stageData.boardText;
            }

            acceptedCandidates.Sort((left, right) => right.score.CompareTo(left.score));

            if (enableExactGeneration || enableAdvancedExactFixedGeneration)
            {
                LogExactGenerationResults(acceptedCandidates, Mathf.Min(10, desiredOutputCount));
            }

            List<StageData> savedStages = new List<StageData>();
            int saveCount = Mathf.Min(Mathf.Max(1, desiredOutputCount), acceptedCandidates.Count);

            for (int i = 0; i < acceptedCandidates.Count; i++)
            {
                EvaluatedStageCandidate candidate = acceptedCandidates[i];

                if (i >= saveCount)
                {
                    DestroyStageCandidate(candidate.stageData);
                    continue;
                }

                candidate.stageData.stageName = $"{stageNamePrefix}_{i + 1:000}";
                candidate.stageData.authorNote += BuildSolverNote(candidate.solverResult, candidate.evaluation);

                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{candidate.stageData.stageName}.asset");
                AssetDatabase.CreateAsset(candidate.stageData, assetPath);
                savedStages.Add(candidate.stageData);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (registerToStageLoader)
            {
                RegisterStagesToStageLoader(savedStages);
            }

            Debug.Log($"Generated {generated} candidates");
            Debug.Log($"Solvable {solvable}");
            Debug.Log($"Requires all pieces {requiresAllPiecesCount}");
            Debug.Log($"Accepted {acceptedCandidates.Count}");
            Debug.Log($"Saved {savedStages.Count} stages");

            EditorUtility.DisplayDialog(
                "Stage Auto Generator",
                $"Generated {generated} candidates\nSolvable {solvable}\nRequires all pieces {requiresAllPiecesCount}\nAccepted {acceptedCandidates.Count}\nSaved {savedStages.Count} stages",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Stage Auto Generator", exception.Message, "OK");
        }
    }

    private void ForceStandardSolverSettings(StageGenerationSettings settings)
    {
        settings.includeExactCoverCells = false;
        settings.exactCoverCellRate = 0f;
        settings.includeFixedPieces = false;
        settings.fixedPieceCount = 0;
        settings.isAdvancedExactFixed = false;
    }

    private void ForceExactSolverSettings(StageGenerationSettings settings)
    {
        settings.includeExactCoverCells = true;
        settings.exactCoverCellRate = exactGenerationCellRate;
        settings.maxExactCoverNumber = exactGenerationMaxExactNumber;
        settings.includeFixedPieces = false;
        settings.fixedPieceCount = 0;
        settings.isAdvancedExactFixed = false;
    }

    private void ForceAdvancedExactFixedSolverSettings(StageGenerationSettings settings, System.Random random)
    {
        int minTotal = Mathf.Max(2, minTotalSolutionPieceCount);
        int maxTotal = Mathf.Max(minTotal, maxTotalSolutionPieceCount);
        int totalPieceCount = RandomRangeInclusive(random, minTotal, maxTotal);
        int minFixed = enableFixedPieceGeneration ? Mathf.Max(1, minFixedPieceCount) : 0;
        int maxFixed = enableFixedPieceGeneration ? Mathf.Max(minFixed, maxFixedPieceCount) : 0;

        settings.isAdvancedExactFixed = true;
        settings.includeExactCoverCells = true;
        settings.exactCoverCellRate = Mathf.Clamp01(advancedExactCellRate);
        settings.maxExactCoverNumber = exactGenerationMaxExactNumber;
        settings.includeFixedPieces = enableFixedPieceGeneration && maxFixed > 0;
        settings.fixedPieceCount = settings.includeFixedPieces
            ? Mathf.Clamp(RandomRangeInclusive(random, minFixed, maxFixed), 1, Mathf.Max(1, totalPieceCount - 1))
            : 0;
        settings.pieceCount = totalPieceCount;
        settings.width = Mathf.Clamp(Mathf.Max(settings.width, 7), minWidth, maxWidth);
        settings.height = Mathf.Clamp(Mathf.Max(settings.height, 7), minHeight, maxHeight);
        settings.profile = GenerationProfile.AdvancedMixed;
        settings.pieceSequence = BuildProfilePieceSequence(GetValidCandidatePieces(), settings, random);
    }

    private StageData CreateNoExactClone(StageData source)
    {
        if (source == null)
        {
            return null;
        }

        StageData clone = CreateInstance<StageData>();
        clone.stageName = source.stageName;
        clone.stageCategory = source.stageCategory;
        clone.boardText = ReplaceExactCellsWithNormalCells(source.boardText);
        clone.pieceStocks = source.pieceStocks != null
            ? new List<StagePieceStock>(source.pieceStocks)
            : new List<StagePieceStock>();
        clone.fixedPieces = source.fixedPieces != null
            ? new List<FixedPieceData>(source.fixedPieces)
            : new List<FixedPieceData>();
        return clone;
    }

    private string ReplaceExactCellsWithNormalCells(string boardText)
    {
        if (string.IsNullOrEmpty(boardText))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(boardText.Length);

        foreach (char character in boardText)
        {
            builder.Append(character >= '1' && character <= '5' ? 'Z' : character);
        }

        return builder.ToString();
    }

    private string BuildSolverNote(SolverResult solverResult, StageEvaluation evaluation)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine();
        builder.AppendLine("Solver evaluation:");
        builder.AppendLine($"Final Score: {evaluation.score}");
        builder.AppendLine($"Solvable: {solverResult.isSolvable}");
        builder.AppendLine($"Solution Count: {solverResult.solutionCount}");
        builder.AppendLine($"Min Pieces Used: {solverResult.minPiecesUsed}");
        builder.AppendLine($"Requires All Pieces: {solverResult.requiresAllPieces}");
        builder.AppendLine($"Constrained Cell Count: {evaluation.constrainedCellCount}");
        builder.AppendLine($"Min Cover Candidate Count: {evaluation.minCoverCandidateCount}");
        builder.AppendLine($"Shape Hint Score: {evaluation.shapeHintScore}");
        builder.AppendLine($"Total Normal Piece Count: {evaluation.totalNormalPieceCount}");
        builder.AppendLine($"Fixed Piece Count: {evaluation.fixedPieceCount}");
        builder.AppendLine($"Exact Cell Count: {evaluation.exactCellCount}");
        builder.AppendLine($"Exact Cell Rate: {evaluation.exactCellRate:0.00}");
        builder.AppendLine($"Exact Number Variety: {evaluation.exactNumberVariety}");
        builder.AppendLine($"No Exact Solution Count: {evaluation.noExactSolutionCount}");
        builder.AppendLine($"Solution Score: {evaluation.solutionScore}");
        builder.AppendLine($"All Pieces Score: {evaluation.allPiecesScore}");
        builder.AppendLine($"Exact Cell Count Score: {evaluation.exactCellCountScore}");
        builder.AppendLine($"Exact Number Variety Score: {evaluation.exactNumberVarietyScore}");
        builder.AppendLine($"Solution Reduction Score: {evaluation.solutionReductionScore}");
        builder.AppendLine($"Piece Role Score: {evaluation.pieceRoleScore}");
        builder.AppendLine($"Fixed Piece Constraint Score: {evaluation.fixedPieceConstraintScore}");
        builder.AppendLine($"Board Shape Score: {evaluation.boardShapeScore}");
        builder.AppendLine($"Connected Component Count: {evaluation.connectedComponentCount}");
        builder.AppendLine($"Small Island Count: {evaluation.smallIslandCount}");
        builder.AppendLine($"Small Island Penalty: {evaluation.smallIslandPenalty}");
        builder.AppendLine($"Isolated Piece Penalty: {evaluation.isolatedPiecePenalty}");
        builder.AppendLine($"Piece Spread Penalty: {evaluation.pieceSpreadPenalty}");
        builder.AppendLine($"Tiny Island With Piece Penalty: {evaluation.tinyIslandWithPiecePenalty}");
        builder.AppendLine($"Exact On Tiny Island Penalty: {evaluation.exactOnTinyIslandPenalty}");
        builder.AppendLine($"Unnatural Long Range Penalty: {evaluation.unnaturalLongRangePenalty}");
        return builder.ToString();
    }

    private void LogExactGenerationResults(List<EvaluatedStageCandidate> acceptedCandidates, int count)
    {
        if (acceptedCandidates.Count == 0)
        {
            Debug.Log("[EXACT GEN RESULT] No accepted candidates.");
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[EXACT GEN RESULT]");

        for (int i = 0; i < Mathf.Min(count, acceptedCandidates.Count); i++)
        {
            EvaluatedStageCandidate candidate = acceptedCandidates[i];
            StageEvaluation evaluation = candidate.evaluation;
            SolverResult solverResult = candidate.solverResult;
            string noExactSolutionsText = evaluation.noExactSolutionCount >= Mathf.Max(1, maxSolutions)
                ? $"{evaluation.noExactSolutionCount}+"
                : evaluation.noExactSolutionCount.ToString();

            builder.AppendLine(
                $"Rank {i + 1} score={evaluation.score} solutions={solverResult.solutionCount} "
                + $"normalPieces={evaluation.totalNormalPieceCount} fixedPieces={evaluation.fixedPieceCount} exact={evaluation.exactCellCount} "
                + $"components={evaluation.connectedComponentCount} islandPenalty={evaluation.smallIslandPenalty} "
                + $"fixedPieceConstraintScore={evaluation.fixedPieceConstraintScore} "
                + $"isolatedPiecePenalty={evaluation.isolatedPiecePenalty} tinyIslandWithPiecePenalty={evaluation.tinyIslandWithPiecePenalty} "
                + $"exactOnTinyIslandPenalty={evaluation.exactOnTinyIslandPenalty} rate={evaluation.exactCellRate:0.00} variety={evaluation.exactNumberVariety} "
                + $"reduction={noExactSolutionsText}->{solverResult.solutionCount}"
            );
        }

        Debug.Log(builder.ToString());
    }

    private void DestroyStageCandidate(StageData stageData)
    {
        if (stageData != null)
        {
            DestroyImmediate(stageData);
        }
    }

    private StageGenerationSettings CreateManualSettings(int seed)
    {
        StageGenerationSettings settings = new StageGenerationSettings
        {
            mode = GenerationMode.ManualPieceSet,
            profile = GenerationProfile.MixedSmall,
            seed = seed,
            width = boardWidth,
            height = boardHeight,
            pieceCount = pieceCount,
            includeExactCoverCells = includeExactCoverCells,
            exactCoverCellRate = exactCoverCellRate,
            maxExactCoverNumber = maxExactCoverNumber,
            includeFixedPieces = includeFixedPieces,
            fixedPieceCount = fixedPieceCount,
            removedCellRate = removedCellRate,
            shapeBias = BoardShapeBias.Balanced
        };

        ClampSettings(settings);
        return settings;
    }

    private StageGenerationSettings CreateRandomProfileSettings(
        List<PieceData> validCandidates,
        System.Random random,
        GenerationProfile? previousProfile,
        string previousPieceSignature,
        int seed
    )
    {
        GenerationProfile profile = ChooseProfile(random, previousProfile);
        int difficulty = RandomRangeInclusive(random, minDifficulty, maxDifficulty);
        StageGenerationSettings settings = new StageGenerationSettings
        {
            mode = GenerationMode.RandomProfile,
            profile = profile,
            seed = seed,
            width = RandomRangeInclusive(random, minWidth, maxWidth),
            height = RandomRangeInclusive(random, minHeight, maxHeight),
            maxExactCoverNumber = 3,
            shapeBias = BoardShapeBias.Balanced
        };

        ApplyProfileDefaults(settings, profile, difficulty, random);
        ClampSettings(settings);
        settings.pieceSequence = BuildProfilePieceSequence(validCandidates, settings, random);

        if (settings.GetPieceSignature() == previousPieceSignature)
        {
            settings.pieceSequence = BuildProfilePieceSequence(validCandidates, settings, random);
        }

        return settings;
    }

    private GenerationProfile ChooseProfile(System.Random random, GenerationProfile? previousProfile)
    {
        List<GenerationProfile> candidates = new List<GenerationProfile>(allowedProfiles);

        if (candidates.Count > 1 && previousProfile.HasValue)
        {
            candidates.Remove(previousProfile.Value);
        }

        return candidates[random.Next(candidates.Count)];
    }

    private void ApplyProfileDefaults(
        StageGenerationSettings settings,
        GenerationProfile profile,
        int difficulty,
        System.Random random
    )
    {
        switch (profile)
        {
            case GenerationProfile.PawnIntro:
                settings.width = RandomRangeInclusive(random, minWidth, Mathf.Min(maxWidth, 6));
                settings.height = RandomRangeInclusive(random, minHeight, Mathf.Min(maxHeight, 6));
                settings.pieceCount = RandomRangeInclusive(random, 1, 2);
                settings.includeExactCoverCells = false;
                settings.fixedPieceCount = 0;
                settings.removedCellRate = 0f;
                settings.shapeBias = BoardShapeBias.Dense;
                break;
            case GenerationProfile.KnightPuzzle:
                settings.pieceCount = RandomProfilePieceCount(random, preferredMinPieceCount, Mathf.Max(preferredMinPieceCount, maxPieceCount));
                settings.includeExactCoverCells = allowExactCoverCells && random.NextDouble() < 0.25;
                settings.exactCoverCellRate = RandomFloat(random, 0.05f, 0.12f);
                settings.fixedPieceCount = 0;
                settings.removedCellRate = RandomFloat(random, 0.12f, 0.28f);
                settings.shapeBias = ChooseBias(random, BoardShapeBias.Sparse, BoardShapeBias.IslandLike, BoardShapeBias.Balanced);
                break;
            case GenerationProfile.LinePieces:
                settings.width = random.NextDouble() < 0.5
                    ? RandomRangeInclusive(random, Mathf.Max(minWidth, 8), maxWidth)
                    : RandomRangeInclusive(random, minWidth, Mathf.Min(maxWidth, 7));
                settings.height = settings.width >= 8
                    ? RandomRangeInclusive(random, minHeight, Mathf.Min(maxHeight, 6))
                    : RandomRangeInclusive(random, Mathf.Max(minHeight, 8), maxHeight);
                settings.pieceCount = RandomProfilePieceCount(random, minPieceCount, maxPieceCount);
                settings.includeExactCoverCells = allowExactCoverCells && random.NextDouble() < 0.2;
                settings.exactCoverCellRate = RandomFloat(random, 0.05f, 0.15f);
                settings.fixedPieceCount = 0;
                settings.removedCellRate = RandomFloat(random, 0.08f, 0.22f);
                settings.shapeBias = settings.width > settings.height ? BoardShapeBias.Wide : BoardShapeBias.Tall;
                break;
            case GenerationProfile.DiagonalPieces:
                settings.pieceCount = RandomProfilePieceCount(random, minPieceCount, maxPieceCount);
                settings.includeExactCoverCells = allowExactCoverCells && random.NextDouble() < 0.2;
                settings.exactCoverCellRate = RandomFloat(random, 0.05f, 0.15f);
                settings.fixedPieceCount = 0;
                settings.removedCellRate = RandomFloat(random, 0.08f, 0.24f);
                settings.shapeBias = BoardShapeBias.Diagonal;
                break;
            case GenerationProfile.MixedSmall:
                settings.width = RandomRangeInclusive(random, minWidth, Mathf.Min(maxWidth, 7));
                settings.height = RandomRangeInclusive(random, minHeight, Mathf.Min(maxHeight, 7));
                settings.pieceCount = RandomProfilePieceCount(random, minPieceCount, maxPieceCount);
                settings.includeExactCoverCells = allowExactCoverCells && random.NextDouble() < 0.25;
                settings.exactCoverCellRate = RandomFloat(random, 0.05f, 0.16f);
                settings.fixedPieceCount = 0;
                settings.removedCellRate = RandomFloat(random, 0f, 0.12f);
                settings.shapeBias = BoardShapeBias.Balanced;
                break;
            case GenerationProfile.QueenPower:
                settings.width = RandomRangeInclusive(random, Mathf.Max(minWidth, 7), maxWidth);
                settings.height = RandomRangeInclusive(random, Mathf.Max(minHeight, 7), maxHeight);
                settings.pieceCount = RandomProfilePieceCount(random, minPieceCount, maxPieceCount);
                settings.includeExactCoverCells = allowExactCoverCells && random.NextDouble() < 0.2;
                settings.exactCoverCellRate = RandomFloat(random, 0.03f, 0.1f);
                settings.fixedPieceCount = 0;
                settings.removedCellRate = RandomFloat(random, 0.05f, 0.18f);
                settings.shapeBias = BoardShapeBias.Dense;
                break;
            case GenerationProfile.SparseBoard:
                settings.pieceCount = RandomProfilePieceCount(random, minPieceCount, maxPieceCount);
                settings.includeExactCoverCells = allowExactCoverCells && random.NextDouble() < 0.2;
                settings.exactCoverCellRate = RandomFloat(random, 0.05f, 0.12f);
                settings.fixedPieceCount = 0;
                settings.removedCellRate = RandomFloat(random, 0.22f, 0.42f);
                settings.shapeBias = ChooseBias(random, BoardShapeBias.Sparse, BoardShapeBias.CrossLike, BoardShapeBias.IslandLike);
                break;
            case GenerationProfile.ManySmallPieces:
                settings.width = RandomRangeInclusive(random, minWidth, Mathf.Min(maxWidth, 8));
                settings.height = RandomRangeInclusive(random, minHeight, Mathf.Min(maxHeight, 8));
                settings.pieceCount = RandomProfilePieceCount(random, Mathf.Max(preferredMinPieceCount, 4), Mathf.Max(maxPieceCount, 6));
                settings.includeExactCoverCells = allowExactCoverCells && random.NextDouble() < 0.15;
                settings.exactCoverCellRate = RandomFloat(random, 0.04f, 0.1f);
                settings.fixedPieceCount = 0;
                settings.removedCellRate = RandomFloat(random, 0f, 0.08f);
                settings.shapeBias = BoardShapeBias.Dense;
                break;
            case GenerationProfile.ExactCoverIntro:
                settings.pieceCount = RandomProfilePieceCount(random, minPieceCount, maxPieceCount);
                settings.includeExactCoverCells = allowExactCoverCells;
                settings.exactCoverCellRate = RandomFloat(random, 0.1f, 0.25f);
                settings.maxExactCoverNumber = RandomRangeInclusive(random, 2, 3);
                settings.fixedPieceCount = 0;
                settings.removedCellRate = RandomFloat(random, 0f, 0.12f);
                settings.shapeBias = BoardShapeBias.Balanced;
                break;
            case GenerationProfile.FixedPieceIntro:
                settings.pieceCount = RandomProfilePieceCount(random, Mathf.Max(minPieceCount, 3), maxPieceCount);
                settings.includeExactCoverCells = allowExactCoverCells && random.NextDouble() < 0.2;
                settings.exactCoverCellRate = RandomFloat(random, 0.05f, 0.12f);
                settings.fixedPieceCount = allowFixedPieces ? 1 : 0;
                settings.removedCellRate = RandomFloat(random, 0f, 0.14f);
                settings.shapeBias = BoardShapeBias.Balanced;
                break;
            case GenerationProfile.AdvancedMixed:
                settings.width = RandomRangeInclusive(random, Mathf.Max(minWidth, 7), maxWidth);
                settings.height = RandomRangeInclusive(random, Mathf.Max(minHeight, 7), maxHeight);
                settings.pieceCount = RandomProfilePieceCount(random, Mathf.Max(preferredMinPieceCount, 4), Mathf.Max(maxPieceCount, 6));
                settings.includeExactCoverCells = allowExactCoverCells;
                settings.exactCoverCellRate = RandomFloat(random, 0.12f, 0.28f);
                settings.maxExactCoverNumber = RandomRangeInclusive(random, 2, 5);
                settings.fixedPieceCount = allowFixedPieces ? RandomRangeInclusive(random, 0, 2) : 0;
                settings.removedCellRate = RandomFloat(random, 0.08f, 0.28f);
                settings.shapeBias = ChooseBias(random, BoardShapeBias.Balanced, BoardShapeBias.Sparse, BoardShapeBias.CrossLike);
                break;
        }

        settings.includeFixedPieces = allowFixedPieces && settings.fixedPieceCount > 0;
    }

    private List<PieceData> BuildProfilePieceSequence(
        List<PieceData> validCandidates,
        StageGenerationSettings settings,
        System.Random random
    )
    {
        List<PieceType> types = new List<PieceType>();

        switch (settings.profile)
        {
            case GenerationProfile.PawnIntro:
                AddRepeated(types, PieceType.Pawn, settings.pieceCount);
                break;
            case GenerationProfile.KnightPuzzle:
                AddRepeated(types, PieceType.Knight, settings.pieceCount);
                break;
            case GenerationProfile.LinePieces:
                AddRepeated(types, PieceType.Rook, Mathf.Max(1, settings.pieceCount - (allowQueen && random.NextDouble() < 0.25 ? 1 : 0)));
                if (types.Count < settings.pieceCount)
                {
                    types.Add(PieceType.Queen);
                }
                break;
            case GenerationProfile.DiagonalPieces:
                AddRepeated(types, PieceType.Bishop, settings.pieceCount);
                break;
            case GenerationProfile.MixedSmall:
                AddRandomTypes(types, random, settings.pieceCount, PieceType.Pawn, PieceType.King, PieceType.Knight);
                break;
            case GenerationProfile.QueenPower:
                types.Add(allowQueen ? PieceType.Queen : PieceType.Rook);
                AddRandomTypes(types, random, settings.pieceCount - 1, PieceType.Pawn, PieceType.Knight);
                break;
            case GenerationProfile.SparseBoard:
                AddRandomTypes(types, random, settings.pieceCount, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.King);
                break;
            case GenerationProfile.ManySmallPieces:
                AddRandomTypes(types, random, settings.pieceCount, PieceType.Pawn, PieceType.King);
                break;
            case GenerationProfile.ExactCoverIntro:
                AddRandomTypes(types, random, settings.pieceCount, PieceType.Pawn, PieceType.King, PieceType.Knight, PieceType.Rook);
                break;
            case GenerationProfile.FixedPieceIntro:
                AddRandomTypes(types, random, settings.pieceCount, PieceType.Pawn, PieceType.King, PieceType.Knight, PieceType.Rook);
                break;
            case GenerationProfile.AdvancedMixed:
                AddRandomTypes(types, random, settings.pieceCount, PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, allowQueen ? PieceType.Queen : PieceType.King, PieceType.King);
                break;
        }

        List<PieceData> pieces = new List<PieceData>();

        foreach (PieceType type in types)
        {
            pieces.Add(GetPieceDataByType(validCandidates, type, random));
        }

        return pieces;
    }

    private StageData GenerateSingleStage(
        List<PieceData> validCandidates,
        StageGenerationSettings settings,
        int serialNumber
    )
    {
        System.Random random = new System.Random(settings.seed);
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
        List<GeneratedPiece> placements = GenerateAnswerPlacements(validCandidates, settings, random, occupiedPositions);
        HashSet<Vector2Int> removedCells = ChooseRemovedCells(settings, placements, random);
        Dictionary<Vector2Int, CellView> cellMap = CreateTemporaryCellMap(settings.width, settings.height, removedCells);

        try
        {
            MarkFixedPieces(placements, settings, random);
            Dictionary<Vector2Int, int> coverCounts = CalculateCoverCounts(placements, cellMap);

            if (HasRedundantPiece(placements, coverCounts, cellMap))
            {
                Debug.Log("[GEN DEBUG] retry generation");
                return null;
            }

            HashSet<Vector2Int> exactCoverPositions = ChooseExactCoverPositions(coverCounts, settings, random);

            StageData stageData = CreateInstance<StageData>();
            stageData.stageName = $"{stageNamePrefix}_{serialNumber:000}";
            stageData.stageCategory = stageCategory;
            stageData.boardText = BuildBoardText(coverCounts, exactCoverPositions, settings);
            stageData.pieceStocks = BuildPieceStocks(placements);
            stageData.fixedPieces = BuildFixedPieces(placements);
            stageData.helpPageToUnlock = helpPageToUnlock;
            stageData.helpPageToShowEveryTime = helpPageToShowEveryTime;
            stageData.authorNote = BuildAuthorNote(settings, placements, removedCells);

            return stageData;
        }
        finally
        {
            DestroyTemporaryCellMap(cellMap);
        }
    }

    private StageData GenerateGrowingStandardStage(
        List<PieceData> validCandidates,
        StageGenerationSettings settings,
        int serialNumber
    )
    {
        System.Random random = new System.Random(settings.seed);
        int boardSize = Mathf.Clamp(Mathf.Max(settings.width, settings.height), minWidth, maxWidth);
        settings.width = boardSize;
        settings.height = boardSize;
        settings.pieceCount = Mathf.Clamp(settings.pieceCount, minPieceCount, Mathf.Min(maxPieceCount, boardSize * boardSize));

        Debug.Log($"[GEN DEBUG] targetPieceCount={settings.pieceCount}");

        List<GeneratedPiece> placements = new List<GeneratedPiece>();
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
        HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();
        PieceData firstPiece = GetGrowingPiece(validCandidates, settings, random, 0);
        Vector2Int firstPosition = GetCenterBiasedPosition(boardSize, random);
        placements.Add(new GeneratedPiece(firstPiece, firstPosition));
        occupiedPositions.Add(firstPosition);

        foreach (Vector2Int position in GetCoveredPositionsForWorkingBoard(firstPiece.pieceType, firstPosition, boardSize))
        {
            activeCells.Add(position);
        }

        Debug.Log($"[GEN DEBUG] added piece type={firstPiece.pieceType}, overlapCount=0, newCellCount={activeCells.Count}");

        for (int i = 1; i < settings.pieceCount; i++)
        {
            PieceData pieceData = GetGrowingPiece(validCandidates, settings, random, i);
            List<GrowingPlacementCandidate> candidates = GetGrowingPlacementCandidates(
                pieceData,
                boardSize,
                occupiedPositions,
                activeCells,
                random
            );

            if (candidates.Count == 0)
            {
                Debug.Log($"[GEN DEBUG] rejected reason=no valid growing placement for {pieceData.pieceType}");
                return null;
            }

            GrowingPlacementCandidate selectedCandidate = ChooseGrowingCandidate(candidates, random);
            placements.Add(new GeneratedPiece(pieceData, selectedCandidate.position));
            occupiedPositions.Add(selectedCandidate.position);

            foreach (Vector2Int coveredPosition in selectedCandidate.coverage)
            {
                activeCells.Add(coveredPosition);
            }

            Debug.Log(
                $"[GEN DEBUG] added piece type={pieceData.pieceType}, overlapCount={selectedCandidate.overlapCount}, "
                + $"newCellCount={selectedCandidate.newCellCount}"
            );
        }

        HashSet<Vector2Int> removedCells = BuildRemovedCellsFromActiveCells(boardSize, activeCells);
        Dictionary<Vector2Int, CellView> cellMap = CreateTemporaryCellMap(boardSize, boardSize, removedCells);

        try
        {
            Dictionary<Vector2Int, int> coverCounts = CalculateCoverCounts(placements, cellMap);

            if (HasRedundantPiece(placements, coverCounts, cellMap))
            {
                Debug.Log("[GEN DEBUG] retry generation");
                return null;
            }

            StageData stageData = CreateInstance<StageData>();
            stageData.stageName = $"{stageNamePrefix}_{serialNumber:000}";
            stageData.stageCategory = StageCategory.Standard;
            stageData.boardText = BuildBoardText(coverCounts, new HashSet<Vector2Int>(), settings);
            stageData.pieceStocks = BuildPieceStocks(placements);
            stageData.fixedPieces = new List<FixedPieceData>();
            stageData.helpPageToUnlock = helpPageToUnlock;
            stageData.helpPageToShowEveryTime = helpPageToShowEveryTime;
            stageData.authorNote = BuildAuthorNote(settings, placements, removedCells);

            return stageData;
        }
        finally
        {
            DestroyTemporaryCellMap(cellMap);
        }
    }

    private StageData GenerateExactCoverStage(
        List<PieceData> validCandidates,
        StageGenerationSettings settings,
        int serialNumber
    )
    {
        System.Random random = new System.Random(settings.seed);
        int boardSize = Mathf.Clamp(Mathf.Max(settings.width, settings.height), minWidth, maxWidth);
        settings.width = boardSize;
        settings.height = boardSize;
        int exactMinPieceCount = settings.isAdvancedExactFixed ? Mathf.Max(2, minTotalSolutionPieceCount) : minPieceCount;
        int exactMaxPieceCount = settings.isAdvancedExactFixed ? Mathf.Max(exactMinPieceCount, maxTotalSolutionPieceCount) : maxPieceCount;
        settings.pieceCount = Mathf.Clamp(settings.pieceCount, exactMinPieceCount, Mathf.Min(exactMaxPieceCount, boardSize * boardSize));

        Debug.Log($"[{(settings.isAdvancedExactFixed ? "ADV EXACT FIXED GEN" : "EXACT GEN")}] targetPieceCount={settings.pieceCount}, exactCellRate={settings.exactCoverCellRate}, maxExactNumber={settings.maxExactCoverNumber}");

        List<GeneratedPiece> placements = GenerateGrowingPlacements(validCandidates, settings, random, boardSize);

        if (placements == null || placements.Count < exactMinPieceCount)
        {
            Debug.Log("[EXACT GEN] rejected reason=failed to grow enough pieces");
            return null;
        }

        MarkFixedPieces(placements, settings, random);

        HashSet<Vector2Int> activeCells = BuildActiveCellsFromPlacements(placements, boardSize);
        HashSet<Vector2Int> removedCells = BuildRemovedCellsFromActiveCells(boardSize, activeCells);
        Dictionary<Vector2Int, CellView> cellMap = CreateTemporaryCellMap(boardSize, boardSize, removedCells);

        try
        {
            Dictionary<Vector2Int, int> coverCounts = CalculateCoverCounts(placements, cellMap);

            if (HasRedundantPiece(placements, coverCounts, cellMap))
            {
                Debug.Log("[EXACT GEN] rejected reason=redundant piece");
                return null;
            }

            HashSet<Vector2Int> exactCoverPositions = ChooseExactCoverPositionsForExactGeneration(
                coverCounts,
                placements,
                settings,
                random
            );

            int requiredExactCellCount = settings.isAdvancedExactFixed ? minAdvancedExactCellCount : minExactCellCount;

            if (exactCoverPositions.Count < requiredExactCellCount)
            {
                Debug.Log($"[EXACT GEN] rejected reason=exactCellCount {exactCoverPositions.Count} < {requiredExactCellCount}");
                return null;
            }

            StageData stageData = CreateInstance<StageData>();
            stageData.stageName = $"{stageNamePrefix}_{serialNumber:000}";
            stageData.stageCategory = settings.isAdvancedExactFixed ? StageCategory.Advanced : StageCategory.ExactCover;
            stageData.boardText = BuildBoardText(coverCounts, exactCoverPositions, settings);
            stageData.pieceStocks = BuildPieceStocks(placements);
            stageData.fixedPieces = BuildFixedPieces(placements);
            stageData.helpPageToUnlock = helpPageToUnlock;
            stageData.helpPageToShowEveryTime = helpPageToShowEveryTime;
            stageData.authorNote = BuildAuthorNote(settings, placements, removedCells);

            return stageData;
        }
        finally
        {
            DestroyTemporaryCellMap(cellMap);
        }
    }

    private List<GeneratedPiece> GenerateGrowingPlacements(
        List<PieceData> validCandidates,
        StageGenerationSettings settings,
        System.Random random,
        int boardSize
    )
    {
        List<GeneratedPiece> placements = new List<GeneratedPiece>();
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
        HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();
        PieceData firstPiece = GetGrowingPiece(validCandidates, settings, random, 0);
        Vector2Int firstPosition = GetCenterBiasedPosition(boardSize, random);
        placements.Add(new GeneratedPiece(firstPiece, firstPosition));
        occupiedPositions.Add(firstPosition);

        foreach (Vector2Int position in GetCoveredPositionsForWorkingBoard(firstPiece.pieceType, firstPosition, boardSize))
        {
            activeCells.Add(position);
        }

        Debug.Log($"[GEN DEBUG] added piece type={firstPiece.pieceType}, overlapCount=0, newCellCount={activeCells.Count}");

        for (int i = 1; i < settings.pieceCount; i++)
        {
            PieceData pieceData = GetGrowingPiece(validCandidates, settings, random, i);
            List<GrowingPlacementCandidate> candidates = GetGrowingPlacementCandidates(
                pieceData,
                boardSize,
                occupiedPositions,
                activeCells,
                random
            );

            if (candidates.Count == 0)
            {
                Debug.Log($"[GEN DEBUG] rejected reason=no valid growing placement for {pieceData.pieceType}");
                return null;
            }

            GrowingPlacementCandidate selectedCandidate = ChooseGrowingCandidate(candidates, random);
            placements.Add(new GeneratedPiece(pieceData, selectedCandidate.position));
            occupiedPositions.Add(selectedCandidate.position);

            foreach (Vector2Int coveredPosition in selectedCandidate.coverage)
            {
                activeCells.Add(coveredPosition);
            }

            Debug.Log(
                $"[GEN DEBUG] added piece type={pieceData.pieceType}, overlapCount={selectedCandidate.overlapCount}, "
                + $"newCellCount={selectedCandidate.newCellCount}"
            );
        }

        return placements;
    }

    private HashSet<Vector2Int> BuildActiveCellsFromPlacements(List<GeneratedPiece> placements, int boardSize)
    {
        HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();

        foreach (GeneratedPiece placement in placements)
        {
            foreach (Vector2Int position in GetCoveredPositionsForWorkingBoard(placement.PieceData.pieceType, placement.Position, boardSize))
            {
                activeCells.Add(position);
            }
        }

        return activeCells;
    }

    private HashSet<Vector2Int> ChooseExactCoverPositionsForExactGeneration(
        Dictionary<Vector2Int, int> coverCounts,
        List<GeneratedPiece> placements,
        StageGenerationSettings settings,
        System.Random random
    )
    {
        HashSet<Vector2Int> piecePositions = new HashSet<Vector2Int>();

        foreach (GeneratedPiece placement in placements)
        {
            piecePositions.Add(placement.Position);
        }

        List<ExactCellCandidate> candidates = new List<ExactCellCandidate>();
        int maxNumber = Mathf.Clamp(settings.maxExactCoverNumber, 1, 5);

        foreach (KeyValuePair<Vector2Int, int> coverCount in coverCounts)
        {
            if (coverCount.Value <= 0 || coverCount.Value > maxNumber || piecePositions.Contains(coverCount.Key))
            {
                continue;
            }

            int score = 0;
            score += coverCount.Value >= 2 ? 20 : 5;
            score += preferHigherExactNumbers ? coverCount.Value * 4 : 0;
            score += CountOrthogonalActiveNeighbors(coverCount.Key, coverCounts) <= 2 ? 5 : 0;
            score += random.Next(0, 5);

            candidates.Add(new ExactCellCandidate
            {
                position = coverCount.Key,
                coverCount = coverCount.Value,
                score = score
            });
        }

        candidates.Sort((left, right) => right.score.CompareTo(left.score));

        int activeCount = CountPositiveCoverCells(coverCounts);
        int requiredExactCellCount = settings.isAdvancedExactFixed ? minAdvancedExactCellCount : minExactCellCount;
        int targetCount = Mathf.Max(requiredExactCellCount, Mathf.RoundToInt(activeCount * Mathf.Clamp01(settings.exactCoverCellRate)));
        targetCount = Mathf.Min(targetCount, candidates.Count);
        HashSet<Vector2Int> exactPositions = new HashSet<Vector2Int>();

        for (int i = 0; i < targetCount; i++)
        {
            exactPositions.Add(candidates[i].position);
        }

        return exactPositions;
    }

    private int CountOrthogonalActiveNeighbors(Vector2Int position, Dictionary<Vector2Int, int> coverCounts)
    {
        int count = 0;

        foreach (Vector2Int direction in OrthogonalDirections)
        {
            if (coverCounts.TryGetValue(position + direction, out int coverCount) && coverCount > 0)
            {
                count++;
            }
        }

        return count;
    }

    private int CountPositiveCoverCells(Dictionary<Vector2Int, int> coverCounts)
    {
        int count = 0;

        foreach (KeyValuePair<Vector2Int, int> coverCount in coverCounts)
        {
            if (coverCount.Value > 0)
            {
                count++;
            }
        }

        return count;
    }

    private PieceData GetGrowingPiece(List<PieceData> validCandidates, StageGenerationSettings settings, System.Random random, int index)
    {
        if (settings.pieceSequence != null && index < settings.pieceSequence.Count)
        {
            return settings.pieceSequence[index];
        }

        return validCandidates[random.Next(validCandidates.Count)];
    }

    private Vector2Int GetCenterBiasedPosition(int boardSize, System.Random random)
    {
        int center = boardSize / 2;
        int radius = Mathf.Max(1, boardSize / 5);
        return new Vector2Int(
            Mathf.Clamp(center + random.Next(-radius, radius + 1), 0, boardSize - 1),
            Mathf.Clamp(center + random.Next(-radius, radius + 1), 0, boardSize - 1)
        );
    }

    private List<GrowingPlacementCandidate> GetGrowingPlacementCandidates(
        PieceData pieceData,
        int boardSize,
        HashSet<Vector2Int> occupiedPositions,
        HashSet<Vector2Int> activeCells,
        System.Random random
    )
    {
        List<GrowingPlacementCandidate> candidates = new List<GrowingPlacementCandidate>();

        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (occupiedPositions.Contains(position))
                {
                    continue;
                }

                List<Vector2Int> coverage = GetCoveredPositionsForWorkingBoard(pieceData.pieceType, position, boardSize);
                int overlapCount = 0;
                int newCellCount = 0;

                foreach (Vector2Int coveredPosition in coverage)
                {
                    if (activeCells.Contains(coveredPosition))
                    {
                        overlapCount++;
                    }
                    else
                    {
                        newCellCount++;
                    }
                }

                int minDistance = GetMinManhattanDistanceToActiveCells(position, coverage, activeCells);
                bool allowZeroOverlap = random.NextDouble() < 0.12;

                if (newCellCount < 1)
                {
                    continue;
                }

                if (overlapCount == 0 && !allowZeroOverlap)
                {
                    continue;
                }

                if (overlapCount > 3)
                {
                    continue;
                }

                if (minDistance > 3)
                {
                    continue;
                }

                candidates.Add(new GrowingPlacementCandidate
                {
                    position = position,
                    coverage = coverage,
                    overlapCount = overlapCount,
                    newCellCount = newCellCount,
                    distanceToExistingBoard = minDistance
                });
            }
        }

        return candidates;
    }

    private GrowingPlacementCandidate ChooseGrowingCandidate(List<GrowingPlacementCandidate> candidates, System.Random random)
    {
        candidates.Sort((left, right) =>
        {
            int leftScore = ScoreGrowingCandidate(left);
            int rightScore = ScoreGrowingCandidate(right);
            return rightScore.CompareTo(leftScore);
        });

        int topCount = Mathf.Min(candidates.Count, Mathf.Max(3, candidates.Count / 3));
        return candidates[random.Next(topCount)];
    }

    private int ScoreGrowingCandidate(GrowingPlacementCandidate candidate)
    {
        int score = 0;
        score += candidate.overlapCount >= 1 && candidate.overlapCount <= 3 ? 12 : 0;
        score += Mathf.Min(candidate.newCellCount, 4) * 4;
        score -= candidate.distanceToExistingBoard * 2;
        return score;
    }

    private int GetMinManhattanDistanceToActiveCells(
        Vector2Int candidatePosition,
        List<Vector2Int> coverage,
        HashSet<Vector2Int> activeCells
    )
    {
        int minDistance = int.MaxValue;

        foreach (Vector2Int activeCell in activeCells)
        {
            minDistance = Mathf.Min(minDistance, ManhattanDistance(candidatePosition, activeCell));

            foreach (Vector2Int coveredPosition in coverage)
            {
                minDistance = Mathf.Min(minDistance, ManhattanDistance(coveredPosition, activeCell));
            }
        }

        return minDistance == int.MaxValue ? 999 : minDistance;
    }

    private int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private HashSet<Vector2Int> BuildRemovedCellsFromActiveCells(int boardSize, HashSet<Vector2Int> activeCells)
    {
        HashSet<Vector2Int> removedCells = new HashSet<Vector2Int>();

        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (!activeCells.Contains(position))
                {
                    removedCells.Add(position);
                }
            }
        }

        return removedCells;
    }

    private List<Vector2Int> GetCoveredPositionsForWorkingBoard(PieceType pieceType, Vector2Int origin, int boardSize)
    {
        List<Vector2Int> results = new List<Vector2Int>();
        TryAddWorkingPosition(origin, boardSize, results);

        switch (pieceType)
        {
            case PieceType.Pawn:
                TryAddWorkingPosition(origin + Vector2Int.up, boardSize, results);
                break;
            case PieceType.Knight:
                AddWorkingStepMoves(origin, KnightDirections, boardSize, results);
                break;
            case PieceType.Bishop:
                AddWorkingLineMoves(origin, DiagonalDirections, boardSize, results);
                break;
            case PieceType.Rook:
                AddWorkingLineMoves(origin, OrthogonalDirections, boardSize, results);
                break;
            case PieceType.Queen:
                AddWorkingLineMoves(origin, OrthogonalDirections, boardSize, results);
                AddWorkingLineMoves(origin, DiagonalDirections, boardSize, results);
                break;
            case PieceType.King:
                AddWorkingStepMoves(origin, OrthogonalDirections, boardSize, results);
                AddWorkingStepMoves(origin, DiagonalDirections, boardSize, results);
                break;
        }

        return results;
    }

    private void AddWorkingStepMoves(Vector2Int origin, Vector2Int[] directions, int boardSize, List<Vector2Int> results)
    {
        foreach (Vector2Int direction in directions)
        {
            TryAddWorkingPosition(origin + direction, boardSize, results);
        }
    }

    private void AddWorkingLineMoves(Vector2Int origin, Vector2Int[] directions, int boardSize, List<Vector2Int> results)
    {
        foreach (Vector2Int direction in directions)
        {
            Vector2Int position = origin + direction;

            while (IsInsideWorkingBoard(position, boardSize))
            {
                AddUniqueWorkingPosition(position, results);
                position += direction;
            }
        }
    }

    private void TryAddWorkingPosition(Vector2Int position, int boardSize, List<Vector2Int> results)
    {
        if (IsInsideWorkingBoard(position, boardSize))
        {
            AddUniqueWorkingPosition(position, results);
        }
    }

    private bool IsInsideWorkingBoard(Vector2Int position, int boardSize)
    {
        return position.x >= 0 && position.x < boardSize && position.y >= 0 && position.y < boardSize;
    }

    private void AddUniqueWorkingPosition(Vector2Int position, List<Vector2Int> results)
    {
        if (!results.Contains(position))
        {
            results.Add(position);
        }
    }

    private Dictionary<Vector2Int, CellView> CreateTemporaryCellMap(int width, int height, HashSet<Vector2Int> removedCells)
    {
        Dictionary<Vector2Int, CellView> cellMap = new Dictionary<Vector2Int, CellView>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                GameObject cellObject = new GameObject($"TempCell_{x}_{y}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                CellView cellView = cellObject.AddComponent<CellView>();
                cellView.GridPosition = position;
                cellView.IsActive = !removedCells.Contains(position);
                cellMap.Add(position, cellView);
            }
        }

        return cellMap;
    }

    private List<GeneratedPiece> GenerateAnswerPlacements(
        List<PieceData> validCandidates,
        StageGenerationSettings settings,
        System.Random random,
        HashSet<Vector2Int> occupiedPositions
    )
    {
        List<GeneratedPiece> placements = new List<GeneratedPiece>();
        int count = Mathf.Min(settings.pieceCount, settings.width * settings.height);

        for (int i = 0; i < count; i++)
        {
            Vector2Int position = GetUnusedPosition(random, occupiedPositions, settings.width, settings.height);
            PieceData pieceData = settings.pieceSequence != null && i < settings.pieceSequence.Count
                ? settings.pieceSequence[i]
                : validCandidates[random.Next(validCandidates.Count)];

            occupiedPositions.Add(position);
            placements.Add(new GeneratedPiece(pieceData, position));
        }

        return placements;
    }

    private HashSet<Vector2Int> ChooseRemovedCells(
        StageGenerationSettings settings,
        List<GeneratedPiece> placements,
        System.Random random
    )
    {
        HashSet<Vector2Int> removedCells = new HashSet<Vector2Int>();

        if (settings.removedCellRate <= 0f)
        {
            return removedCells;
        }

        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

        foreach (GeneratedPiece placement in placements)
        {
            occupied.Add(placement.Position);
        }

        int cellCount = settings.width * settings.height;
        int targetRemovedCount = Mathf.RoundToInt(cellCount * Mathf.Clamp01(settings.removedCellRate));
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int y = 0; y < settings.height; y++)
        {
            for (int x = 0; x < settings.width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (!occupied.Contains(position) && ShouldConsiderRemovedCell(position, settings, random))
                {
                    candidates.Add(position);
                }
            }
        }

        Shuffle(candidates, random);

        for (int i = 0; i < Mathf.Min(targetRemovedCount, candidates.Count); i++)
        {
            removedCells.Add(candidates[i]);
        }

        return removedCells;
    }

    private bool ShouldConsiderRemovedCell(Vector2Int position, StageGenerationSettings settings, System.Random random)
    {
        switch (settings.shapeBias)
        {
            case BoardShapeBias.Dense:
                return random.NextDouble() < 0.45;
            case BoardShapeBias.Sparse:
                return true;
            case BoardShapeBias.Wide:
                return position.y == 0 || position.y == settings.height - 1 || random.NextDouble() < 0.35;
            case BoardShapeBias.Tall:
                return position.x == 0 || position.x == settings.width - 1 || random.NextDouble() < 0.35;
            case BoardShapeBias.Diagonal:
                return Mathf.Abs(position.x - position.y) > Mathf.Max(settings.width, settings.height) / 2 || random.NextDouble() < 0.35;
            case BoardShapeBias.CrossLike:
                return position.x != settings.width / 2 && position.y != settings.height / 2;
            case BoardShapeBias.IslandLike:
                return random.NextDouble() < 0.75;
            default:
                return random.NextDouble() < 0.65;
        }
    }

    private void MarkFixedPieces(List<GeneratedPiece> placements, StageGenerationSettings settings, System.Random random)
    {
        if (!settings.includeFixedPieces || settings.fixedPieceCount <= 0 || placements.Count == 0)
        {
            return;
        }

        List<int> indices = new List<int>();

        for (int i = 0; i < placements.Count; i++)
        {
            indices.Add(i);
        }

        Shuffle(indices, random);

        int count = Mathf.Min(Mathf.Max(0, settings.fixedPieceCount), placements.Count);

        for (int i = 0; i < count; i++)
        {
            placements[indices[i]].IsFixed = true;
        }
    }

    private Vector2Int GetUnusedPosition(System.Random random, HashSet<Vector2Int> occupiedPositions, int width, int height)
    {
        int maxAttempts = width * height * 2;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2Int position = new Vector2Int(random.Next(width), random.Next(height));

            if (!occupiedPositions.Contains(position))
            {
                return position;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
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

    private bool HasRedundantPiece(
        List<GeneratedPiece> placements,
        Dictionary<Vector2Int, int> coverCounts,
        Dictionary<Vector2Int, CellView> cellMap
    )
    {
        foreach (GeneratedPiece placement in placements)
        {
            List<Vector2Int> coveredPositions = CoverageCalculator.GetCoveredPositions(
                placement.PieceData.pieceType,
                placement.Position,
                cellMap
            );
            bool hasUniqueCoveredCell = false;

            foreach (Vector2Int position in coveredPositions)
            {
                if (coverCounts.TryGetValue(position, out int coverCount) && coverCount == 1)
                {
                    hasUniqueCoveredCell = true;
                    break;
                }
            }

            if (hasUniqueCoveredCell)
            {
                continue;
            }

            string pieceName = !string.IsNullOrWhiteSpace(placement.PieceData.displayName)
                ? placement.PieceData.displayName
                : placement.PieceData.pieceType.ToString();
            Debug.Log($"[GEN DEBUG] redundant piece detected: {pieceName} at {placement.Position}");
            return true;
        }

        return false;
    }

    private string BuildBoardText(
        Dictionary<Vector2Int, int> coverCounts,
        HashSet<Vector2Int> exactCoverPositions,
        StageGenerationSettings settings
    )
    {
        StringBuilder builder = new StringBuilder();

        for (int y = settings.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < settings.width; x++)
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

    private HashSet<Vector2Int> ChooseExactCoverPositions(
        Dictionary<Vector2Int, int> coverCounts,
        StageGenerationSettings settings,
        System.Random random
    )
    {
        HashSet<Vector2Int> exactCoverPositions = new HashSet<Vector2Int>();

        if (!settings.includeExactCoverCells || settings.exactCoverCellRate <= 0f)
        {
            return exactCoverPositions;
        }

        List<Vector2Int> activePositions = new List<Vector2Int>();
        List<Vector2Int> exactCoverCandidates = new List<Vector2Int>();
        int maxCoverNumber = Mathf.Clamp(settings.maxExactCoverNumber, 1, 5);

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

        int targetExactCoverCount = Mathf.RoundToInt(activePositions.Count * Mathf.Clamp01(settings.exactCoverCellRate));
        targetExactCoverCount = Mathf.Min(targetExactCoverCount, exactCoverCandidates.Count);

        Shuffle(exactCoverCandidates, random);

        for (int i = 0; i < targetExactCoverCount; i++)
        {
            exactCoverPositions.Add(exactCoverCandidates[i]);
        }

        return exactCoverPositions;
    }

    private bool IsValidGeneratedStage(StageData stageData, StageGenerationSettings settings)
    {
        if (stageData == null || string.IsNullOrWhiteSpace(stageData.boardText))
        {
            return false;
        }

        int activeCount = CountActiveCells(stageData.boardText);
        int minActiveCount = Mathf.Max(3, settings.pieceCount + 1);
        int maxActiveCount = Mathf.Max(minActiveCount, Mathf.RoundToInt(settings.width * settings.height * 0.95f));

        if (activeCount < minActiveCount || activeCount > maxActiveCount)
        {
            return false;
        }

        if (stageData.pieceStocks.Count == 0 && stageData.fixedPieces.Count == 0)
        {
            return false;
        }

        foreach (FixedPieceData fixedPiece in stageData.fixedPieces)
        {
            if (fixedPiece == null || fixedPiece.pieceData == null || GetBoardChar(stageData.boardText, fixedPiece.position) == 'X')
            {
                return false;
            }
        }

        foreach (char character in stageData.boardText)
        {
            if (character >= '6' && character <= '9')
            {
                return false;
            }
        }

        return true;
    }

    private bool IsTooSimilarToPrevious(
        StageData stageData,
        StageGenerationSettings settings,
        GenerationProfile? previousProfile,
        string previousPieceSignature,
        string previousBoardText
    )
    {
        if (generationMode == GenerationMode.ManualPieceSet)
        {
            return false;
        }

        if (previousProfile.HasValue && previousProfile.Value == settings.profile)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(previousPieceSignature) && previousPieceSignature == settings.GetPieceSignature())
        {
            return true;
        }

        return !string.IsNullOrEmpty(previousBoardText) && previousBoardText == stageData.boardText;
    }

    private int CountActiveCells(string boardText)
    {
        int count = 0;

        foreach (char character in boardText)
        {
            if (character == 'Z' || character == 'z' || character == '#' || (character >= '1' && character <= '5'))
            {
                count++;
            }
        }

        return count;
    }

    private char GetBoardChar(string boardText, Vector2Int position)
    {
        string[] rows = boardText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        int rowIndex = rows.Length - 1 - position.y;

        if (rowIndex < 0 || rowIndex >= rows.Length || position.x < 0 || position.x >= rows[rowIndex].Length)
        {
            return 'X';
        }

        return rows[rowIndex][position.x];
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

    private string BuildAuthorNote(StageGenerationSettings settings, List<GeneratedPiece> placements, HashSet<Vector2Int> removedCells)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Auto generated stage.");
        builder.AppendLine($"Mode: {settings.mode}");
        builder.AppendLine($"Profile: {settings.profile}");
        builder.AppendLine($"Shape Bias: {settings.shapeBias}");
        builder.AppendLine($"Seed: {settings.seed}");
        builder.AppendLine($"Removed Cells: {removedCells.Count}");
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

    private PieceData GetPieceDataByType(List<PieceData> validCandidates, PieceType pieceType, System.Random random)
    {
        List<PieceData> matchingPieces = validCandidates.FindAll(pieceData => pieceData != null && pieceData.pieceType == pieceType);

        if (matchingPieces.Count > 0)
        {
            return matchingPieces[random.Next(matchingPieces.Count)];
        }

        return validCandidates[random.Next(validCandidates.Count)];
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

    private void ClampSettings(StageGenerationSettings settings)
    {
        minPieceCount = Mathf.Max(1, minPieceCount);
        preferredMinPieceCount = Mathf.Max(minPieceCount, preferredMinPieceCount);
        maxPieceCount = Mathf.Max(preferredMinPieceCount, maxPieceCount);
        settings.width = Mathf.Clamp(settings.width, minWidth, maxWidth);
        settings.height = Mathf.Clamp(settings.height, minHeight, maxHeight);
        int effectiveMinPieceCount = AllowsLowPieceCount(settings.profile) ? 1 : minPieceCount;
        int effectiveMaxPieceCount = Mathf.Min(maxPieceCount, settings.width * settings.height);
        settings.pieceCount = Mathf.Clamp(settings.pieceCount, effectiveMinPieceCount, Mathf.Max(effectiveMinPieceCount, effectiveMaxPieceCount));
        settings.exactCoverCellRate = Mathf.Clamp01(settings.exactCoverCellRate);
        settings.maxExactCoverNumber = Mathf.Clamp(settings.maxExactCoverNumber, 1, 5);
        settings.fixedPieceCount = settings.includeFixedPieces ? Mathf.Clamp(settings.fixedPieceCount, 0, settings.pieceCount) : 0;
        settings.removedCellRate = Mathf.Clamp(settings.removedCellRate, 0f, 0.75f);

        if (stageCategory == StageCategory.Standard)
        {
            settings.includeExactCoverCells = false;
            settings.exactCoverCellRate = 0f;
            settings.includeFixedPieces = false;
            settings.fixedPieceCount = 0;
        }
    }

    private int RandomRangeInclusive(System.Random random, int min, int max)
    {
        int clampedMax = Mathf.Max(min, max);
        return random.Next(min, clampedMax + 1);
    }

    private int RandomProfilePieceCount(System.Random random, int min, int max)
    {
        int effectiveMin = Mathf.Max(minPieceCount, min);
        int effectivePreferredMin = Mathf.Max(effectiveMin, preferredMinPieceCount);
        int effectiveMax = Mathf.Max(effectivePreferredMin, max);

        if (random.NextDouble() < 0.7)
        {
            return RandomRangeInclusive(random, effectivePreferredMin, effectiveMax);
        }

        return RandomRangeInclusive(random, effectiveMin, effectiveMax);
    }

    private bool AllowsLowPieceCount(GenerationProfile profile)
    {
        return profile == GenerationProfile.PawnIntro;
    }

    private float RandomFloat(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private BoardShapeBias ChooseBias(System.Random random, params BoardShapeBias[] biases)
    {
        return biases[random.Next(biases.Length)];
    }

    private void AddRepeated(List<PieceType> types, PieceType type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            types.Add(type);
        }
    }

    private void AddRandomTypes(List<PieceType> types, System.Random random, int count, params PieceType[] candidates)
    {
        for (int i = 0; i < count; i++)
        {
            types.Add(candidates[random.Next(candidates.Length)]);
        }
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

    private class SolverResult
    {
        public bool isSolvable;
        public int solutionCount;
        public int minPiecesUsed = int.MaxValue;
        public bool requiresAllPieces;
        public List<Solution> sampleSolutions = new List<Solution>();
    }

    private class Solution
    {
        public List<SolverPlacement> placements = new List<SolverPlacement>();
    }

    private class SolverPlacement
    {
        public PieceData pieceData;
        public Vector2Int position;
    }

    private class StageEvaluation
    {
        public bool accepted;
        public int score;
        public int constrainedCellCount;
        public int minCoverCandidateCount;
        public int shapeHintScore;
        public int totalNormalPieceCount;
        public int fixedPieceCount;
        public int exactCellCount;
        public int noExactSolutionCount;
        public float exactCellRate;
        public string exactNumberVariety;
        public int solutionScore;
        public int allPiecesScore;
        public int exactCellCountScore;
        public int exactNumberVarietyScore;
        public int solutionReductionScore;
        public int pieceRoleScore;
        public int boardShapeScore;
        public int connectedComponentCount;
        public int smallIslandCount;
        public int smallIslandPenalty;
        public int isolatedPiecePenalty;
        public int pieceSpreadPenalty;
        public int tinyIslandWithPiecePenalty;
        public int exactOnTinyIslandPenalty;
        public int unnaturalLongRangePenalty;
        public int fixedPieceConstraintScore;
        public string rejectReason;
    }

    private class EvaluatedStageCandidate
    {
        public StageData stageData;
        public StageGenerationSettings settings;
        public SolverResult solverResult;
        public StageEvaluation evaluation;
        public int score;
    }

    private static class StageEvaluator
    {
        public static StageEvaluation Evaluate(
            StageData stageData,
            SolverResult solverResult,
            bool requireAllPieces,
            int maxAcceptedSolutions,
            int minPieceCount
        )
        {
            StageEvaluation evaluation = new StageEvaluation();
            evaluation.totalNormalPieceCount = CountNormalPieces(stageData);

            if (stageData == null || string.IsNullOrWhiteSpace(stageData.boardText))
            {
                evaluation.rejectReason = "empty stage";
                return evaluation;
            }

            if (evaluation.totalNormalPieceCount < minPieceCount)
            {
                evaluation.rejectReason = $"piece count too low {evaluation.totalNormalPieceCount} < {minPieceCount}";
                return evaluation;
            }

            if (!solverResult.isSolvable || solverResult.solutionCount <= 0)
            {
                evaluation.rejectReason = "not solvable";
                return evaluation;
            }

            if (solverResult.minPiecesUsed < minPieceCount)
            {
                evaluation.rejectReason = $"minPiecesUsed too low {solverResult.minPiecesUsed} < {minPieceCount}";
                return evaluation;
            }

            if (requireAllPieces && !solverResult.requiresAllPieces)
            {
                evaluation.rejectReason = "does not require all pieces";
                return evaluation;
            }

            int activeCount = CountActiveCellsStatic(stageData.boardText);

            if (activeCount < 4 || HasNonStandardCells(stageData.boardText) || HasFixedPieces(stageData))
            {
                evaluation.rejectReason = "not a valid Standard stage";
                return evaluation;
            }

            ConstraintMetrics constraintMetrics = CalculateConstraintMetrics(stageData);
            int shapeHintScore = CalculateShapeHintScore(stageData);

            evaluation.constrainedCellCount = constraintMetrics.constrainedCellCount;
            evaluation.minCoverCandidateCount = constraintMetrics.minCoverCandidateCount;
            evaluation.shapeHintScore = shapeHintScore;

            if (solverResult.solutionCount > maxAcceptedSolutions
                || constraintMetrics.constrainedCellCount == 0
                || shapeHintScore <= 0)
            {
                evaluation.rejectReason = $"weak inference signals solutions={solverResult.solutionCount}, constrained={constraintMetrics.constrainedCellCount}, shapeHint={shapeHintScore}";
                return evaluation;
            }

            int solutionScore = solverResult.solutionCount <= 2
                ? 4
                : Mathf.Max(0, 22 - solverResult.solutionCount);
            int score = 50;
            score += solverResult.requiresAllPieces ? 35 : 8;
            score += solutionScore;
            score += Mathf.Min(30, constraintMetrics.constrainedCellCount * 4);
            score += Mathf.Max(0, 8 - constraintMetrics.minCoverCandidateCount) * 2;
            score += shapeHintScore;
            score += CountDistinctPieceTypes(stageData) * 5;
            score += Mathf.Min(20, CountInternalHoles(stageData.boardText) * 3 + CountEdgeCuts(stageData.boardText));

            if (constraintMetrics.averageCoverCandidateCount > 10f)
            {
                score -= Mathf.RoundToInt((constraintMetrics.averageCoverCandidateCount - 10f) * 2f);
            }

            evaluation.accepted = true;
            evaluation.score = score;
            return evaluation;
        }

        public static StageEvaluation EvaluateExact(
            StageData stageData,
            SolverResult solverResult,
            SolverResult noExactSolverResult,
            bool requireAllPieces,
            int maxAcceptedSolutions,
            int minPieceCount,
            int minExactCellCount,
            int maxExactNumber,
            bool requireFixedPieces = false,
            int minFixedPieceCount = 0,
            int minTotalSolutionPieceCount = 0
        )
        {
            StageEvaluation evaluation = new StageEvaluation();
            evaluation.totalNormalPieceCount = CountNormalPieces(stageData);
            evaluation.fixedPieceCount = CountFixedPieces(stageData);
            evaluation.exactCellCount = CountExactCells(stageData != null ? stageData.boardText : string.Empty);
            evaluation.noExactSolutionCount = noExactSolverResult != null ? noExactSolverResult.solutionCount : 0;
            int validCellCount = CountActiveCellsStatic(stageData != null ? stageData.boardText : string.Empty);
            evaluation.exactCellRate = validCellCount > 0 ? evaluation.exactCellCount / (float)validCellCount : 0f;
            evaluation.exactNumberVariety = BuildExactNumberVarietyText(stageData != null ? stageData.boardText : string.Empty);

            if (stageData == null || string.IsNullOrWhiteSpace(stageData.boardText))
            {
                evaluation.rejectReason = "empty stage";
                return evaluation;
            }

            if (HasExactNumberOutsideRange(stageData.boardText, maxExactNumber))
            {
                evaluation.rejectReason = "exact number outside allowed range";
                return evaluation;
            }

            if (evaluation.totalNormalPieceCount < minPieceCount)
            {
                evaluation.rejectReason = $"piece count too low {evaluation.totalNormalPieceCount} < {minPieceCount}";
                return evaluation;
            }

            if (requireFixedPieces && evaluation.fixedPieceCount < minFixedPieceCount)
            {
                evaluation.rejectReason = $"fixedPieceCount {evaluation.fixedPieceCount} < {minFixedPieceCount}";
                return evaluation;
            }

            if (requireFixedPieces && evaluation.totalNormalPieceCount + evaluation.fixedPieceCount < minTotalSolutionPieceCount)
            {
                evaluation.rejectReason = $"total solution piece count too low {evaluation.totalNormalPieceCount + evaluation.fixedPieceCount} < {minTotalSolutionPieceCount}";
                return evaluation;
            }

            if (evaluation.exactCellCount < minExactCellCount)
            {
                evaluation.rejectReason = $"exactCellCount {evaluation.exactCellCount} < {minExactCellCount}";
                return evaluation;
            }

            if (!solverResult.isSolvable || solverResult.solutionCount <= 0)
            {
                evaluation.rejectReason = "not solvable";
                return evaluation;
            }

            if (requireAllPieces && !solverResult.requiresAllPieces)
            {
                evaluation.rejectReason = "does not require all pieces";
                return evaluation;
            }

            if (solverResult.solutionCount > maxAcceptedSolutions)
            {
                evaluation.rejectReason = $"too many exact solutions {solverResult.solutionCount} > {maxAcceptedSolutions}";
                return evaluation;
            }

            if (IsExactNumberTooBiased(stageData.boardText))
            {
                evaluation.rejectReason = "exact numbers are too biased";
                return evaluation;
            }

            if (requireFixedPieces && !HasMultipleTwoOrThreeExactCells(stageData.boardText))
            {
                evaluation.rejectReason = "not enough 2/3 exact cells";
                return evaluation;
            }

            if (validCellCount < 5)
            {
                evaluation.rejectReason = "active cell count too low";
                return evaluation;
            }

            ConstraintMetrics constraintMetrics = CalculateConstraintMetrics(stageData);
            evaluation.constrainedCellCount = constraintMetrics.constrainedCellCount;
            evaluation.minCoverCandidateCount = constraintMetrics.minCoverCandidateCount;
            evaluation.shapeHintScore = CalculateShapeHintScore(stageData);

            evaluation.solutionScore = CalculateExactSolutionScore(solverResult.solutionCount);
            evaluation.allPiecesScore = solverResult.requiresAllPieces ? 50 : 0;
            evaluation.exactCellCountScore = CalculateExactCellCountScore(evaluation.exactCellCount, evaluation.exactCellRate, minExactCellCount);
            evaluation.exactNumberVarietyScore = CalculateExactNumberVarietyScore(stageData.boardText);
            evaluation.solutionReductionScore = CalculateSolutionReductionScore(noExactSolverResult, solverResult);
            evaluation.pieceRoleScore = CalculatePieceRoleScore(stageData);
            evaluation.boardShapeScore = CalculateBoardShapeScore(stageData.boardText);
            evaluation.fixedPieceConstraintScore = requireFixedPieces ? CalculateFixedPieceConstraintScore(stageData) : 0;
            BoardNaturalnessMetrics naturalnessMetrics = CalculateBoardNaturalnessMetrics(stageData);
            evaluation.connectedComponentCount = naturalnessMetrics.connectedComponentCount;
            evaluation.smallIslandCount = naturalnessMetrics.smallIslandCount;
            evaluation.smallIslandPenalty = naturalnessMetrics.smallIslandPenalty;
            evaluation.isolatedPiecePenalty = naturalnessMetrics.isolatedPiecePenalty;
            evaluation.pieceSpreadPenalty = naturalnessMetrics.pieceSpreadPenalty;
            evaluation.tinyIslandWithPiecePenalty = naturalnessMetrics.tinyIslandWithPiecePenalty;
            evaluation.exactOnTinyIslandPenalty = naturalnessMetrics.exactOnTinyIslandPenalty;
            evaluation.unnaturalLongRangePenalty = naturalnessMetrics.unnaturalLongRangePenalty;

            int score = evaluation.solutionScore
                + evaluation.allPiecesScore
                + evaluation.exactCellCountScore
                + evaluation.exactNumberVarietyScore
                + evaluation.solutionReductionScore
                + evaluation.pieceRoleScore
                + evaluation.fixedPieceConstraintScore
                + evaluation.boardShapeScore
                + naturalnessMetrics.connectedComponentScore
                + evaluation.smallIslandPenalty
                + evaluation.isolatedPiecePenalty
                + evaluation.pieceSpreadPenalty
                + evaluation.tinyIslandWithPiecePenalty
                + evaluation.exactOnTinyIslandPenalty
                + evaluation.unnaturalLongRangePenalty;

            evaluation.accepted = true;
            evaluation.score = score;
            return evaluation;
        }

        private static int CountNormalPieces(StageData stageData)
        {
            int count = 0;

            if (stageData == null || stageData.pieceStocks == null)
            {
                return count;
            }

            foreach (StagePieceStock stock in stageData.pieceStocks)
            {
                if (stock != null && stock.pieceData != null && stock.count > 0)
                {
                    count += stock.count;
                }
            }

            return count;
        }

        private static int CountFixedPieces(StageData stageData)
        {
            int count = 0;

            if (stageData == null || stageData.fixedPieces == null)
            {
                return count;
            }

            foreach (FixedPieceData fixedPiece in stageData.fixedPieces)
            {
                if (fixedPiece != null && fixedPiece.pieceData != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountExactCells(string boardText)
        {
            int count = 0;

            foreach (char character in boardText)
            {
                if (character >= '1' && character <= '5')
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountDistinctExactNumbers(string boardText)
        {
            HashSet<char> numbers = new HashSet<char>();

            foreach (char character in boardText)
            {
                if (character >= '1' && character <= '5')
                {
                    numbers.Add(character);
                }
            }

            return numbers.Count;
        }

        private static Dictionary<char, int> CountExactNumbers(string boardText)
        {
            Dictionary<char, int> counts = new Dictionary<char, int>();

            foreach (char character in boardText)
            {
                if (character < '1' || character > '5')
                {
                    continue;
                }

                if (counts.ContainsKey(character))
                {
                    counts[character]++;
                }
                else
                {
                    counts.Add(character, 1);
                }
            }

            return counts;
        }

        private static string BuildExactNumberVarietyText(string boardText)
        {
            Dictionary<char, int> counts = CountExactNumbers(boardText);
            List<char> numbers = new List<char>(counts.Keys);
            numbers.Sort();
            return numbers.Count > 0 ? string.Join("/", numbers) : "none";
        }

        private static bool IsExactNumberTooBiased(string boardText)
        {
            Dictionary<char, int> counts = new Dictionary<char, int>();
            int total = 0;

            foreach (char character in boardText)
            {
                if (character < '1' || character > '5')
                {
                    continue;
                }

                total++;

                if (counts.ContainsKey(character))
                {
                    counts[character]++;
                }
                else
                {
                    counts.Add(character, 1);
                }
            }

            if (total < 4)
            {
                return false;
            }

            foreach (int count in counts.Values)
            {
                if (count >= Mathf.CeilToInt(total * 0.85f))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasMultipleTwoOrThreeExactCells(string boardText)
        {
            Dictionary<char, int> counts = CountExactNumbers(boardText);
            counts.TryGetValue('2', out int twoCount);
            counts.TryGetValue('3', out int threeCount);
            return twoCount + threeCount >= 2;
        }

        private static BoundsInt2D GetActiveBounds(string[] rows)
        {
            BoundsInt2D bounds = new BoundsInt2D
            {
                minX = int.MaxValue,
                minY = int.MaxValue,
                maxX = int.MinValue,
                maxY = int.MinValue
            };

            for (int y = 0; y < rows.Length; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    if (!IsActiveChar(rows[y][x]))
                    {
                        continue;
                    }

                    bounds.minX = Mathf.Min(bounds.minX, x);
                    bounds.maxX = Mathf.Max(bounds.maxX, x);
                    bounds.minY = Mathf.Min(bounds.minY, y);
                    bounds.maxY = Mathf.Max(bounds.maxY, y);
                }
            }

            if (bounds.minX == int.MaxValue)
            {
                bounds.minX = 0;
                bounds.maxX = 0;
                bounds.minY = 0;
                bounds.maxY = 0;
            }

            return bounds;
        }

        private static int CountActiveComponents(string[] rows)
        {
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            int components = 0;

            for (int y = 0; y < rows.Length; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    Vector2Int start = new Vector2Int(x, y);

                    if (!IsActiveChar(rows[y][x]) || visited.Contains(start))
                    {
                        continue;
                    }

                    components++;
                    Queue<Vector2Int> queue = new Queue<Vector2Int>();
                    queue.Enqueue(start);
                    visited.Add(start);

                    while (queue.Count > 0)
                    {
                        Vector2Int current = queue.Dequeue();

                        foreach (Vector2Int direction in OrthogonalDirections)
                        {
                            Vector2Int next = current + direction;

                            if (visited.Contains(next)
                                || next.y < 0
                                || next.y >= rows.Length
                                || next.x < 0
                                || next.x >= rows[next.y].Length
                                || !IsActiveChar(rows[next.y][next.x]))
                            {
                                continue;
                            }

                            visited.Add(next);
                            queue.Enqueue(next);
                        }
                    }
                }
            }

            return components;
        }

        private static bool HasNonStandardCells(string boardText)
        {
            foreach (char character in boardText)
            {
                if (character >= '1' && character <= '5')
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasFixedPieces(StageData stageData)
        {
            return stageData.fixedPieces != null && stageData.fixedPieces.Count > 0;
        }

        private static bool HasExactNumberOutsideRange(string boardText, int maxExactNumber)
        {
            foreach (char character in boardText)
            {
                if (character >= '1' && character <= '5' && character - '0' > maxExactNumber)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CalculateExactSolutionScore(int solutionCount)
        {
            if (solutionCount == 1)
            {
                return 40;
            }

            if (solutionCount <= 3)
            {
                return 30;
            }

            if (solutionCount <= 10)
            {
                return 15;
            }

            return 0;
        }

        private static int CalculateExactCellCountScore(int exactCellCount, float exactCellRate, int minExactCellCount)
        {
            int score = exactCellCount >= minExactCellCount ? 10 : 0;

            if (exactCellRate >= 0.35f && exactCellRate <= 0.55f)
            {
                score += 15;
            }
            else if (exactCellRate > 0.7f)
            {
                score -= 15;
            }
            else if (exactCellRate > 0.6f)
            {
                score -= 8;
            }

            return score;
        }

        private static int CalculateExactNumberVarietyScore(string boardText)
        {
            Dictionary<char, int> counts = CountExactNumbers(boardText);
            int score = 0;

            if (HasAnyExactNumberAtLeast(counts, '2'))
            {
                score += 10;
            }

            if (counts.ContainsKey('2') && counts.ContainsKey('3'))
            {
                score += 20;
            }

            if (counts.Count == 1 && counts.ContainsKey('1'))
            {
                score -= 20;
            }

            if (counts.ContainsKey('4') || counts.ContainsKey('5'))
            {
                score += 5;
            }

            int highCount = 0;
            counts.TryGetValue('4', out int fourCount);
            counts.TryGetValue('5', out int fiveCount);
            highCount = fourCount + fiveCount;
            int total = 0;

            foreach (int count in counts.Values)
            {
                total += count;
            }

            if (total > 0 && highCount > total * 0.45f)
            {
                score -= 10;
            }

            return score;
        }

        private static bool HasAnyExactNumberAtLeast(Dictionary<char, int> counts, char number)
        {
            for (char current = number; current <= '5'; current++)
            {
                if (counts.ContainsKey(current))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CalculateSolutionReductionScore(SolverResult noExactSolverResult, SolverResult exactSolverResult)
        {
            if (noExactSolverResult == null || exactSolverResult == null || exactSolverResult.solutionCount <= 0)
            {
                return 0;
            }

            if (noExactSolverResult.solutionCount >= 100 && exactSolverResult.solutionCount <= 5)
            {
                return 30;
            }

            if (noExactSolverResult.solutionCount > exactSolverResult.solutionCount * 5)
            {
                return 20;
            }

            return 0;
        }

        private static int CalculatePieceRoleScore(StageData stageData)
        {
            SolverResult solverResult = StageSolver.Solve(stageData, 1);

            if (solverResult.sampleSolutions.Count == 0)
            {
                return 0;
            }

            SolverBoard board = SolverBoard.Create(stageData);
            Solution solution = solverResult.sampleSolutions[0];
            int[] coverage = new int[board.activeCells.Count];
            List<List<int>> coveredByPiece = new List<List<int>>();

            foreach (SolverPlacement placement in solution.placements)
            {
                List<int> coveredIndices = board.GetCoveredIndices(placement.pieceData.pieceType, placement.position);
                coveredByPiece.Add(coveredIndices);

                foreach (int index in coveredIndices)
                {
                    coverage[index]++;
                }
            }

            bool everyPieceHasUniqueCell = true;
            bool everyPieceTouchesExactCell = true;
            int maxCoveredByOnePiece = 0;

            foreach (List<int> coveredIndices in coveredByPiece)
            {
                bool hasUniqueCell = false;
                bool touchesExactCell = false;
                maxCoveredByOnePiece = Mathf.Max(maxCoveredByOnePiece, coveredIndices.Count);

                foreach (int index in coveredIndices)
                {
                    if (coverage[index] == 1)
                    {
                        hasUniqueCell = true;
                    }

                    if (board.activeCells[index].requiresExactCover)
                    {
                        touchesExactCell = true;
                    }
                }

                everyPieceHasUniqueCell &= hasUniqueCell;
                everyPieceTouchesExactCell &= touchesExactCell;
            }

            int score = 0;
            score += everyPieceHasUniqueCell ? 20 : 0;
            score += everyPieceTouchesExactCell ? 10 : 0;

            if (board.activeCells.Count > 0 && maxCoveredByOnePiece > board.activeCells.Count * 0.7f)
            {
                score -= 10;
            }

            return score;
        }

        private static int CalculateFixedPieceConstraintScore(StageData stageData)
        {
            if (stageData == null || stageData.fixedPieces == null || stageData.fixedPieces.Count == 0)
            {
                return -30;
            }

            SolverBoard board = SolverBoard.Create(stageData);
            string[] rows = GetRows(stageData.boardText);
            Dictionary<Vector2Int, int> componentSizeByPosition = BuildComponentSizeMap(rows);
            int score = 0;
            int fixedPiecesTouchingExact = 0;
            int fixedCoverageOnExactCells = 0;
            int fixedCoverageOnHighExactCells = 0;

            foreach (FixedPieceData fixedPiece in stageData.fixedPieces)
            {
                if (fixedPiece == null || fixedPiece.pieceData == null || !board.positionToIndex.ContainsKey(fixedPiece.position))
                {
                    score -= 20;
                    continue;
                }

                int touchedExactCells = 0;
                int touchedHighExactCells = 0;

                foreach (int coveredIndex in board.GetCoveredIndices(fixedPiece.pieceData.pieceType, fixedPiece.position))
                {
                    SolverCell cell = board.activeCells[coveredIndex];

                    if (!cell.requiresExactCover)
                    {
                        continue;
                    }

                    touchedExactCells++;

                    if (cell.requiredCoverCount >= 2)
                    {
                        touchedHighExactCells++;
                    }
                }

                if (touchedExactCells > 0)
                {
                    fixedPiecesTouchingExact++;
                    fixedCoverageOnExactCells += touchedExactCells;
                    fixedCoverageOnHighExactCells += touchedHighExactCells;
                }
                else
                {
                    score -= 15;
                }

                if (componentSizeByPosition.TryGetValue(fixedPiece.position, out int componentSize) && componentSize <= 3)
                {
                    score -= 35;
                }
            }

            score += Mathf.Min(30, fixedCoverageOnExactCells * 5);
            score += Mathf.Min(25, fixedCoverageOnHighExactCells * 8);
            score += fixedPiecesTouchingExact == stageData.fixedPieces.Count ? 15 : 0;

            return score;
        }

        private static int CalculateBoardShapeScore(string boardText)
        {
            string[] rows = GetRows(boardText);
            int activeCount = CountActiveCellsStatic(boardText);

            if (activeCount <= 0)
            {
                return -30;
            }

            BoundsInt2D bounds = GetActiveBounds(rows);
            int boundsArea = Mathf.Max(1, bounds.width * bounds.height);
            int holes = CountInternalHoles(boardText);
            int components = CountActiveComponents(rows);
            int score = 0;

            if (activeCount < 6)
            {
                score -= 30;
            }
            else if (activeCount > boundsArea * 0.9f && holes == 0)
            {
                score -= 20;
            }
            else
            {
                score += 10;
            }

            if (holes > 0 || CountEdgeCuts(boardText) > 2)
            {
                score += 10;
            }

            if (components == 1)
            {
                score += 10;
            }
            else if (components > 2)
            {
                score -= 20;
            }

            if (activeCount > 35)
            {
                score -= 10;
            }

            return score;
        }

        private static BoardNaturalnessMetrics CalculateBoardNaturalnessMetrics(StageData stageData)
        {
            BoardNaturalnessMetrics metrics = new BoardNaturalnessMetrics();
            string[] rows = GetRows(stageData.boardText);
            List<List<Vector2Int>> components = GetActiveComponents(rows);
            Dictionary<Vector2Int, int> componentSizeByPosition = new Dictionary<Vector2Int, int>();
            metrics.connectedComponentCount = components.Count;

            if (components.Count == 1)
            {
                metrics.connectedComponentScore = 15;
            }
            else if (components.Count == 2)
            {
                metrics.connectedComponentScore = -5;
            }
            else if (components.Count >= 3)
            {
                metrics.connectedComponentScore = -30;
            }

            foreach (List<Vector2Int> component in components)
            {
                foreach (Vector2Int position in component)
                {
                    componentSizeByPosition[position] = component.Count;
                }

                if (component.Count <= 3)
                {
                    metrics.smallIslandCount++;
                }

                if (component.Count == 1)
                {
                    metrics.smallIslandPenalty -= 30;
                }
                else if (component.Count == 2)
                {
                    metrics.smallIslandPenalty -= 20;
                }
                else if (component.Count == 3)
                {
                    metrics.smallIslandPenalty -= 10;
                }
            }

            SolverResult solverResult = StageSolver.Solve(stageData, 1);
            Solution sampleSolution = solverResult.sampleSolutions.Count > 0 ? solverResult.sampleSolutions[0] : null;

            if (sampleSolution != null)
            {
                metrics.tinyIslandWithPiecePenalty = CalculateTinyIslandWithPiecePenalty(sampleSolution, componentSizeByPosition);
                metrics.isolatedPiecePenalty = CalculateIsolatedPiecePenalty(sampleSolution);
                metrics.pieceSpreadPenalty = CalculatePieceSpreadPenalty(sampleSolution);
                metrics.unnaturalLongRangePenalty = CalculateUnnaturalLongRangePenalty(stageData, sampleSolution);
            }

            metrics.exactOnTinyIslandPenalty = CalculateExactOnTinyIslandPenalty(rows, componentSizeByPosition);
            return metrics;
        }

        private static int CalculateTinyIslandWithPiecePenalty(Solution solution, Dictionary<Vector2Int, int> componentSizeByPosition)
        {
            int penalty = 0;

            foreach (SolverPlacement placement in solution.placements)
            {
                if (componentSizeByPosition.TryGetValue(placement.position, out int size) && size <= 3)
                {
                    penalty -= 40;
                }
            }

            return penalty;
        }

        private static int CalculateIsolatedPiecePenalty(Solution solution)
        {
            int penalty = 0;

            foreach (SolverPlacement placement in solution.placements)
            {
                int nearestDistance = int.MaxValue;

                foreach (SolverPlacement other in solution.placements)
                {
                    if (ReferenceEquals(placement, other))
                    {
                        continue;
                    }

                    nearestDistance = Mathf.Min(nearestDistance, ManhattanDistance(placement.position, other.position));
                }

                if (nearestDistance >= 7)
                {
                    penalty -= 30;
                }
                else if (nearestDistance >= 5)
                {
                    penalty -= 15;
                }
            }

            return penalty;
        }

        private static int CalculatePieceSpreadPenalty(Solution solution)
        {
            if (solution.placements.Count <= 1)
            {
                return 0;
            }

            int totalDistance = 0;
            int maxDistance = 0;
            int pairCount = 0;

            for (int i = 0; i < solution.placements.Count; i++)
            {
                for (int j = i + 1; j < solution.placements.Count; j++)
                {
                    int distance = ManhattanDistance(solution.placements[i].position, solution.placements[j].position);
                    totalDistance += distance;
                    maxDistance = Mathf.Max(maxDistance, distance);
                    pairCount++;
                }
            }

            float averageDistance = totalDistance / (float)Mathf.Max(1, pairCount);
            int penalty = 0;

            if (averageDistance > 5f)
            {
                penalty -= 15;
            }

            if (maxDistance >= 9)
            {
                penalty -= 15;
            }

            return penalty;
        }

        private static int CalculateExactOnTinyIslandPenalty(string[] rows, Dictionary<Vector2Int, int> componentSizeByPosition)
        {
            Dictionary<int, int> exactCountByComponentSize = new Dictionary<int, int>();
            int penalty = 0;

            for (int y = 0; y < rows.Length; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char boardChar = rows[y][x];

                    if (boardChar < '1' || boardChar > '5')
                    {
                        continue;
                    }

                    Vector2Int position = new Vector2Int(x, y);

                    if (!componentSizeByPosition.TryGetValue(position, out int size) || size > 3)
                    {
                        continue;
                    }

                    if (!exactCountByComponentSize.ContainsKey(size))
                    {
                        exactCountByComponentSize[size] = 0;
                    }

                    exactCountByComponentSize[size]++;
                }
            }

            foreach (KeyValuePair<int, int> entry in exactCountByComponentSize)
            {
                if (entry.Value >= 2)
                {
                    penalty -= 25;
                }

                if (entry.Value >= entry.Key)
                {
                    penalty -= 15;
                }
            }

            return penalty;
        }

        private static int CalculateUnnaturalLongRangePenalty(StageData stageData, Solution solution)
        {
            SolverBoard board = SolverBoard.Create(stageData);
            int penalty = 0;

            foreach (SolverPlacement placement in solution.placements)
            {
                if (placement.pieceData.pieceType != PieceType.Bishop
                    && placement.pieceData.pieceType != PieceType.Rook
                    && placement.pieceData.pieceType != PieceType.Queen)
                {
                    continue;
                }

                int farExactCount = 0;
                int nearContributionCount = 0;

                foreach (int coveredIndex in board.GetCoveredIndices(placement.pieceData.pieceType, placement.position))
                {
                    SolverCell cell = board.activeCells[coveredIndex];
                    int distance = ManhattanDistance(placement.position, cell.position);

                    if (cell.requiresExactCover && distance >= 5)
                    {
                        farExactCount++;
                    }

                    if (distance <= 3)
                    {
                        nearContributionCount++;
                    }
                }

                if (farExactCount >= 2 && nearContributionCount <= 2)
                {
                    penalty -= 20;
                }
            }

            return penalty;
        }

        private static List<List<Vector2Int>> GetActiveComponents(string[] rows)
        {
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            List<List<Vector2Int>> components = new List<List<Vector2Int>>();

            for (int y = 0; y < rows.Length; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    Vector2Int start = new Vector2Int(x, y);

                    if (!IsActiveChar(rows[y][x]) || visited.Contains(start))
                    {
                        continue;
                    }

                    List<Vector2Int> component = new List<Vector2Int>();
                    Queue<Vector2Int> queue = new Queue<Vector2Int>();
                    queue.Enqueue(start);
                    visited.Add(start);

                    while (queue.Count > 0)
                    {
                        Vector2Int current = queue.Dequeue();
                        component.Add(current);

                        foreach (Vector2Int direction in OrthogonalDirections)
                        {
                            Vector2Int next = current + direction;

                            if (visited.Contains(next)
                                || next.y < 0
                                || next.y >= rows.Length
                                || next.x < 0
                                || next.x >= rows[next.y].Length
                                || !IsActiveChar(rows[next.y][next.x]))
                            {
                                continue;
                            }

                            visited.Add(next);
                            queue.Enqueue(next);
                        }
                    }

                    components.Add(component);
                }
            }

            return components;
        }

        private static Dictionary<Vector2Int, int> BuildComponentSizeMap(string[] rows)
        {
            Dictionary<Vector2Int, int> componentSizeByPosition = new Dictionary<Vector2Int, int>();
            List<List<Vector2Int>> components = GetActiveComponents(rows);

            foreach (List<Vector2Int> component in components)
            {
                foreach (Vector2Int position in component)
                {
                    componentSizeByPosition[position] = component.Count;
                }
            }

            return componentSizeByPosition;
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static ConstraintMetrics CalculateConstraintMetrics(StageData stageData)
        {
            SolverBoard board = SolverBoard.Create(stageData);
            List<PieceData> pieces = StageSolver.ExpandPieceStocksForEvaluation(stageData.pieceStocks);
            ConstraintMetrics metrics = new ConstraintMetrics
            {
                minCoverCandidateCount = int.MaxValue
            };

            if (board.activeCells.Count == 0 || pieces.Count == 0)
            {
                metrics.minCoverCandidateCount = 0;
                return metrics;
            }

            int[] coverCandidateCounts = new int[board.activeCells.Count];

            foreach (PieceData piece in pieces)
            {
                foreach (Vector2Int origin in board.activePositions)
                {
                    foreach (int coveredIndex in board.GetCoveredIndices(piece.pieceType, origin))
                    {
                        coverCandidateCounts[coveredIndex]++;
                    }
                }
            }

            int total = 0;

            foreach (int count in coverCandidateCounts)
            {
                metrics.minCoverCandidateCount = Mathf.Min(metrics.minCoverCandidateCount, count);
                total += count;

                if (count >= 1 && count <= 3)
                {
                    metrics.constrainedCellCount++;
                }
            }

            metrics.averageCoverCandidateCount = total / (float)coverCandidateCounts.Length;
            return metrics;
        }

        private static int CalculateShapeHintScore(StageData stageData)
        {
            SolverBoard board = SolverBoard.Create(stageData);
            HashSet<PieceType> pieceTypes = GetPieceTypes(stageData);
            int score = 0;

            if (pieceTypes.Contains(PieceType.Rook) && HasStraightRun(board, minLength: 3))
            {
                score += 12;
            }

            if (pieceTypes.Contains(PieceType.Bishop) && HasDiagonalRun(board, minLength: 3))
            {
                score += 12;
            }

            if (pieceTypes.Contains(PieceType.Knight) && HasKnightHint(board))
            {
                score += 12;
            }

            if (pieceTypes.Contains(PieceType.King) && HasCompactCluster(board))
            {
                score += 10;
            }

            if (pieceTypes.Contains(PieceType.Pawn) && HasVerticalPair(board))
            {
                score += 8;
            }

            if (pieceTypes.Contains(PieceType.Queen))
            {
                score -= 8;
            }

            return score;
        }

        private static HashSet<PieceType> GetPieceTypes(StageData stageData)
        {
            HashSet<PieceType> pieceTypes = new HashSet<PieceType>();

            foreach (StagePieceStock stock in stageData.pieceStocks)
            {
                if (stock != null && stock.pieceData != null)
                {
                    pieceTypes.Add(stock.pieceData.pieceType);
                }
            }

            return pieceTypes;
        }

        private static bool HasStraightRun(SolverBoard board, int minLength)
        {
            foreach (Vector2Int position in board.activePositions)
            {
                if (CountRun(board, position, Vector2Int.right) >= minLength
                    || CountRun(board, position, Vector2Int.up) >= minLength)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasDiagonalRun(SolverBoard board, int minLength)
        {
            foreach (Vector2Int position in board.activePositions)
            {
                if (CountRun(board, position, new Vector2Int(1, 1)) >= minLength
                    || CountRun(board, position, new Vector2Int(1, -1)) >= minLength)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountRun(SolverBoard board, Vector2Int start, Vector2Int direction)
        {
            int count = 0;
            Vector2Int position = start;

            while (board.positionToIndex.ContainsKey(position))
            {
                count++;
                position += direction;
            }

            return count;
        }

        private static bool HasKnightHint(SolverBoard board)
        {
            foreach (Vector2Int position in board.activePositions)
            {
                int orthogonalNeighborCount = 0;
                bool hasKnightNeighbor = false;

                foreach (Vector2Int direction in OrthogonalDirections)
                {
                    if (board.positionToIndex.ContainsKey(position + direction))
                    {
                        orthogonalNeighborCount++;
                    }
                }

                foreach (Vector2Int direction in KnightDirections)
                {
                    if (board.positionToIndex.ContainsKey(position + direction))
                    {
                        hasKnightNeighbor = true;
                        break;
                    }
                }

                if (hasKnightNeighbor && orthogonalNeighborCount <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasCompactCluster(SolverBoard board)
        {
            foreach (Vector2Int position in board.activePositions)
            {
                int neighborCount = 0;

                foreach (Vector2Int direction in OrthogonalDirections)
                {
                    if (board.positionToIndex.ContainsKey(position + direction))
                    {
                        neighborCount++;
                    }
                }

                foreach (Vector2Int direction in DiagonalDirections)
                {
                    if (board.positionToIndex.ContainsKey(position + direction))
                    {
                        neighborCount++;
                    }
                }

                if (neighborCount >= 3)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasVerticalPair(SolverBoard board)
        {
            foreach (Vector2Int position in board.activePositions)
            {
                if (board.positionToIndex.ContainsKey(position + Vector2Int.up))
                {
                    return true;
                }
            }

            return false;
        }

        private class ConstraintMetrics
        {
            public int constrainedCellCount;
            public int minCoverCandidateCount;
            public float averageCoverCandidateCount;
        }

        private class BoardNaturalnessMetrics
        {
            public int connectedComponentCount;
            public int connectedComponentScore;
            public int smallIslandCount;
            public int smallIslandPenalty;
            public int isolatedPiecePenalty;
            public int pieceSpreadPenalty;
            public int tinyIslandWithPiecePenalty;
            public int exactOnTinyIslandPenalty;
            public int unnaturalLongRangePenalty;
        }

        private static int CountDistinctPieceTypes(StageData stageData)
        {
            HashSet<PieceType> pieceTypes = new HashSet<PieceType>();

            foreach (StagePieceStock stock in stageData.pieceStocks)
            {
                if (stock != null && stock.pieceData != null)
                {
                    pieceTypes.Add(stock.pieceData.pieceType);
                }
            }

            foreach (FixedPieceData fixedPiece in stageData.fixedPieces)
            {
                if (fixedPiece != null && fixedPiece.pieceData != null)
                {
                    pieceTypes.Add(fixedPiece.pieceData.pieceType);
                }
            }

            return pieceTypes.Count;
        }

        private static int CountInternalHoles(string boardText)
        {
            string[] rows = GetRows(boardText);
            int holes = 0;

            for (int y = 1; y < rows.Length - 1; y++)
            {
                for (int x = 1; x < rows[y].Length - 1; x++)
                {
                    if (rows[y][x] != 'X')
                    {
                        continue;
                    }

                    if (IsActiveChar(GetChar(rows, x + 1, y))
                        && IsActiveChar(GetChar(rows, x - 1, y))
                        && IsActiveChar(GetChar(rows, x, y + 1))
                        && IsActiveChar(GetChar(rows, x, y - 1)))
                    {
                        holes++;
                    }
                }
            }

            return holes;
        }

        private static int CountEdgeCuts(string boardText)
        {
            string[] rows = GetRows(boardText);
            int cuts = 0;

            foreach (string row in rows)
            {
                if (!string.IsNullOrEmpty(row) && (row[0] == 'X' || row[row.Length - 1] == 'X'))
                {
                    cuts++;
                }
            }

            return cuts;
        }
    }

    private static class StageSolver
    {
        private const int MaxSearchNodes = 200000;

        public static SolverResult Solve(StageData stageData, int maxSolutions)
        {
            SolverBoard board = SolverBoard.Create(stageData);
            SolverResult result = new SolverResult();

            if (board.activeCells.Count == 0)
            {
                result.minPiecesUsed = 0;
                return result;
            }

            List<PieceData> pieces = ExpandPieceStocks(stageData.pieceStocks);
            int totalAvailablePieces = pieces.Count;
            int[] coverage = new int[board.activeCells.Count];
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

            ApplyFixedPieces(stageData.fixedPieces, board, coverage, occupied);

            SolverSearchState state = new SolverSearchState
            {
                board = board,
                pieces = pieces,
                coverage = coverage,
                occupied = occupied,
                result = result,
                maxSolutions = Mathf.Max(1, maxSolutions)
            };

            Search(state, 0, 0, new List<SolverPlacement>());

            result.isSolvable = result.solutionCount > 0;
            result.requiresAllPieces = result.isSolvable && result.minPiecesUsed == totalAvailablePieces;

            if (!result.isSolvable)
            {
                result.minPiecesUsed = 0;
            }

            return result;
        }

        public static List<PieceData> ExpandPieceStocksForEvaluation(List<StagePieceStock> pieceStocks)
        {
            return ExpandPieceStocks(pieceStocks);
        }

        private static void Search(
            SolverSearchState state,
            int pieceIndex,
            int piecesUsed,
            List<SolverPlacement> placements
        )
        {
            if (state.result.solutionCount >= state.maxSolutions || state.searchNodes >= MaxSearchNodes)
            {
                return;
            }

            state.searchNodes++;

            if (pieceIndex >= state.pieces.Count)
            {
                if (!IsClear(state.board, state.coverage))
                {
                    return;
                }

                state.result.solutionCount++;
                state.result.minPiecesUsed = Mathf.Min(state.result.minPiecesUsed, piecesUsed);

                if (state.result.sampleSolutions.Count < 3)
                {
                    state.result.sampleSolutions.Add(new Solution
                    {
                        placements = new List<SolverPlacement>(placements)
                    });
                }

                return;
            }

            Search(state, pieceIndex + 1, piecesUsed, placements);

            PieceData pieceData = state.pieces[pieceIndex];

            foreach (Vector2Int position in state.board.activePositions)
            {
                if (state.occupied.Contains(position))
                {
                    continue;
                }

                List<int> coveredIndices = state.board.GetCoveredIndices(pieceData.pieceType, position);

                if (coveredIndices.Count == 0)
                {
                    continue;
                }

                foreach (int coveredIndex in coveredIndices)
                {
                    state.coverage[coveredIndex]++;
                }

                state.occupied.Add(position);
                placements.Add(new SolverPlacement
                {
                    pieceData = pieceData,
                    position = position
                });

                Search(state, pieceIndex + 1, piecesUsed + 1, placements);

                placements.RemoveAt(placements.Count - 1);
                state.occupied.Remove(position);

                foreach (int coveredIndex in coveredIndices)
                {
                    state.coverage[coveredIndex]--;
                }

                if (state.result.solutionCount >= state.maxSolutions || state.searchNodes >= MaxSearchNodes)
                {
                    return;
                }
            }
        }

        private static void ApplyFixedPieces(
            List<FixedPieceData> fixedPieces,
            SolverBoard board,
            int[] coverage,
            HashSet<Vector2Int> occupied
        )
        {
            if (fixedPieces == null)
            {
                return;
            }

            foreach (FixedPieceData fixedPiece in fixedPieces)
            {
                if (fixedPiece == null || fixedPiece.pieceData == null || !board.positionToIndex.ContainsKey(fixedPiece.position))
                {
                    continue;
                }

                occupied.Add(fixedPiece.position);

                foreach (int coveredIndex in board.GetCoveredIndices(fixedPiece.pieceData.pieceType, fixedPiece.position))
                {
                    coverage[coveredIndex]++;
                }
            }
        }

        private static bool IsClear(SolverBoard board, int[] coverage)
        {
            for (int i = 0; i < board.activeCells.Count; i++)
            {
                SolverCell cell = board.activeCells[i];

                if (cell.requiresExactCover)
                {
                    if (coverage[i] != cell.requiredCoverCount)
                    {
                        return false;
                    }
                }
                else if (coverage[i] < 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<PieceData> ExpandPieceStocks(List<StagePieceStock> pieceStocks)
        {
            List<PieceData> pieces = new List<PieceData>();

            if (pieceStocks == null)
            {
                return pieces;
            }

            foreach (StagePieceStock stock in pieceStocks)
            {
                if (stock == null || stock.pieceData == null || stock.count <= 0)
                {
                    continue;
                }

                for (int i = 0; i < stock.count; i++)
                {
                    pieces.Add(stock.pieceData);
                }
            }

            pieces.Sort((left, right) => right.pieceType.CompareTo(left.pieceType));
            return pieces;
        }
    }

    private class SolverSearchState
    {
        public SolverBoard board;
        public List<PieceData> pieces;
        public int[] coverage;
        public HashSet<Vector2Int> occupied;
        public SolverResult result;
        public int maxSolutions;
        public int searchNodes;
    }

    private class SolverBoard
    {
        public List<SolverCell> activeCells = new List<SolverCell>();
        public List<Vector2Int> activePositions = new List<Vector2Int>();
        public Dictionary<Vector2Int, int> positionToIndex = new Dictionary<Vector2Int, int>();

        public static SolverBoard Create(StageData stageData)
        {
            SolverBoard board = new SolverBoard();
            string[] rows = GetRows(stageData.boardText);

            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                string row = rows[rowIndex];
                int y = rows.Length - 1 - rowIndex;

                for (int x = 0; x < row.Length; x++)
                {
                    char boardChar = row[x];

                    if (!IsActiveChar(boardChar))
                    {
                        continue;
                    }

                    Vector2Int position = new Vector2Int(x, y);
                    SolverCell cell = new SolverCell
                    {
                        position = position,
                        requiresExactCover = boardChar >= '1' && boardChar <= '5',
                        requiredCoverCount = boardChar >= '1' && boardChar <= '5' ? boardChar - '0' : 1
                    };
                    board.positionToIndex.Add(position, board.activeCells.Count);
                    board.activeCells.Add(cell);
                    board.activePositions.Add(position);
                }
            }

            return board;
        }

        public List<int> GetCoveredIndices(PieceType pieceType, Vector2Int origin)
        {
            List<int> results = new List<int>();
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

        private void AddStepMoves(Vector2Int origin, Vector2Int[] directions, List<int> results)
        {
            foreach (Vector2Int direction in directions)
            {
                TryAddPosition(origin + direction, results);
            }
        }

        private void AddLineMoves(Vector2Int origin, Vector2Int[] directions, List<int> results)
        {
            foreach (Vector2Int direction in directions)
            {
                Vector2Int position = origin + direction;

                while (positionToIndex.ContainsKey(position))
                {
                    TryAddPosition(position, results);
                    position += direction;
                }
            }
        }

        private void TryAddPosition(Vector2Int position, List<int> results)
        {
            if (positionToIndex.TryGetValue(position, out int index) && !results.Contains(index))
            {
                results.Add(index);
            }
        }
    }

    private class SolverCell
    {
        public Vector2Int position;
        public bool requiresExactCover;
        public int requiredCoverCount;
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

    private static string[] GetRows(string boardText)
    {
        if (string.IsNullOrWhiteSpace(boardText))
        {
            return Array.Empty<string>();
        }

        return boardText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    private static char GetChar(string[] rows, int x, int y)
    {
        if (y < 0 || y >= rows.Length || x < 0 || x >= rows[y].Length)
        {
            return 'X';
        }

        return rows[y][x];
    }

    private static bool IsActiveChar(char boardChar)
    {
        return boardChar == 'Z'
            || boardChar == 'z'
            || boardChar == '#'
            || (boardChar >= '1' && boardChar <= '5');
    }

    private static int CountActiveCellsStatic(string boardText)
    {
        int count = 0;

        foreach (char character in boardText)
        {
            if (IsActiveChar(character))
            {
                count++;
            }
        }

        return count;
    }

    private class StageGenerationSettings
    {
        public GenerationMode mode;
        public GenerationProfile profile;
        public int seed;
        public int width;
        public int height;
        public int pieceCount;
        public bool includeExactCoverCells;
        public float exactCoverCellRate;
        public int maxExactCoverNumber;
        public bool includeFixedPieces;
        public int fixedPieceCount;
        public bool isAdvancedExactFixed;
        public float removedCellRate;
        public BoardShapeBias shapeBias;
        public List<PieceData> pieceSequence = new List<PieceData>();

        public string GetPieceSignature()
        {
            List<string> pieces = new List<string>();

            foreach (PieceData pieceData in pieceSequence)
            {
                pieces.Add(pieceData != null ? pieceData.pieceType.ToString() : "None");
            }

            pieces.Sort();
            return string.Join(",", pieces);
        }
    }

    private class GrowingPlacementCandidate
    {
        public Vector2Int position;
        public List<Vector2Int> coverage;
        public int overlapCount;
        public int newCellCount;
        public int distanceToExistingBoard;
    }

    private class ExactCellCandidate
    {
        public Vector2Int position;
        public int coverCount;
        public int score;
    }

    private struct BoundsInt2D
    {
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;

        public int width => maxX - minX + 1;
        public int height => maxY - minY + 1;
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
