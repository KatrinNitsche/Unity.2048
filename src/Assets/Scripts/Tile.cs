using UnityEngine;

public class Tile : MonoBehaviour
{
    public int tileValue;
    public bool mergedThisTurn;

    public Vector2 startingPosition;
    public Vector2 moveToPosition;
    
    public bool destroyMe = false;

    public Transform colliderTile;

    public bool willMergeWidthCollidingTile;
}
