using System.Collections.Generic;
using UnityEngine;

public static class StageCoverageUtility
{
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

    public static List<Vector2Int> GetCoveredPositions(PieceType pieceType, Vector2Int origin, HashSet<Vector2Int> activeCells)
    {
        List<Vector2Int> results = new List<Vector2Int>();

        if (activeCells == null || !activeCells.Contains(origin))
        {
            return results;
        }

        AddUnique(origin, results);

        switch (pieceType)
        {
            case PieceType.Pawn:
                TryAddPosition(origin + Vector2Int.up, activeCells, results);
                break;
            case PieceType.Knight:
                AddStepMoves(origin, KnightDirections, activeCells, results);
                break;
            case PieceType.Bishop:
                AddLineMoves(origin, DiagonalDirections, activeCells, results);
                break;
            case PieceType.Rook:
                AddLineMoves(origin, OrthogonalDirections, activeCells, results);
                break;
            case PieceType.Queen:
                AddLineMoves(origin, OrthogonalDirections, activeCells, results);
                AddLineMoves(origin, DiagonalDirections, activeCells, results);
                break;
            case PieceType.King:
                AddStepMoves(origin, OrthogonalDirections, activeCells, results);
                AddStepMoves(origin, DiagonalDirections, activeCells, results);
                break;
        }

        return results;
    }

    public static List<Vector2Int> GetCoveredPositionsOnFullBoard(PieceType pieceType, Vector2Int origin, int width, int height)
    {
        HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                activeCells.Add(new Vector2Int(x, y));
            }
        }

        return GetCoveredPositions(pieceType, origin, activeCells);
    }

    public static Dictionary<Vector2Int, int> CalculateCoverCounts(IEnumerable<StagePiecePlacement> placements, HashSet<Vector2Int> activeCells)
    {
        Dictionary<Vector2Int, int> coverCounts = new Dictionary<Vector2Int, int>();

        if (placements == null)
        {
            return coverCounts;
        }

        foreach (StagePiecePlacement placement in placements)
        {
            if (placement == null || placement.pieceData == null)
            {
                continue;
            }

            foreach (Vector2Int position in GetCoveredPositions(placement.pieceData.pieceType, placement.position, activeCells))
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

    private static void AddStepMoves(Vector2Int origin, Vector2Int[] directions, HashSet<Vector2Int> activeCells, List<Vector2Int> results)
    {
        foreach (Vector2Int direction in directions)
        {
            TryAddPosition(origin + direction, activeCells, results);
        }
    }

    private static void AddLineMoves(Vector2Int origin, Vector2Int[] directions, HashSet<Vector2Int> activeCells, List<Vector2Int> results)
    {
        foreach (Vector2Int direction in directions)
        {
            Vector2Int position = origin + direction;

            while (activeCells.Contains(position))
            {
                AddUnique(position, results);
                position += direction;
            }
        }
    }

    private static void TryAddPosition(Vector2Int position, HashSet<Vector2Int> activeCells, List<Vector2Int> results)
    {
        if (activeCells.Contains(position))
        {
            AddUnique(position, results);
        }
    }

    private static void AddUnique(Vector2Int position, List<Vector2Int> results)
    {
        if (!results.Contains(position))
        {
            results.Add(position);
        }
    }
}
