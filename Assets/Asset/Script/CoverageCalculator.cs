using System.Collections.Generic;
using UnityEngine;

public static class CoverageCalculator
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

    public static List<Vector2Int> GetCoveredPositions(
        PieceType pieceType,
        Vector2Int origin,
        Dictionary<Vector2Int, CellView> cells
    )
    {
        List<Vector2Int> results = new List<Vector2Int>();

        if (cells == null)
        {
            Debug.LogWarning("CoverageCalculatorに渡されたcellsがnullです。");
            return results;
        }

        TryAddPosition(origin, cells, results);

        switch (pieceType)
        {
            case PieceType.Pawn:
                TryAddPosition(origin + Vector2Int.up, cells, results);
                break;
            case PieceType.Knight:
                AddStepMoves(origin, KnightDirections, cells, results);
                break;
            case PieceType.Bishop:
                AddLineMoves(origin, DiagonalDirections, cells, results);
                break;
            case PieceType.Rook:
                AddLineMoves(origin, OrthogonalDirections, cells, results);
                break;
            case PieceType.Queen:
                AddLineMoves(origin, OrthogonalDirections, cells, results);
                AddLineMoves(origin, DiagonalDirections, cells, results);
                break;
            case PieceType.King:
                AddStepMoves(origin, OrthogonalDirections, cells, results);
                AddStepMoves(origin, DiagonalDirections, cells, results);
                break;
        }

        return results;
    }

    private static void AddStepMoves(
        Vector2Int origin,
        Vector2Int[] directions,
        Dictionary<Vector2Int, CellView> cells,
        List<Vector2Int> results
    )
    {
        foreach (Vector2Int direction in directions)
        {
            TryAddPosition(origin + direction, cells, results);
        }
    }

    private static void AddLineMoves(
        Vector2Int origin,
        Vector2Int[] directions,
        Dictionary<Vector2Int, CellView> cells,
        List<Vector2Int> results
    )
    {
        foreach (Vector2Int direction in directions)
        {
            Vector2Int position = origin + direction;

            while (IsValidActiveCell(position, cells))
            {
                AddUnique(position, results);
                position += direction;
            }
        }
    }

    private static void TryAddPosition(
        Vector2Int position,
        Dictionary<Vector2Int, CellView> cells,
        List<Vector2Int> results
    )
    {
        if (!IsValidActiveCell(position, cells))
        {
            return;
        }

        AddUnique(position, results);
    }

    private static bool IsValidActiveCell(Vector2Int position, Dictionary<Vector2Int, CellView> cells)
    {
        return cells.TryGetValue(position, out CellView cell)
            && cell != null
            && cell.IsActive;
    }

    private static void AddUnique(Vector2Int position, List<Vector2Int> results)
    {
        if (!results.Contains(position))
        {
            results.Add(position);
        }
    }
}
