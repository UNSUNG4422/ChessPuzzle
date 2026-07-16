using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SimpleStageGeneratorWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/unsung/StageData";

    [SerializeField] private DefaultAsset outputFolder;
    [SerializeField] private string stageNamePrefix = "SimpleStandard";
    [SerializeField] private int width = 7;
    [SerializeField] private int height = 7;
    [SerializeField] private int pieceCount = 4;
    [SerializeField] private int candidateCount = 100;
    [SerializeField] private int desiredOutputCount = 5;
    [SerializeField] private int maxSolutions = 50;
    [SerializeField] private int maxAcceptedSolutions = 10;
    [SerializeField] private int maxSearchNodes = 100000;
    [SerializeField] private int minActiveCells = 8;
    [SerializeField] private int maxActiveCells = 32;
    [SerializeField] private int randomSeed = 12345;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private List<PieceData> candidatePieces = new List<PieceData>();

    private Vector2 scrollPosition;

    [MenuItem("Tools/ChessPuzzle/Simple Stage Generator")]
    public static void Open()
    {
        GetWindow<SimpleStageGeneratorWindow>("Simple Stage Generator");
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

        try
        {
            EditorGUILayout.LabelField("Simple Standard Generator", EditorStyles.boldLabel);
            outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);
            stageNamePrefix = EditorGUILayout.TextField("Stage Name Prefix", stageNamePrefix);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
            width = Mathf.Max(1, EditorGUILayout.IntField("Width", width));
            height = Mathf.Max(1, EditorGUILayout.IntField("Height", height));
            pieceCount = Mathf.Clamp(EditorGUILayout.IntField("Piece Count", pieceCount), 1, width * height);
            candidateCount = Mathf.Max(1, EditorGUILayout.IntField("Candidate Count", candidateCount));
            desiredOutputCount = Mathf.Max(1, EditorGUILayout.IntField("Desired Output Count", desiredOutputCount));
            randomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);
            useRandomSeed = EditorGUILayout.Toggle("Use Random Seed", useRandomSeed);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Solver", EditorStyles.boldLabel);
            maxSolutions = Mathf.Max(1, EditorGUILayout.IntField("Max Solutions", maxSolutions));
            maxAcceptedSolutions = Mathf.Max(1, EditorGUILayout.IntField("Max Accepted Solutions", maxAcceptedSolutions));
            maxSearchNodes = Mathf.Max(1, EditorGUILayout.IntField("Max Search Nodes", maxSearchNodes));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quality Filters", EditorStyles.boldLabel);
            minActiveCells = Mathf.Max(1, EditorGUILayout.IntField("Min Active Cells", minActiveCells));
            maxActiveCells = Mathf.Max(minActiveCells, EditorGUILayout.IntField("Max Active Cells", maxActiveCells));

            EditorGUILayout.Space();
            DrawCandidatePieceList();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!CanGenerate()))
            {
                if (GUILayout.Button("Generate", GUILayout.Height(32f)))
                {
                    Generate();
                }
            }

            EditorGUILayout.HelpBox(
                "Generates Standard stages only: Z/X boardText, no exact cover, no fixed pieces, StageCategory.Standard.",
                MessageType.Info
            );
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawCandidatePieceList()
    {
        EditorGUILayout.LabelField("Candidate PieceData List", EditorStyles.boldLabel);

        for (int i = 0; i < candidatePieces.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            try
            {
                candidatePieces[i] = (PieceData)EditorGUILayout.ObjectField(candidatePieces[i], typeof(PieceData), false);

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    candidatePieces.RemoveAt(i);
                    i--;
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
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
            EditorUtility.DisplayDialog("Simple Stage Generator", "Select an output folder under Assets.", "OK");
            return;
        }

        int seed = useRandomSeed ? Environment.TickCount : randomSeed;
        StandardStageGenerator.Settings settings = new StandardStageGenerator.Settings
        {
            width = width,
            height = height,
            pieceCount = pieceCount,
            candidateCount = candidateCount,
            desiredOutputCount = desiredOutputCount,
            maxSolutions = maxSolutions,
            maxAcceptedSolutions = maxAcceptedSolutions,
            maxSearchNodes = maxSearchNodes,
            minActiveCells = minActiveCells,
            maxActiveCells = maxActiveCells,
            seed = seed,
            candidatePieces = new List<PieceData>(candidatePieces)
        };

        try
        {
            StandardStageGenerator generator = new StandardStageGenerator();
            List<StageGenerationCandidate> candidates = generator.Generate(settings);
            List<StageData> savedStages = new List<StageData>();

            for (int i = 0; i < candidates.Count; i++)
            {
                string stageName = $"{stageNamePrefix}_{i + 1:000}";
                StageData stageData = SimpleStageExporter.SaveCandidate(candidates[i], folderPath, stageName);
                savedStages.Add(stageData);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Simple Stage Generator",
                $"Seed: {seed}\nAccepted: {candidates.Count}\nSaved: {savedStages.Count}",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Simple Stage Generator", exception.Message, "OK");
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
}
