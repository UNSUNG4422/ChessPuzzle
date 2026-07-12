using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class StageGenerationCandidate
{
    public string stageName;
    public int width;
    public int height;
    public string boardText;
    public HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();
    public List<StagePiecePlacement> solutionPlacements = new List<StagePiecePlacement>();
    public Dictionary<Vector2Int, int> knownSolutionCoverCounts = new Dictionary<Vector2Int, int>();
    public SimpleStageSolver.Result solverResult;
    public int connectedComponentCount;
    public int singleCellIslandCount;
    public int activeCellCount => activeCells != null ? activeCells.Count : 0;

    public void RebuildBoardText()
    {
        StringBuilder builder = new StringBuilder();

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                builder.Append(activeCells.Contains(new Vector2Int(x, y)) ? 'Z' : 'X');
            }

            if (y > 0)
            {
                builder.AppendLine();
            }
        }

        boardText = builder.ToString();
    }
}

public class StagePiecePlacement
{
    public PieceData pieceData;
    public Vector2Int position;
}
