using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class ExactFixedStageGeneratorWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/unsung/StageData";

    [SerializeField] private DefaultAsset outputFolder;
    [SerializeField] private string stageNamePrefix = "ExactFixed";
    [SerializeField] private int desiredOutputCount = 5;
    [SerializeField] private int candidateCount = 100;
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;
    [SerializeField] private int normalPieceCount = 5;
    [SerializeField] private int minFixedPieceCount = 1;
    [SerializeField] private int maxFixedPieceCount = 2;
    [SerializeField] private int overlapExactCellCount = 5;
    [SerializeField] private int minSingleCoverExactCellCount = 0;
    [SerializeField] private int maxSingleCoverExactCellCount = 2;
    [SerializeField] private int maxRemoveSingleCoverCells = 4;
    [SerializeField] private int maxSolutions = 50;
    [SerializeField] private int maxAcceptedSolutions = 10;
    [SerializeField] private int maxSearchNodes = 150000;
    [SerializeField] private int randomSeed = 12345;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private List<PieceData> candidatePieces = new List<PieceData>();
    [SerializeField] private List<FixedPieceDataMap> fixedPieceDataMaps = new List<FixedPieceDataMap>();

    private Vector2 scrollPosition;

    [MenuItem("Tools/ChessPuzzle/Exact Fixed Stage Generator")]
    public static void Open()
    {
        GetWindow<ExactFixedStageGeneratorWindow>("Exact Fixed Stage Generator");
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
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Exact + Fixed Stage Generator", EditorStyles.boldLabel);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);
        stageNamePrefix = EditorGUILayout.TextField("Stage Name Prefix", stageNamePrefix);

        EditorGUILayout.Space();
        desiredOutputCount = Mathf.Max(1, EditorGUILayout.IntField("Desired Output Count", desiredOutputCount));
        candidateCount = Mathf.Max(1, EditorGUILayout.IntField("Candidate Count", candidateCount));
        width = Mathf.Max(1, EditorGUILayout.IntField("Width", width));
        height = Mathf.Max(1, EditorGUILayout.IntField("Height", height));
        normalPieceCount = Mathf.Clamp(EditorGUILayout.IntField("Normal Piece Count", normalPieceCount), 1, width * height);
        minFixedPieceCount = Mathf.Max(1, EditorGUILayout.IntField("Min Fixed Piece Count", minFixedPieceCount));
        maxFixedPieceCount = Mathf.Max(minFixedPieceCount, EditorGUILayout.IntField("Max Fixed Piece Count", maxFixedPieceCount));

        EditorGUILayout.Space();
        overlapExactCellCount = Mathf.Max(1, EditorGUILayout.IntField("Overlap Exact Cell Count", overlapExactCellCount));
        minSingleCoverExactCellCount = Mathf.Max(0, EditorGUILayout.IntField("Min Single Cover Exact Cell Count", minSingleCoverExactCellCount));
        maxSingleCoverExactCellCount = Mathf.Max(minSingleCoverExactCellCount, EditorGUILayout.IntField("Max Single Cover Exact Cell Count", maxSingleCoverExactCellCount));
        maxRemoveSingleCoverCells = Mathf.Max(0, EditorGUILayout.IntField("Max Remove Single Cover Cells", maxRemoveSingleCoverCells));

        EditorGUILayout.Space();
        maxSolutions = Mathf.Max(1, EditorGUILayout.IntField("Max Solutions", maxSolutions));
        maxAcceptedSolutions = Mathf.Max(1, EditorGUILayout.IntField("Max Accepted Solutions", maxAcceptedSolutions));
        maxSearchNodes = Mathf.Max(1, EditorGUILayout.IntField("Max Search Nodes", maxSearchNodes));
        randomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);
        useRandomSeed = EditorGUILayout.Toggle("Use Random Seed", useRandomSeed);

        EditorGUILayout.Space();
        DrawCandidatePieceList();
        EditorGUILayout.Space();
        DrawFixedPieceDataMaps();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!CanGenerate()))
        {
            if (GUILayout.Button("Generate", GUILayout.Height(32f)))
            {
                Generate();
            }
        }

        EditorGUILayout.HelpBox(
            "Experimental generator for stages with exact cover cells and fixed pieces. Existing StageAutoGeneratorWindow is not used.",
            MessageType.Info
        );

        EditorGUILayout.EndScrollView();
    }

    private void DrawCandidatePieceList()
    {
        EditorGUILayout.LabelField("Candidate PieceData List", EditorStyles.boldLabel);

        for (int i = 0; i < candidatePieces.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            candidatePieces[i] = (PieceData)EditorGUILayout.ObjectField(candidatePieces[i], typeof(PieceData), false);

            if (GUILayout.Button("Remove", GUILayout.Width(70f)))
            {
                candidatePieces.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add PieceData"))
        {
            candidatePieces.Add(null);
        }

        if (GUILayout.Button("Load All PieceData"))
        {
            LoadAllPieceDataAssets();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFixedPieceDataMaps()
    {
        EditorGUILayout.LabelField("Fixed Piece Data Maps", EditorStyles.boldLabel);

        for (int i = 0; i < fixedPieceDataMaps.Count; i++)
        {
            if (fixedPieceDataMaps[i] == null)
            {
                fixedPieceDataMaps[i] = new FixedPieceDataMap();
            }

            EditorGUILayout.BeginHorizontal();
            fixedPieceDataMaps[i].normalPieceData = (PieceData)EditorGUILayout.ObjectField(
                fixedPieceDataMaps[i].normalPieceData,
                typeof(PieceData),
                false
            );
            fixedPieceDataMaps[i].fixedPieceData = (PieceData)EditorGUILayout.ObjectField(
                fixedPieceDataMaps[i].fixedPieceData,
                typeof(PieceData),
                false
            );

            if (GUILayout.Button("Remove", GUILayout.Width(70f)))
            {
                fixedPieceDataMaps.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Fixed Map"))
        {
            fixedPieceDataMaps.Add(new FixedPieceDataMap());
        }
    }

    private bool CanGenerate()
    {
        return GetOutputFolderPath() != null
            && !string.IsNullOrWhiteSpace(stageNamePrefix)
            && candidatePieces.Exists(pieceData => pieceData != null);
    }

    private void Generate()
    {
        string folderPath = GetOutputFolderPath();

        if (folderPath == null)
        {
            EditorUtility.DisplayDialog("Exact Fixed Stage Generator", "Select an output folder under Assets.", "OK");
            return;
        }

        ExactFixedSettings settings = new ExactFixedSettings
        {
            width = width,
            height = height,
            normalPieceCount = normalPieceCount,
            minFixedPieceCount = minFixedPieceCount,
            maxFixedPieceCount = maxFixedPieceCount,
            overlapExactCellCount = overlapExactCellCount,
            minSingleCoverExactCellCount = minSingleCoverExactCellCount,
            maxSingleCoverExactCellCount = maxSingleCoverExactCellCount,
            maxRemoveSingleCoverCells = maxRemoveSingleCoverCells,
            maxSolutions = maxSolutions,
            maxAcceptedSolutions = maxAcceptedSolutions,
            maxSearchNodes = maxSearchNodes,
            candidatePieces = new List<PieceData>(candidatePieces),
            seed = useRandomSeed ? Environment.TickCount : randomSeed
        };

        settings.candidatePieces.RemoveAll(pieceData => pieceData == null);

        try
        {
            System.Random random = new System.Random(settings.seed);
            List<ExactFixedCandidate> accepted = new List<ExactFixedCandidate>();

            for (int i = 0; i < candidateCount && accepted.Count < desiredOutputCount; i++)
            {
                Debug.Log($"[EXACT FIXED GEN] Try candidate {i + 1}");
                ExactFixedCandidate candidate = TryCreateCandidate(settings, random, i + 1);

                if (candidate != null)
                {
                    accepted.Add(candidate);
                }
            }

            List<StageData> savedStages = new List<StageData>();

            for (int i = 0; i < accepted.Count; i++)
            {
                string stageName = $"{stageNamePrefix}_{i + 1:000}";
                string assetPath;
                SaveValidationResult saveValidation = ValidateBeforeSave(accepted[i], stageName);

                LogSaveValidation(stageName, accepted[i], saveValidation);

                if (!saveValidation.isValid)
                {
                    Debug.LogError($"[EXACT FIXED GEN ERROR] Save validation failed stage={stageName} reason={saveValidation.reason}");
                    continue;
                }

                StageData stageData = SaveCandidate(accepted[i], folderPath, stageName, out assetPath);
                savedStages.Add(stageData);
                Debug.Log($"[EXACT FIXED GEN] saved {assetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Exact Fixed Stage Generator",
                $"Seed: {settings.seed}\nAccepted: {accepted.Count}\nSaved: {savedStages.Count}",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Exact Fixed Stage Generator", exception.Message, "OK");
        }
    }

    // Generator
    private ExactFixedCandidate TryCreateCandidate(ExactFixedSettings settings, System.Random random, int serialNumber)
    {
        ExactFixedCandidate candidate = new ExactFixedCandidate
        {
            width = settings.width,
            height = settings.height
        };

        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        AddRandomPlacements(candidate.normalPlacements, settings.normalPieceCount, settings, occupied, random);

        int fixedCount = RandomRangeInclusive(random, settings.minFixedPieceCount, settings.maxFixedPieceCount);
        AddRandomPlacements(candidate.fixedPlacements, fixedCount, settings, occupied, random);

        Debug.Log($"[EXACT FIXED GEN] placements normal={candidate.normalPlacements.Count} fixed={candidate.fixedPlacements.Count}");

        HashSet<Vector2Int> fullBoard = BuildFullBoard(settings.width, settings.height);
        List<StagePiecePlacement> allPlacements = candidate.GetAllPlacements();
        candidate.coverCounts = StageCoverageUtility.CalculateCoverCounts(allPlacements, fullBoard);
        candidate.activeCells = new HashSet<Vector2Int>(candidate.coverCounts.Keys);

        if (candidate.activeCells.Count < Mathf.Max(6, settings.normalPieceCount + fixedCount + 3))
        {
            return Reject(candidate, "activeCells too low");
        }

        if (!ChooseExactCells(candidate, settings, random))
        {
            return Reject(candidate, "not enough exact cell candidates");
        }

        Debug.Log($"[EXACT FIXED GEN] overlapExact={candidate.overlapExactCount} singleExact={candidate.singleExactCount}");

        TryRemoveSingleCoverCells(candidate, settings, random);
        candidate.removedCellCount = settings.width * settings.height - candidate.activeCells.Count;
        Debug.Log($"[EXACT FIXED GEN] removedCells={candidate.removedCellCount}");

        candidate.coverCounts = StageCoverageUtility.CalculateCoverCounts(candidate.GetAllPlacements(), candidate.activeCells);
        ValidationResult validation = ValidateKnownSolution(candidate);
        candidate.validation = validation;
        Debug.Log($"[EXACT FIXED GEN] knownSolutionValid={validation.isValid} reason={validation.reason}");

        if (!validation.isValid)
        {
            return null;
        }

        if (!FixedPiecesAffectExactCells(candidate))
        {
            return Reject(candidate, "fixed pieces do not affect exact cells");
        }

        if (CountSingleCellIslands(candidate.activeCells) > 1)
        {
            return Reject(candidate, "too many single-cell islands");
        }

        candidate.solverResult = ExactFixedSolver.Solve(
            candidate.activeCells,
            candidate.exactCells,
            candidate.normalPlacements,
            candidate.fixedPlacements,
            settings.maxSolutions,
            settings.maxSearchNodes
        );

        Debug.Log($"[EXACT FIXED GEN] solver solutions={candidate.solverResult.solutionCount} status={candidate.solverResult.status}");

        if (candidate.solverResult.hitNodeLimit
            || candidate.solverResult.hitSolutionLimit
            || candidate.solverResult.solutionCount < 1
            || candidate.solverResult.solutionCount > settings.maxAcceptedSolutions
            || candidate.solverResult.minPiecesUsed != settings.normalPieceCount)
        {
            return null;
        }

        candidate.coverCounts = StageCoverageUtility.CalculateCoverCounts(candidate.GetAllPlacements(), candidate.activeCells);
        ValidationResult finalValidation = ValidateKnownSolution(candidate);
        candidate.validation = finalValidation;
        Debug.Log($"[EXACT FIXED GEN] knownSolutionValid={finalValidation.isValid} reason={finalValidation.reason}");

        if (!finalValidation.isValid)
        {
            return null;
        }

        candidate.boardText = BuildBoardText(candidate);
        return candidate;
    }

    private ExactFixedCandidate Reject(ExactFixedCandidate candidate, string reason)
    {
        candidate.validation = new ValidationResult { isValid = false, reason = reason };
        Debug.Log($"[EXACT FIXED GEN] knownSolutionValid=false reason={reason}");
        return null;
    }

    private void AddRandomPlacements(
        List<StagePiecePlacement> target,
        int count,
        ExactFixedSettings settings,
        HashSet<Vector2Int> occupied,
        System.Random random
    )
    {
        for (int i = 0; i < count; i++)
        {
            target.Add(new StagePiecePlacement
            {
                pieceData = settings.candidatePieces[random.Next(settings.candidatePieces.Count)],
                position = GetUnusedPosition(settings.width, settings.height, occupied, random)
            });
        }
    }

    private bool ChooseExactCells(ExactFixedCandidate candidate, ExactFixedSettings settings, System.Random random)
    {
        HashSet<Vector2Int> occupied = candidate.GetOccupiedPositions();
        List<Vector2Int> overlapCandidates = new List<Vector2Int>();
        List<Vector2Int> singleCandidates = new List<Vector2Int>();

        foreach (KeyValuePair<Vector2Int, int> coverCount in candidate.coverCounts)
        {
            if (occupied.Contains(coverCount.Key) || coverCount.Value < 1 || coverCount.Value > 5)
            {
                continue;
            }

            if (coverCount.Value >= 2)
            {
                overlapCandidates.Add(coverCount.Key);
            }
            else
            {
                singleCandidates.Add(coverCount.Key);
            }
        }

        if (overlapCandidates.Count < settings.overlapExactCellCount)
        {
            return false;
        }

        Shuffle(overlapCandidates, random);

        for (int i = 0; i < settings.overlapExactCellCount; i++)
        {
            Vector2Int position = overlapCandidates[i];
            candidate.exactCells[position] = candidate.coverCounts[position];
            candidate.overlapExactCount++;
        }

        Shuffle(singleCandidates, random);
        int singleTarget = RandomRangeInclusive(random, settings.minSingleCoverExactCellCount, settings.maxSingleCoverExactCellCount);

        if (singleCandidates.Count < settings.minSingleCoverExactCellCount)
        {
            return false;
        }

        for (int i = 0; i < Mathf.Min(singleTarget, singleCandidates.Count); i++)
        {
            Vector2Int position = singleCandidates[i];

            if (candidate.exactCells.ContainsKey(position))
            {
                continue;
            }

            candidate.exactCells[position] = 1;
            candidate.singleExactCount++;
        }

        return candidate.overlapExactCount >= 1;
    }

    private void TryRemoveSingleCoverCells(ExactFixedCandidate candidate, ExactFixedSettings settings, System.Random random)
    {
        HashSet<Vector2Int> occupied = candidate.GetOccupiedPositions();
        List<Vector2Int> removalCandidates = new List<Vector2Int>();

        foreach (Vector2Int position in candidate.activeCells)
        {
            if (candidate.exactCells.ContainsKey(position)
                || occupied.Contains(position)
                || !candidate.coverCounts.TryGetValue(position, out int coverCount)
                || coverCount != 1)
            {
                continue;
            }

            removalCandidates.Add(position);
        }

        Shuffle(removalCandidates, random);
        int removed = 0;

        foreach (Vector2Int position in removalCandidates)
        {
            if (removed >= settings.maxRemoveSingleCoverCells)
            {
                return;
            }

            HashSet<Vector2Int> tempActiveCells = new HashSet<Vector2Int>(candidate.activeCells);
            tempActiveCells.Remove(position);
            Dictionary<Vector2Int, int> tempCoverage = StageCoverageUtility.CalculateCoverCounts(candidate.GetAllPlacements(), tempActiveCells);
            ValidationResult validation = ValidateKnownSolution(tempActiveCells, candidate.exactCells, tempCoverage, candidate.GetAllPlacements());

            if (!validation.isValid)
            {
                continue;
            }

            candidate.activeCells = tempActiveCells;
            candidate.coverCounts = tempCoverage;
            removed++;
        }
    }

    // Validation
    private ValidationResult ValidateKnownSolution(ExactFixedCandidate candidate)
    {
        return ValidateKnownSolution(candidate.activeCells, candidate.exactCells, candidate.coverCounts, candidate.GetAllPlacements());
    }

    private ValidationResult ValidateKnownSolution(
        HashSet<Vector2Int> activeCells,
        Dictionary<Vector2Int, int> exactCells,
        Dictionary<Vector2Int, int> coverCounts,
        List<StagePiecePlacement> allPlacements
    )
    {
        foreach (StagePiecePlacement placement in allPlacements)
        {
            if (placement == null || placement.pieceData == null)
            {
                return new ValidationResult { isValid = false, reason = "null placement" };
            }

            if (!activeCells.Contains(placement.position))
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Piece on inactive cell pos={placement.position}");
                return new ValidationResult { isValid = false, reason = $"piece outside active cells {placement.position}" };
            }
        }

        foreach (Vector2Int position in activeCells)
        {
            if (!coverCounts.TryGetValue(position, out int coverCount) || coverCount < 1)
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Uncovered active cell pos={position}");
                return new ValidationResult { isValid = false, reason = $"uncovered active cell {position}" };
            }

            if (exactCells.TryGetValue(position, out int requiredCoverCount) && coverCount != requiredCoverCount)
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Exact cell mismatch pos={position} required={requiredCoverCount} actual={coverCount}");
                return new ValidationResult
                {
                    isValid = false,
                    reason = $"exact mismatch at {position}: expected {requiredCoverCount}, actual {coverCount}"
                };
            }
        }

        foreach (KeyValuePair<Vector2Int, int> exactCell in exactCells)
        {
            if (!activeCells.Contains(exactCell.Key) || exactCell.Value < 1 || exactCell.Value > 5)
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Invalid exact cell pos={exactCell.Key} required={exactCell.Value}");
                return new ValidationResult { isValid = false, reason = $"invalid exact cell {exactCell.Key}" };
            }
        }

        return new ValidationResult { isValid = true, reason = "ok" };
    }

    private SaveValidationResult ValidateBeforeSave(ExactFixedCandidate candidate, string stageName)
    {
        candidate.boardText = BuildBoardText(candidate);
        HashSet<Vector2Int> boardActiveCells = ParseActiveCellsFromBoardText(candidate.boardText);
        Dictionary<Vector2Int, int> boardExactCells = ParseExactCellsFromBoardText(candidate.boardText);

        if (!SetsEqual(candidate.activeCells, boardActiveCells))
        {
            return new SaveValidationResult
            {
                isValid = false,
                reason = "boardText active cells do not match candidate active cells"
            };
        }

        if (!DictionariesEqual(candidate.exactCells, boardExactCells))
        {
            return new SaveValidationResult
            {
                isValid = false,
                reason = "boardText exact cells do not match candidate exact cells"
            };
        }

        Dictionary<PieceData, int> requiredNormalCounts = CountPlacementsByPieceData(candidate.normalPlacements);
        List<StagePieceStock> pieceStocks = BuildPieceStocks(candidate.normalPlacements);
        List<FixedPieceData> fixedPieces = BuildFixedPieces(candidate.fixedPlacements, out List<FixedPieceSaveLog> fixedSaveLogs);

        LogNormalPieceCounts(requiredNormalCounts);
        LogPieceStocks(pieceStocks);
        LogFixedPieceSaves(fixedSaveLogs);

        if (CountExpandedStocks(pieceStocks) != candidate.normalPlacements.Count)
        {
            return new SaveValidationResult { isValid = false, reason = "pieceStocks normal piece count mismatch" };
        }

        SaveValidationResult stockValidation = ValidatePieceStocks(requiredNormalCounts, pieceStocks);
        if (!stockValidation.isValid)
        {
            return stockValidation;
        }

        if (fixedPieces.Count != candidate.fixedPlacements.Count)
        {
            return new SaveValidationResult { isValid = false, reason = "fixed piece count mismatch" };
        }

        for (int i = 0; i < candidate.fixedPlacements.Count; i++)
        {
            StagePiecePlacement fixedPlacement = candidate.fixedPlacements[i];
            FixedPieceData savedFixedPiece = fixedPieces[i];

            if (savedFixedPiece.pieceData == null)
            {
                return new SaveValidationResult { isValid = false, reason = $"fixed map not found for {fixedPlacement.pieceData.name}" };
            }

            if (savedFixedPiece.position != fixedPlacement.position)
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Fixed piece position mismatch pos={fixedPlacement.position}");
                return new SaveValidationResult { isValid = false, reason = $"fixed piece position mismatch {fixedPlacement.position}" };
            }

            if (!IsMappedFixedPieceData(savedFixedPiece.pieceData))
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Fixed piece saved with normal PieceData: {savedFixedPiece.pieceData.name}");
                return new SaveValidationResult { isValid = false, reason = $"fixed piece saved with normal PieceData {savedFixedPiece.pieceData.name}" };
            }

            if (savedFixedPiece.pieceData == fixedPlacement.pieceData)
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Fixed piece saved with normal PieceData: {savedFixedPiece.pieceData.name}");
                return new SaveValidationResult { isValid = false, reason = $"fixed piece saved with normal PieceData {savedFixedPiece.pieceData.name}" };
            }

            if (savedFixedPiece.pieceData.pieceType != fixedPlacement.pieceData.pieceType)
            {
                return new SaveValidationResult
                {
                    isValid = false,
                    reason = $"fixed piece type mismatch {fixedPlacement.pieceData.name} -> {savedFixedPiece.pieceData.name}"
                };
            }

            if (requiredNormalCounts.ContainsKey(savedFixedPiece.pieceData))
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Fixed piece saved with normal PieceData: {savedFixedPiece.pieceData.name}");
                return new SaveValidationResult { isValid = false, reason = $"fixed piece mixed into pieceStocks {savedFixedPiece.pieceData.name}" };
            }

            if (!boardActiveCells.Contains(fixedPlacement.position))
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Fixed piece position mismatch pos={fixedPlacement.position}");
                return new SaveValidationResult { isValid = false, reason = $"fixed piece on inactive cell {fixedPlacement.position}" };
            }
        }

        foreach (StagePiecePlacement normalPlacement in candidate.normalPlacements)
        {
            if (!boardActiveCells.Contains(normalPlacement.position))
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Piece on inactive cell pos={normalPlacement.position}");
                return new SaveValidationResult { isValid = false, reason = $"normal piece on inactive cell {normalPlacement.position}" };
            }
        }

        Dictionary<Vector2Int, int> recomputedCoverage = StageCoverageUtility.CalculateCoverCounts(
            candidate.GetAllPlacements(),
            boardActiveCells
        );
        ValidationResult knownSolutionValidation = ValidateKnownSolution(
            boardActiveCells,
            boardExactCells,
            recomputedCoverage,
            candidate.GetAllPlacements()
        );

        if (!knownSolutionValidation.isValid)
        {
            return new SaveValidationResult
            {
                isValid = false,
                reason = knownSolutionValidation.reason,
                knownSolutionValidation = knownSolutionValidation
            };
        }

        if (candidate.solverResult == null
            || candidate.solverResult.solutionCount == 0
            || candidate.solverResult.hitNodeLimit
            || candidate.solverResult.hitSolutionLimit)
        {
            return new SaveValidationResult
            {
                isValid = false,
                reason = "solver result is not acceptable",
                knownSolutionValidation = knownSolutionValidation
            };
        }

        return new SaveValidationResult
        {
            isValid = true,
            reason = "ok",
            knownSolutionValidation = knownSolutionValidation
        };
    }

    private void LogSaveValidation(string stageName, ExactFixedCandidate candidate, SaveValidationResult validation)
    {
        int totalCells = candidate.width * candidate.height;
        int removedCells = totalCells - candidate.activeCells.Count;
        string knownSolutionText = validation.knownSolutionValidation != null
            ? validation.knownSolutionValidation.isValid.ToString()
            : validation.isValid.ToString();
        string knownSolutionReason = validation.knownSolutionValidation != null
            ? validation.knownSolutionValidation.reason
            : validation.reason;

        Debug.Log($"[EXACT FIXED GEN VALIDATE] stage={stageName}");
        Debug.Log($"[EXACT FIXED GEN VALIDATE] knownSolution={knownSolutionText} reason={knownSolutionReason}");
        Debug.Log($"[EXACT FIXED GEN VALIDATE] normalPieces={candidate.normalPlacements.Count} fixedPieces={candidate.fixedPlacements.Count}");
        Debug.Log($"[EXACT FIXED GEN VALIDATE] activeCells={candidate.activeCells.Count} exactCells={candidate.exactCells.Count} removedCells={removedCells}");
        Debug.Log($"[EXACT FIXED GEN VALIDATE] solverSolutions={candidate.solverResult?.solutionCount ?? 0} status={candidate.solverResult?.status}");
    }

    private Dictionary<PieceData, int> CountPlacementsByPieceData(List<StagePiecePlacement> placements)
    {
        Dictionary<PieceData, int> counts = new Dictionary<PieceData, int>();

        foreach (StagePiecePlacement placement in placements)
        {
            if (placement == null || placement.pieceData == null)
            {
                continue;
            }

            if (counts.ContainsKey(placement.pieceData))
            {
                counts[placement.pieceData]++;
            }
            else
            {
                counts.Add(placement.pieceData, 1);
            }
        }

        return counts;
    }

    private SaveValidationResult ValidatePieceStocks(Dictionary<PieceData, int> requiredCounts, List<StagePieceStock> pieceStocks)
    {
        Dictionary<PieceData, int> actualCounts = new Dictionary<PieceData, int>();

        foreach (StagePieceStock stock in pieceStocks)
        {
            if (stock == null || stock.pieceData == null || stock.count <= 0)
            {
                continue;
            }

            if (actualCounts.ContainsKey(stock.pieceData))
            {
                actualCounts[stock.pieceData] += stock.count;
            }
            else
            {
                actualCounts.Add(stock.pieceData, stock.count);
            }
        }

        foreach (KeyValuePair<PieceData, int> required in requiredCounts)
        {
            actualCounts.TryGetValue(required.Key, out int actual);

            if (actual != required.Value)
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Missing pieceStock for {required.Key.name} required={required.Value} actual={actual}");
                return new SaveValidationResult
                {
                    isValid = false,
                    reason = $"pieceStock mismatch for {required.Key.name}"
                };
            }
        }

        foreach (KeyValuePair<PieceData, int> actual in actualCounts)
        {
            if (!requiredCounts.ContainsKey(actual.Key))
            {
                Debug.LogError($"[EXACT FIXED GEN ERROR] Extra pieceStock for {actual.Key.name} actual={actual.Value}");
                return new SaveValidationResult
                {
                    isValid = false,
                    reason = $"extra pieceStock for {actual.Key.name}"
                };
            }
        }

        return new SaveValidationResult { isValid = true, reason = "ok" };
    }

    private bool IsMappedFixedPieceData(PieceData pieceData)
    {
        foreach (FixedPieceDataMap map in fixedPieceDataMaps)
        {
            if (map != null && map.fixedPieceData == pieceData)
            {
                return true;
            }
        }

        return false;
    }

    private void LogNormalPieceCounts(Dictionary<PieceData, int> counts)
    {
        Debug.Log("[EXACT FIXED GEN] normalPieces:");

        foreach (KeyValuePair<PieceData, int> count in counts)
        {
            Debug.Log($"- {count.Key.name} count={count.Value}");
        }
    }

    private void LogPieceStocks(List<StagePieceStock> pieceStocks)
    {
        Debug.Log("[EXACT FIXED GEN] pieceStocks:");

        foreach (StagePieceStock stock in pieceStocks)
        {
            if (stock != null && stock.pieceData != null)
            {
                Debug.Log($"- {stock.pieceData.name} count={stock.count}");
            }
        }
    }

    private void LogFixedPieceSaves(List<FixedPieceSaveLog> saveLogs)
    {
        Debug.Log("[EXACT FIXED GEN] fixedPieces:");

        foreach (FixedPieceSaveLog saveLog in saveLogs)
        {
            string normalName = saveLog.generatedNormalPieceData != null ? saveLog.generatedNormalPieceData.name : "null";
            string fixedName = saveLog.savedFixedPieceData != null ? saveLog.savedFixedPieceData.name : "null";
            Debug.Log($"- generatedNormal={normalName} savedFixed={fixedName} pos={saveLog.position}");
        }
    }

    private bool FixedPiecesAffectExactCells(ExactFixedCandidate candidate)
    {
        foreach (StagePiecePlacement fixedPlacement in candidate.fixedPlacements)
        {
            foreach (Vector2Int coveredPosition in StageCoverageUtility.GetCoveredPositions(
                fixedPlacement.pieceData.pieceType,
                fixedPlacement.position,
                candidate.activeCells
            ))
            {
                if (candidate.exactCells.ContainsKey(coveredPosition))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private int CountSingleCellIslands(HashSet<Vector2Int> activeCells)
    {
        int count = 0;

        foreach (Vector2Int position in activeCells)
        {
            bool hasNeighbor = activeCells.Contains(position + Vector2Int.up)
                || activeCells.Contains(position + Vector2Int.down)
                || activeCells.Contains(position + Vector2Int.left)
                || activeCells.Contains(position + Vector2Int.right);

            if (!hasNeighbor)
            {
                count++;
            }
        }

        return count;
    }

    // Solver
    private static class ExactFixedSolver
    {
        public static ExactFixedSolverResult Solve(
            HashSet<Vector2Int> activeCells,
            Dictionary<Vector2Int, int> exactCells,
            List<StagePiecePlacement> normalPlacements,
            List<StagePiecePlacement> fixedPlacements,
            int maxSolutions,
            int maxSearchNodes
        )
        {
            ExactFixedSolverResult result = new ExactFixedSolverResult();
            List<PieceData> normalPieces = new List<PieceData>();

            foreach (StagePiecePlacement placement in normalPlacements)
            {
                normalPieces.Add(placement.pieceData);
            }

            SolverState state = new SolverState
            {
                activeCells = activeCells,
                activePositions = new List<Vector2Int>(activeCells),
                exactCells = exactCells,
                normalPieces = normalPieces,
                coverage = new Dictionary<Vector2Int, int>(),
                occupied = new HashSet<Vector2Int>(),
                result = result,
                maxSolutions = Mathf.Max(1, maxSolutions),
                maxSearchNodes = Mathf.Max(1, maxSearchNodes)
            };

            foreach (StagePiecePlacement fixedPlacement in fixedPlacements)
            {
                state.occupied.Add(fixedPlacement.position);
                AddCoverage(
                    state.coverage,
                    StageCoverageUtility.GetCoveredPositions(fixedPlacement.pieceData.pieceType, fixedPlacement.position, activeCells),
                    1
                );
            }

            Search(state, 0, 0);

            if (result.hitSolutionLimit)
            {
                result.status = SimpleStageSolver.Status.StoppedBySolutionLimit;
            }
            else if (result.hitNodeLimit)
            {
                result.status = SimpleStageSolver.Status.StoppedByNodeLimit;
            }
            else if (result.solutionCount > 0)
            {
                result.status = SimpleStageSolver.Status.SolvedCompletely;
            }
            else
            {
                result.status = SimpleStageSolver.Status.NoSolution;
                result.minPiecesUsed = 0;
            }

            return result;
        }

        private static void Search(SolverState state, int pieceIndex, int piecesUsed)
        {
            if (state.result.solutionCount >= state.maxSolutions)
            {
                state.result.hitSolutionLimit = true;
                return;
            }

            if (state.result.searchedNodes >= state.maxSearchNodes)
            {
                state.result.hitNodeLimit = true;
                return;
            }

            state.result.searchedNodes++;

            if (pieceIndex >= state.normalPieces.Count)
            {
                if (!IsClear(state.activeCells, state.exactCells, state.coverage))
                {
                    return;
                }

                state.result.solutionCount++;
                state.result.minPiecesUsed = Mathf.Min(state.result.minPiecesUsed, piecesUsed);

                if (state.result.solutionCount >= state.maxSolutions)
                {
                    state.result.hitSolutionLimit = true;
                }

                return;
            }

            Search(state, pieceIndex + 1, piecesUsed);

            if (state.result.hitSolutionLimit || state.result.hitNodeLimit)
            {
                return;
            }

            PieceData pieceData = state.normalPieces[pieceIndex];

            foreach (Vector2Int position in state.activePositions)
            {
                if (state.occupied.Contains(position))
                {
                    continue;
                }

                List<Vector2Int> coveredPositions = StageCoverageUtility.GetCoveredPositions(pieceData.pieceType, position, state.activeCells);

                if (coveredPositions.Count == 0)
                {
                    continue;
                }

                AddCoverage(state.coverage, coveredPositions, 1);
                state.occupied.Add(position);
                Search(state, pieceIndex + 1, piecesUsed + 1);
                state.occupied.Remove(position);
                AddCoverage(state.coverage, coveredPositions, -1);

                if (state.result.hitSolutionLimit || state.result.hitNodeLimit)
                {
                    return;
                }
            }
        }

        private static bool IsClear(
            HashSet<Vector2Int> activeCells,
            Dictionary<Vector2Int, int> exactCells,
            Dictionary<Vector2Int, int> coverage
        )
        {
            foreach (Vector2Int position in activeCells)
            {
                coverage.TryGetValue(position, out int coverCount);

                if (exactCells.TryGetValue(position, out int requiredCoverCount))
                {
                    if (coverCount != requiredCoverCount)
                    {
                        return false;
                    }
                }
                else if (coverCount < 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddCoverage(Dictionary<Vector2Int, int> coverage, List<Vector2Int> positions, int delta)
        {
            foreach (Vector2Int position in positions)
            {
                coverage.TryGetValue(position, out int count);
                count += delta;

                if (count <= 0)
                {
                    coverage.Remove(position);
                }
                else
                {
                    coverage[position] = count;
                }
            }
        }
    }

    // Exporter
    private StageData SaveCandidate(ExactFixedCandidate candidate, string folderPath, string stageName, out string assetPath)
    {
        StageData stageData = ScriptableObject.CreateInstance<StageData>();
        stageData.stageName = stageName;
        stageData.name = stageName;
        stageData.stageCategory = StageCategory.Advanced;
        stageData.boardText = candidate.boardText;
        stageData.pieceStocks = BuildPieceStocks(candidate.normalPlacements);
        stageData.fixedPieces = BuildFixedPieces(candidate.fixedPlacements, out _);
        stageData.helpPageToUnlock = HelpPageType.None;
        stageData.helpPageToShowEveryTime = HelpPageType.None;
        stageData.authorNote = BuildAuthorNote(candidate);

        assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{stageName}.asset");
        AssetDatabase.CreateAsset(stageData, assetPath);
        return stageData;
    }

    private string BuildAuthorNote(ExactFixedCandidate candidate)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("generated by ExactFixedStageGenerator");
        builder.AppendLine("normal piece placements:");

        foreach (StagePiecePlacement placement in candidate.normalPlacements)
        {
            builder.AppendLine($"{placement.pieceData.pieceType} at {placement.position}");
        }

        builder.AppendLine("fixed piece placements:");

        foreach (StagePiecePlacement placement in candidate.fixedPlacements)
        {
            PieceData fixedPieceData = ConvertToFixedPieceData(placement.pieceData);
            string fixedName = fixedPieceData != null ? fixedPieceData.name : "MissingFixedMap";
            builder.AppendLine($"{placement.pieceData.pieceType} at {placement.position} savedFixed={fixedName}");
        }

        builder.AppendLine($"exact cell count: {candidate.exactCells.Count}");
        builder.AppendLine($"removed cell count: {candidate.removedCellCount}");
        builder.AppendLine($"known solution validation result: {candidate.validation.isValid} {candidate.validation.reason}");

        if (candidate.solverResult != null)
        {
            builder.AppendLine("solver result:");
            builder.AppendLine($"status: {candidate.solverResult.status}");
            builder.AppendLine($"solutionCount: {candidate.solverResult.solutionCount}");
            builder.AppendLine($"minPiecesUsed: {candidate.solverResult.minPiecesUsed}");
            builder.AppendLine($"searchedNodes: {candidate.solverResult.searchedNodes}");
            builder.AppendLine($"hitSolutionLimit: {candidate.solverResult.hitSolutionLimit}");
            builder.AppendLine($"hitNodeLimit: {candidate.solverResult.hitNodeLimit}");
        }

        return builder.ToString();
    }

    private List<StagePieceStock> BuildPieceStocks(List<StagePiecePlacement> placements)
    {
        Dictionary<PieceData, int> counts = new Dictionary<PieceData, int>();

        foreach (StagePiecePlacement placement in placements)
        {
            if (counts.ContainsKey(placement.pieceData))
            {
                counts[placement.pieceData]++;
            }
            else
            {
                counts.Add(placement.pieceData, 1);
            }
        }

        List<StagePieceStock> stocks = new List<StagePieceStock>();

        foreach (KeyValuePair<PieceData, int> count in counts)
        {
            stocks.Add(new StagePieceStock
            {
                pieceData = count.Key,
                count = count.Value
            });
        }

        return stocks;
    }

    private List<FixedPieceData> BuildFixedPieces(List<StagePiecePlacement> placements, out List<FixedPieceSaveLog> saveLogs)
    {
        List<FixedPieceData> fixedPieces = new List<FixedPieceData>();
        saveLogs = new List<FixedPieceSaveLog>();

        foreach (StagePiecePlacement placement in placements)
        {
            PieceData fixedPieceData = ConvertToFixedPieceData(placement.pieceData);
            saveLogs.Add(new FixedPieceSaveLog
            {
                generatedNormalPieceData = placement.pieceData,
                savedFixedPieceData = fixedPieceData,
                position = placement.position
            });

            fixedPieces.Add(new FixedPieceData
            {
                pieceData = fixedPieceData,
                position = placement.position
            });
        }

        return fixedPieces;
    }

    private PieceData ConvertToFixedPieceData(PieceData normalPieceData)
    {
        if (normalPieceData == null)
        {
            Debug.LogError("[EXACT FIXED GEN ERROR] Fixed map not found for null");
            return null;
        }

        foreach (FixedPieceDataMap map in fixedPieceDataMaps)
        {
            if (map == null || map.normalPieceData == null || map.fixedPieceData == null)
            {
                continue;
            }

            if (map.normalPieceData == normalPieceData)
            {
                return map.fixedPieceData;
            }
        }

        Debug.LogError($"[EXACT FIXED GEN ERROR] Fixed map not found for {normalPieceData.name}");
        return null;
    }

    private string BuildBoardText(ExactFixedCandidate candidate)
    {
        StringBuilder builder = new StringBuilder();

        for (int y = candidate.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < candidate.width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (!candidate.activeCells.Contains(position))
                {
                    builder.Append('X');
                }
                else if (candidate.exactCells.TryGetValue(position, out int requiredCoverCount))
                {
                    builder.Append((char)('0' + Mathf.Clamp(requiredCoverCount, 1, 5)));
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

    private HashSet<Vector2Int> ParseActiveCellsFromBoardText(string boardText)
    {
        HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();
        string[] rows = GetBoardRows(boardText);

        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            string row = rows[rowIndex];
            int y = rows.Length - 1 - rowIndex;

            for (int x = 0; x < row.Length; x++)
            {
                char boardChar = row[x];

                if (IsActiveBoardChar(boardChar))
                {
                    activeCells.Add(new Vector2Int(x, y));
                }
            }
        }

        return activeCells;
    }

    private Dictionary<Vector2Int, int> ParseExactCellsFromBoardText(string boardText)
    {
        Dictionary<Vector2Int, int> exactCells = new Dictionary<Vector2Int, int>();
        string[] rows = GetBoardRows(boardText);

        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            string row = rows[rowIndex];
            int y = rows.Length - 1 - rowIndex;

            for (int x = 0; x < row.Length; x++)
            {
                char boardChar = row[x];

                if (boardChar >= '1' && boardChar <= '5')
                {
                    exactCells[new Vector2Int(x, y)] = boardChar - '0';
                }
            }
        }

        return exactCells;
    }

    private string[] GetBoardRows(string boardText)
    {
        if (string.IsNullOrEmpty(boardText))
        {
            return Array.Empty<string>();
        }

        return boardText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    private bool IsActiveBoardChar(char boardChar)
    {
        return boardChar == 'Z'
            || boardChar == 'z'
            || boardChar == '#'
            || (boardChar >= '1' && boardChar <= '5');
    }

    private bool SetsEqual(HashSet<Vector2Int> left, HashSet<Vector2Int> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (Vector2Int position in left)
        {
            if (!right.Contains(position))
            {
                return false;
            }
        }

        return true;
    }

    private bool DictionariesEqual(Dictionary<Vector2Int, int> left, Dictionary<Vector2Int, int> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (KeyValuePair<Vector2Int, int> entry in left)
        {
            if (!right.TryGetValue(entry.Key, out int value) || value != entry.Value)
            {
                return false;
            }
        }

        return true;
    }

    private int CountExpandedStocks(List<StagePieceStock> pieceStocks)
    {
        int count = 0;

        foreach (StagePieceStock stock in pieceStocks)
        {
            if (stock != null && stock.pieceData != null && stock.count > 0)
            {
                count += stock.count;
            }
        }

        return count;
    }

    // Utilities and data
    private HashSet<Vector2Int> BuildFullBoard(int boardWidth, int boardHeight)
    {
        HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                activeCells.Add(new Vector2Int(x, y));
            }
        }

        return activeCells;
    }

    private Vector2Int GetUnusedPosition(int boardWidth, int boardHeight, HashSet<Vector2Int> occupied, System.Random random)
    {
        int maxAttempts = boardWidth * boardHeight * 2;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2Int position = new Vector2Int(random.Next(boardWidth), random.Next(boardHeight));

            if (occupied.Add(position))
            {
                return position;
            }
        }

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (occupied.Add(position))
                {
                    return position;
                }
            }
        }

        throw new InvalidOperationException("No unused board position is available.");
    }

    private int RandomRangeInclusive(System.Random random, int min, int max)
    {
        return random.Next(min, Mathf.Max(min, max) + 1);
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

    private void LoadAllPieceDataAssets()
    {
        candidatePieces.Clear();
        string[] guids = AssetDatabase.FindAssets("t:PieceData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PieceData pieceData = AssetDatabase.LoadAssetAtPath<PieceData>(path);

            if (pieceData != null && !candidatePieces.Contains(pieceData))
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

    private class ExactFixedSettings
    {
        public int width;
        public int height;
        public int normalPieceCount;
        public int minFixedPieceCount;
        public int maxFixedPieceCount;
        public int overlapExactCellCount;
        public int minSingleCoverExactCellCount;
        public int maxSingleCoverExactCellCount;
        public int maxRemoveSingleCoverCells;
        public int maxSolutions;
        public int maxAcceptedSolutions;
        public int maxSearchNodes;
        public int seed;
        public List<PieceData> candidatePieces;
    }

    private class ExactFixedCandidate
    {
        public int width;
        public int height;
        public HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();
        public Dictionary<Vector2Int, int> coverCounts = new Dictionary<Vector2Int, int>();
        public Dictionary<Vector2Int, int> exactCells = new Dictionary<Vector2Int, int>();
        public List<StagePiecePlacement> normalPlacements = new List<StagePiecePlacement>();
        public List<StagePiecePlacement> fixedPlacements = new List<StagePiecePlacement>();
        public string boardText;
        public int overlapExactCount;
        public int singleExactCount;
        public int removedCellCount;
        public ValidationResult validation;
        public ExactFixedSolverResult solverResult;

        public List<StagePiecePlacement> GetAllPlacements()
        {
            List<StagePiecePlacement> placements = new List<StagePiecePlacement>(normalPlacements);
            placements.AddRange(fixedPlacements);
            return placements;
        }

        public HashSet<Vector2Int> GetOccupiedPositions()
        {
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

            foreach (StagePiecePlacement placement in normalPlacements)
            {
                occupied.Add(placement.position);
            }

            foreach (StagePiecePlacement placement in fixedPlacements)
            {
                occupied.Add(placement.position);
            }

            return occupied;
        }
    }

    private class ValidationResult
    {
        public bool isValid;
        public string reason;
    }

    private class SaveValidationResult
    {
        public bool isValid;
        public string reason;
        public ValidationResult knownSolutionValidation;
    }

    [Serializable]
    private class FixedPieceDataMap
    {
        public PieceData normalPieceData;
        public PieceData fixedPieceData;
    }

    private class FixedPieceSaveLog
    {
        public PieceData generatedNormalPieceData;
        public PieceData savedFixedPieceData;
        public Vector2Int position;
    }

    private class ExactFixedSolverResult
    {
        public int solutionCount;
        public int minPiecesUsed = int.MaxValue;
        public int searchedNodes;
        public bool hitSolutionLimit;
        public bool hitNodeLimit;
        public SimpleStageSolver.Status status = SimpleStageSolver.Status.NoSolution;
    }

    private class SolverState
    {
        public HashSet<Vector2Int> activeCells;
        public List<Vector2Int> activePositions;
        public Dictionary<Vector2Int, int> exactCells;
        public List<PieceData> normalPieces;
        public Dictionary<Vector2Int, int> coverage;
        public HashSet<Vector2Int> occupied;
        public ExactFixedSolverResult result;
        public int maxSolutions;
        public int maxSearchNodes;
    }
}
