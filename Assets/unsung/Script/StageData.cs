using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Stage Data")]
public class StageData : ScriptableObject
{
    public string stageName;

    [TextArea(5, 15)]
    public string boardText;

    public List<StagePieceStock> pieceStocks = new List<StagePieceStock>();

    [TextArea(3, 10)]
    public string authorNote;
}
