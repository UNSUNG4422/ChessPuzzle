using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SimpleStageExporter
{
    public static StageData SaveCandidate(StageGenerationCandidate candidate, string folderPath, string stageName)
    {
        StageData stageData = ScriptableObject.CreateInstance<StageData>();
        stageData.stageName = stageName;
        stageData.name = stageName;
        stageData.stageCategory = StageCategory.Standard;
        stageData.boardText = candidate.boardText;
        stageData.pieceStocks = StandardStageGenerator.BuildPieceStocks(candidate.solutionPlacements);
        stageData.fixedPieces = new List<FixedPieceData>();
        stageData.helpPageToUnlock = HelpPageType.None;
        stageData.helpPageToShowEveryTime = HelpPageType.None;
        stageData.authorNote = BuildAuthorNote(candidate);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{stageName}.asset");
        AssetDatabase.CreateAsset(stageData, assetPath);
        return stageData;
    }

    private static string BuildAuthorNote(StageGenerationCandidate candidate)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Auto generated with Simple Stage Generator.");
        builder.AppendLine("Rules: Standard only, no exact cover, no fixed pieces.");
        builder.AppendLine($"Board Size: {candidate.width}x{candidate.height}");
        builder.AppendLine($"Active Cells: {candidate.activeCellCount}");
        builder.AppendLine($"Connected Components: {candidate.connectedComponentCount}");
        builder.AppendLine($"Single Cell Islands: {candidate.singleCellIslandCount}");

        if (candidate.solverResult != null)
        {
            builder.AppendLine("Solver:");
            builder.AppendLine($"Status: {candidate.solverResult.status}");
            builder.AppendLine($"Solution Count: {candidate.solverResult.solutionCount}");
            builder.AppendLine($"Min Pieces Used: {candidate.solverResult.minPiecesUsed}");
            builder.AppendLine($"Searched Nodes: {candidate.solverResult.searchedNodes}");
            builder.AppendLine($"Hit Solution Limit: {candidate.solverResult.hitSolutionLimit}");
            builder.AppendLine($"Hit Node Limit: {candidate.solverResult.hitNodeLimit}");
        }

        builder.AppendLine("Known solution placements:");

        foreach (StagePiecePlacement placement in candidate.solutionPlacements)
        {
            if (placement == null || placement.pieceData == null)
            {
                continue;
            }

            builder.AppendLine($"{placement.pieceData.pieceType} at {placement.position}");
        }

        return builder.ToString();
    }
}
