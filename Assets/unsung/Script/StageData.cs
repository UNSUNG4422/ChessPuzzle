using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Stage Data")]
public class StageData : ScriptableObject
{
    public string stageName;

    [TextArea(5, 15)]
    public string boardText;

    public StageCategory stageCategory = StageCategory.Standard;
    public List<StagePieceStock> pieceStocks = new List<StagePieceStock>();
    public List<FixedPieceData> fixedPieces = new List<FixedPieceData>();
    public HelpPageType helpPageToUnlock = HelpPageType.None;
    public bool forceShowHelpOnFirstUnlock = true;
    public HelpPageType helpPageToShowEveryTime = HelpPageType.None;

    [TextArea(3, 10)]
    public string authorNote;
}

public enum HelpPageType
{
    None,
    HelpA_Basic,
    HelpB_ExactCover,
    HelpC_FixedPieces
}

public enum StageCategory
{
    Tutorial,
    Standard,
    ExactCover,
    FixedPiece,
    Advanced
}

[System.Serializable]
public class FixedPieceData
{
    public PieceData pieceData;
    public Vector2Int position;
}
