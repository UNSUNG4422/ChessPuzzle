using System.Collections;
using UnityEngine;

public class KingMover2D : MonoBehaviour
{
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Vector2 boardOrigin = Vector2.zero;
    [SerializeField] private Vector2Int boardSize = new Vector2Int(8, 8);
    [SerializeField] private float moveDuration = 0.12f;
    [SerializeField] private bool allowMouseClick = true;

    private Vector2Int boardPosition;
    private bool isMoving;

    private void Start()
    {
        boardPosition = WorldToBoard(transform.position);
        transform.position = BoardToWorld(boardPosition);
    }

    private void Update()
    {
        if (isMoving)
        {
            return;
        }

        Vector2Int direction = ReadKeyboardDirection();

        if (direction != Vector2Int.zero)
        {
            TryMove(direction);
            return;
        }

        if (allowMouseClick && Input.GetMouseButtonDown(0))
        {
            TryMoveToClickedSquare();
        }
    }

    private Vector2Int ReadKeyboardDirection()
    {
        int x = 0;
        int y = 0;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            x = -1;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            x = 1;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            y = -1;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            y = 1;
        }

        return new Vector2Int(x, y);
    }

    private void TryMoveToClickedSquare()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return;
        }

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int clickedPosition = WorldToBoard(mouseWorldPosition);
        Vector2Int direction = clickedPosition - boardPosition;

        if (Mathf.Abs(direction.x) <= 1 && Mathf.Abs(direction.y) <= 1)
        {
            TryMove(direction);
        }
    }

    private void TryMove(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        Vector2Int nextPosition = boardPosition + direction;

        if (!IsInsideBoard(nextPosition))
        {
            return;
        }

        boardPosition = nextPosition;
        StartCoroutine(MoveTo(BoardToWorld(boardPosition)));
    }

    private IEnumerator MoveTo(Vector3 targetPosition)
    {
        isMoving = true;

        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;
    }

    private bool IsInsideBoard(Vector2Int position)
    {
        return position.x >= 0
            && position.x < boardSize.x
            && position.y >= 0
            && position.y < boardSize.y;
    }

    private Vector2Int WorldToBoard(Vector3 worldPosition)
    {
        Vector2 localPosition = ((Vector2)worldPosition - boardOrigin) / tileSize;
        return new Vector2Int(Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.y));
    }

    private Vector3 BoardToWorld(Vector2Int position)
    {
        return new Vector3(
            boardOrigin.x + position.x * tileSize,
            boardOrigin.y + position.y * tileSize,
            transform.position.z
        );
    }
}
