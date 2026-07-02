using UnityEngine;

[CreateAssetMenu(fileName = "PieceData", menuName = "Chess Puzzle/Piece Data")]
public class PieceData : ScriptableObject
{
    public PieceType pieceType;
    public string displayName;
    public Sprite icon;
}
