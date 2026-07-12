using System.Collections.Generic;
using UnityEngine;

public static class SimpleStageSolver
{
    public enum Status
    {
        NoSolution,
        SolvedCompletely,
        StoppedBySolutionLimit,
        StoppedByNodeLimit
    }

    public class Result
    {
        public int solutionCount;
        public int minPiecesUsed = int.MaxValue;
        public int searchedNodes;
        public bool hitSolutionLimit;
        public bool hitNodeLimit;
        public Status status = Status.NoSolution;
    }

    private class SearchState
    {
        public HashSet<Vector2Int> activeCells;
        public List<Vector2Int> activePositions;
        public List<PieceData> pieces;
        public Dictionary<Vector2Int, int> coverage;
        public HashSet<Vector2Int> occupied;
        public Result result;
        public int maxSolutions;
        public int maxSearchNodes;
    }

    public static Result Solve(
        HashSet<Vector2Int> activeCells,
        List<StagePieceStock> pieceStocks,
        int maxSolutions,
        int maxSearchNodes
    )
    {
        Result result = new Result();

        if (activeCells == null || activeCells.Count == 0)
        {
            result.minPiecesUsed = 0;
            return result;
        }

        List<PieceData> pieces = ExpandPieceStocks(pieceStocks);
        SearchState state = new SearchState
        {
            activeCells = activeCells,
            activePositions = new List<Vector2Int>(activeCells),
            pieces = pieces,
            coverage = new Dictionary<Vector2Int, int>(),
            occupied = new HashSet<Vector2Int>(),
            result = result,
            maxSolutions = Mathf.Max(1, maxSolutions),
            maxSearchNodes = Mathf.Max(1, maxSearchNodes)
        };

        Search(state, 0, 0);

        if (result.hitSolutionLimit)
        {
            result.status = Status.StoppedBySolutionLimit;
        }
        else if (result.hitNodeLimit)
        {
            result.status = Status.StoppedByNodeLimit;
        }
        else if (result.solutionCount > 0)
        {
            result.status = Status.SolvedCompletely;
        }
        else
        {
            result.status = Status.NoSolution;
            result.minPiecesUsed = 0;
        }

        return result;
    }

    private static void Search(SearchState state, int pieceIndex, int piecesUsed)
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

        if (pieceIndex >= state.pieces.Count)
        {
            if (!IsClear(state.activeCells, state.coverage))
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

        PieceData pieceData = state.pieces[pieceIndex];

        foreach (Vector2Int position in state.activePositions)
        {
            if (state.occupied.Contains(position))
            {
                continue;
            }

            List<Vector2Int> coveredPositions = StageCoverageUtility.GetCoveredPositions(
                pieceData.pieceType,
                position,
                state.activeCells
            );

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

    private static bool IsClear(HashSet<Vector2Int> activeCells, Dictionary<Vector2Int, int> coverage)
    {
        foreach (Vector2Int position in activeCells)
        {
            if (!coverage.TryGetValue(position, out int count) || count < 1)
            {
                return false;
            }
        }

        return true;
    }

    public static List<PieceData> ExpandPieceStocks(List<StagePieceStock> pieceStocks)
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
