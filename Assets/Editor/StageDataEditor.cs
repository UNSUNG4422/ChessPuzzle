using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageData))]
public class StageDataEditor : Editor
{
    private const float PreferredCellSize = 22f;
    private const float MinCellSize = 6f;
    private const float CellGap = 1f;

    private static readonly Color ActiveCellColor = new Color(0.82f, 0.88f, 0.94f, 1f);
    private static readonly Color InactiveCellColor = new Color(0.24f, 0.24f, 0.24f, 1f);
    private static readonly Color ExactCellColor = new Color(0.96f, 0.78f, 0.38f, 1f);
    private static readonly Color UnknownCellColor = new Color(0.95f, 0.36f, 0.36f, 1f);
    private static readonly Color BorderColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    private static readonly Color PieceTextColor = new Color(0.05f, 0.05f, 0.05f, 1f);

    private GUIStyle centeredLabelStyle;
    private GUIStyle pieceLabelStyle;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        DrawBoardPreview((StageData)target);
    }

    private void DrawBoardPreview(StageData stageData)
    {
        EditorGUILayout.LabelField("Board Preview", EditorStyles.boldLabel);

        string[] rows = GetBoardRows(stageData.boardText);
        if (rows.Length == 0)
        {
            EditorGUILayout.HelpBox("boardText is empty.", MessageType.Info);
            return;
        }

        int width = GetMaxRowLength(rows);
        int height = rows.Length;
        if (width == 0)
        {
            EditorGUILayout.HelpBox("boardText has no cells to preview.", MessageType.Info);
            return;
        }

        float availableWidth = Mathf.Max(1f, EditorGUIUtility.currentViewWidth - 40f);
        float cellSize = Mathf.Min(PreferredCellSize, availableWidth / width);
        cellSize = Mathf.Max(MinCellSize, cellSize);
        float boardWidth = width * cellSize;
        float boardHeight = height * cellSize;

        Rect boardRect = GUILayoutUtility.GetRect(boardWidth, boardHeight, GUILayout.ExpandWidth(false));
        Dictionary<Vector2Int, string> fixedPieceLabels = BuildFixedPieceLabels(stageData.fixedPieces);

        int unknownCount = 0;
        int outOfBoundsFixedPieceCount = 0;

        for (int rowIndex = 0; rowIndex < height; rowIndex++)
        {
            string row = rows[rowIndex];
            int y = height - 1 - rowIndex;

            for (int x = 0; x < width; x++)
            {
                char boardChar = x < row.Length ? row[x] : 'X';
                Vector2Int position = new Vector2Int(x, y);
                Rect cellRect = new Rect(
                    boardRect.x + x * cellSize,
                    boardRect.y + rowIndex * cellSize,
                    cellSize,
                    cellSize
                );

                DrawCell(cellRect, boardChar);

                if (IsUnknownCell(boardChar))
                {
                    unknownCount++;
                }

                string label = GetCellLabel(boardChar);
                if (fixedPieceLabels.TryGetValue(position, out string pieceLabel))
                {
                    label = pieceLabel;
                }

                if (!string.IsNullOrEmpty(label))
                {
                    DrawCellLabel(cellRect, label, fixedPieceLabels.ContainsKey(position), cellSize);
                }
            }
        }

        foreach (Vector2Int fixedPiecePosition in fixedPieceLabels.Keys)
        {
            if (fixedPiecePosition.x < 0
                || fixedPiecePosition.x >= width
                || fixedPiecePosition.y < 0
                || fixedPiecePosition.y >= height)
            {
                outOfBoundsFixedPieceCount++;
            }
        }

        EditorGUILayout.LabelField($"{width} x {height}", EditorStyles.miniLabel);

        if (unknownCount > 0)
        {
            EditorGUILayout.HelpBox($"{unknownCount} unknown board character(s) found.", MessageType.Warning);
        }

        if (outOfBoundsFixedPieceCount > 0)
        {
            EditorGUILayout.HelpBox($"{outOfBoundsFixedPieceCount} fixed piece(s) are outside the preview bounds.", MessageType.Warning);
        }
    }

    private void DrawCell(Rect cellRect, char boardChar)
    {
        EditorGUI.DrawRect(cellRect, BorderColor);

        Rect innerRect = new Rect(
            cellRect.x + CellGap,
            cellRect.y + CellGap,
            Mathf.Max(1f, cellRect.width - CellGap * 2f),
            Mathf.Max(1f, cellRect.height - CellGap * 2f)
        );

        EditorGUI.DrawRect(innerRect, GetCellColor(boardChar));
    }

    private void DrawCellLabel(Rect cellRect, string label, bool isPieceLabel, float cellSize)
    {
        EnsureStyles();

        GUIStyle style = isPieceLabel ? pieceLabelStyle : centeredLabelStyle;
        style.fontSize = Mathf.Clamp(Mathf.RoundToInt(cellSize * (isPieceLabel ? 0.62f : 0.55f)), 7, 13);
        GUI.Label(cellRect, label, style);
    }

    private void EnsureStyles()
    {
        if (centeredLabelStyle == null)
        {
            centeredLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black }
            };
        }

        if (pieceLabelStyle == null)
        {
            pieceLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = PieceTextColor }
            };
        }
    }

    private static string[] GetBoardRows(string boardText)
    {
        if (string.IsNullOrEmpty(boardText))
        {
            return new string[0];
        }

        string normalizedText = boardText.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
        if (string.IsNullOrEmpty(normalizedText))
        {
            return new string[0];
        }

        return normalizedText.Split('\n');
    }

    private static int GetMaxRowLength(string[] rows)
    {
        int width = 0;

        foreach (string row in rows)
        {
            width = Mathf.Max(width, row.Length);
        }

        return width;
    }

    private static Dictionary<Vector2Int, string> BuildFixedPieceLabels(List<FixedPieceData> fixedPieces)
    {
        Dictionary<Vector2Int, string> labels = new Dictionary<Vector2Int, string>();

        if (fixedPieces == null)
        {
            return labels;
        }

        foreach (FixedPieceData fixedPiece in fixedPieces)
        {
            if (fixedPiece == null || fixedPiece.pieceData == null)
            {
                continue;
            }

            labels[fixedPiece.position] = GetPieceLabel(fixedPiece.pieceData.pieceType);
        }

        return labels;
    }

    private static Color GetCellColor(char boardChar)
    {
        if (IsActiveCell(boardChar))
        {
            return ActiveCellColor;
        }

        if (IsInactiveCell(boardChar))
        {
            return InactiveCellColor;
        }

        if (IsExactCell(boardChar))
        {
            return ExactCellColor;
        }

        return UnknownCellColor;
    }

    private static string GetCellLabel(char boardChar)
    {
        return IsExactCell(boardChar) ? boardChar.ToString() : string.Empty;
    }

    private static bool IsActiveCell(char boardChar)
    {
        return boardChar == 'Z' || boardChar == 'z' || boardChar == '#';
    }

    private static bool IsInactiveCell(char boardChar)
    {
        return boardChar == 'X'
            || boardChar == 'x'
            || boardChar == '.'
            || boardChar == ' ';
    }

    private static bool IsExactCell(char boardChar)
    {
        return boardChar >= '1' && boardChar <= '5';
    }

    private static bool IsUnknownCell(char boardChar)
    {
        return !IsActiveCell(boardChar) && !IsInactiveCell(boardChar) && !IsExactCell(boardChar);
    }

    private static string GetPieceLabel(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return "P";
            case PieceType.Knight:
                return "N";
            case PieceType.Bishop:
                return "B";
            case PieceType.Rook:
                return "R";
            case PieceType.Queen:
                return "Q";
            case PieceType.King:
                return "K";
            default:
                return "?";
        }
    }
}
