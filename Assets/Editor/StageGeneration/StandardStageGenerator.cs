using System;
using System.Collections.Generic;
using UnityEngine;

public class StandardStageGenerator
{
    public class Settings
    {
        public int width;
        public int height;
        public int pieceCount;
        public int candidateCount;
        public int desiredOutputCount;
        public int maxSolutions;
        public int maxAcceptedSolutions;
        public int maxSearchNodes;
        public int minActiveCells;
        public int maxActiveCells;
        public int maxConnectedComponents = 2;
        public int maxSingleCellIslands = 0;
        public int seed;
        public List<PieceData> candidatePieces = new List<PieceData>();
    }

    public List<StageGenerationCandidate> Generate(Settings settings)
    {
        ValidateSettings(settings);

        List<StageGenerationCandidate> accepted = new List<StageGenerationCandidate>();
        System.Random random = new System.Random(settings.seed);

        for (int i = 0; i < settings.candidateCount; i++)
        {
            StageGenerationCandidate candidate = CreateCandidate(settings, random, i + 1);

            if (!HasEveryPieceUniqueCoveredCell(candidate))
            {
                continue;
            }

            if (candidate.activeCellCount < settings.minActiveCells || candidate.activeCellCount > settings.maxActiveCells)
            {
                continue;
            }

            CalculateComponentMetrics(candidate);

            if (candidate.connectedComponentCount > settings.maxConnectedComponents
                || candidate.singleCellIslandCount > settings.maxSingleCellIslands)
            {
                continue;
            }

            List<StagePieceStock> pieceStocks = BuildPieceStocks(candidate.solutionPlacements);
            SimpleStageSolver.Result solverResult = SimpleStageSolver.Solve(
                candidate.activeCells,
                pieceStocks,
                settings.maxSolutions,
                settings.maxSearchNodes
            );

            candidate.solverResult = solverResult;

            if (solverResult.solutionCount < 1
                || solverResult.solutionCount > settings.maxAcceptedSolutions
                || solverResult.minPiecesUsed != settings.pieceCount)
            {
                continue;
            }

            accepted.Add(candidate);

            if (accepted.Count >= settings.desiredOutputCount)
            {
                break;
            }
        }

        return accepted;
    }

    private StageGenerationCandidate CreateCandidate(Settings settings, System.Random random, int serialNumber)
    {
        StageGenerationCandidate candidate = new StageGenerationCandidate
        {
            width = settings.width,
            height = settings.height,
            stageName = $"SimpleStandard_{serialNumber:000}"
        };

        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

        for (int i = 0; i < settings.pieceCount; i++)
        {
            PieceData pieceData = settings.candidatePieces[random.Next(settings.candidatePieces.Count)];
            Vector2Int position = GetUnusedPosition(settings.width, settings.height, occupied, random);
            occupied.Add(position);

            candidate.solutionPlacements.Add(new StagePiecePlacement
            {
                pieceData = pieceData,
                position = position
            });
        }

        HashSet<Vector2Int> fullBoard = BuildFullBoard(settings.width, settings.height);

        foreach (StagePiecePlacement placement in candidate.solutionPlacements)
        {
            foreach (Vector2Int coveredPosition in StageCoverageUtility.GetCoveredPositions(
                placement.pieceData.pieceType,
                placement.position,
                fullBoard
            ))
            {
                candidate.activeCells.Add(coveredPosition);
            }
        }

        candidate.knownSolutionCoverCounts = StageCoverageUtility.CalculateCoverCounts(
            candidate.solutionPlacements,
            candidate.activeCells
        );
        candidate.RebuildBoardText();
        return candidate;
    }

    private bool HasEveryPieceUniqueCoveredCell(StageGenerationCandidate candidate)
    {
        foreach (StagePiecePlacement placement in candidate.solutionPlacements)
        {
            bool hasUniqueCell = false;
            List<Vector2Int> coveredPositions = StageCoverageUtility.GetCoveredPositions(
                placement.pieceData.pieceType,
                placement.position,
                candidate.activeCells
            );

            foreach (Vector2Int position in coveredPositions)
            {
                if (candidate.knownSolutionCoverCounts.TryGetValue(position, out int coverCount) && coverCount == 1)
                {
                    hasUniqueCell = true;
                    break;
                }
            }

            if (!hasUniqueCell)
            {
                return false;
            }
        }

        return true;
    }

    private void CalculateComponentMetrics(StageGenerationCandidate candidate)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        candidate.connectedComponentCount = 0;
        candidate.singleCellIslandCount = 0;

        foreach (Vector2Int start in candidate.activeCells)
        {
            if (visited.Contains(start))
            {
                continue;
            }

            int componentSize = FloodFillComponentSize(start, candidate.activeCells, visited);
            candidate.connectedComponentCount++;

            if (componentSize == 1)
            {
                candidate.singleCellIslandCount++;
            }
        }
    }

    private int FloodFillComponentSize(Vector2Int start, HashSet<Vector2Int> activeCells, HashSet<Vector2Int> visited)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);
        int count = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            count++;

            TryEnqueue(current + Vector2Int.up, activeCells, visited, queue);
            TryEnqueue(current + Vector2Int.down, activeCells, visited, queue);
            TryEnqueue(current + Vector2Int.left, activeCells, visited, queue);
            TryEnqueue(current + Vector2Int.right, activeCells, visited, queue);
        }

        return count;
    }

    private void TryEnqueue(
        Vector2Int position,
        HashSet<Vector2Int> activeCells,
        HashSet<Vector2Int> visited,
        Queue<Vector2Int> queue
    )
    {
        if (activeCells.Contains(position) && visited.Add(position))
        {
            queue.Enqueue(position);
        }
    }

    public static List<StagePieceStock> BuildPieceStocks(List<StagePiecePlacement> placements)
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

    private HashSet<Vector2Int> BuildFullBoard(int width, int height)
    {
        HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                activeCells.Add(new Vector2Int(x, y));
            }
        }

        return activeCells;
    }

    private Vector2Int GetUnusedPosition(int width, int height, HashSet<Vector2Int> occupied, System.Random random)
    {
        int maxAttempts = width * height * 2;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2Int position = new Vector2Int(random.Next(width), random.Next(height));

            if (!occupied.Contains(position))
            {
                return position;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (!occupied.Contains(position))
                {
                    return position;
                }
            }
        }

        throw new InvalidOperationException("No unused board position is available.");
    }

    private void ValidateSettings(Settings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        settings.width = Mathf.Max(1, settings.width);
        settings.height = Mathf.Max(1, settings.height);
        settings.pieceCount = Mathf.Clamp(settings.pieceCount, 1, settings.width * settings.height);
        settings.candidateCount = Mathf.Max(1, settings.candidateCount);
        settings.desiredOutputCount = Mathf.Max(1, settings.desiredOutputCount);
        settings.maxSolutions = Mathf.Max(1, settings.maxSolutions);
        settings.maxAcceptedSolutions = Mathf.Max(1, settings.maxAcceptedSolutions);
        settings.maxSearchNodes = Mathf.Max(1, settings.maxSearchNodes);
        settings.minActiveCells = Mathf.Max(1, settings.minActiveCells);
        settings.maxActiveCells = Mathf.Max(settings.minActiveCells, settings.maxActiveCells);

        if (settings.candidatePieces == null || settings.candidatePieces.Count == 0)
        {
            throw new InvalidOperationException("Candidate PieceData List is empty.");
        }

        settings.candidatePieces.RemoveAll(pieceData => pieceData == null);

        if (settings.candidatePieces.Count == 0)
        {
            throw new InvalidOperationException("Candidate PieceData List has no valid PieceData.");
        }
    }
}
