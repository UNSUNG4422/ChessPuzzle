using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class BoardGenerator : MonoBehaviour
{
    [TextArea(5, 15)]
    [SerializeField] private string boardText;
    [SerializeField] private string[] boardRows;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject whiteCellPrefab;
    [SerializeField] private GameObject blackCellPrefab;
    [SerializeField] private Transform boardRoot;
    [SerializeField] private float cellSpacing = 1.1f;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private Color whiteCellColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Color blackCellColor = new Color(0.82f, 0.82f, 0.82f, 1f);

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateBoard();
        }
    }

    public void GenerateBoard()
    {
        List<CellView> generatedCells = new List<CellView>();
        string[] rows = GetBoardRows();

        if (cellPrefab == null && whiteCellPrefab == null && blackCellPrefab == null)
        {
            Debug.LogWarning("BoardGenerator has no cell prefab assigned.");
            return;
        }

        if (rows.Length == 0)
        {
            Debug.LogWarning("BoardGenerator board data is empty.");
            return;
        }

        Transform root = boardRoot != null ? boardRoot : transform;
        ClearBoard();

        List<Vector2Int> activePositions = GetActivePositions(rows);
        if (activePositions.Count == 0)
        {
            Debug.LogWarning("BoardGenerator board data does not contain any active cells.");
            return;
        }

        Vector3 centerOffset = CalculateCenterOffset(activePositions);

        foreach (Vector2Int gridPosition in activePositions)
        {
            GameObject prefab = GetCellPrefab(gridPosition);

            if (prefab == null)
            {
                Debug.LogWarning($"No prefab could be resolved for Cell {gridPosition}.");
                continue;
            }

            Vector3 localPosition = new Vector3(
                gridPosition.x * cellSpacing,
                gridPosition.y * cellSpacing,
                0f
            );
            Vector3 worldPosition = root.position + localPosition - centerOffset;
            GameObject cellObject = InstantiateCellPrefab(prefab, worldPosition, root);
            cellObject.name = $"Cell_{gridPosition.x}_{gridPosition.y}";

            CellView cellView = cellObject.GetComponent<CellView>();
            if (cellView == null)
            {
                Debug.LogWarning($"{cellObject.name} does not have a CellView component.");
                continue;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(cellView, "Configure Board Cell");
            }
#endif

            cellView.GridPosition = gridPosition;
            cellView.SetCellColor(GetCellColor(gridPosition));
            ApplyCoverRequirement(cellView, GetBoardCharacter(rows, gridPosition));
            cellView.SetActiveState(true);
            cellView.SetCovered(false);
            cellView.SetPiece(false);
            generatedCells.Add(cellView);

            MarkObjectDirty(cellObject);
            MarkObjectDirty(cellView);
        }

        PuzzleManager puzzleManager = GetPuzzleManager();
        if (puzzleManager != null)
        {
            puzzleManager.SetCells(generatedCells);
            MarkObjectDirty(puzzleManager);
        }
        else
        {
            Debug.LogWarning("PuzzleManager was not found in the scene.");
        }

        MarkObjectDirty(this);
        MarkSceneDirty();
    }

    public void LoadBoardText(string newBoardText)
    {
        boardText = newBoardText ?? string.Empty;
        MarkObjectDirty(this);
    }

    public void LoadStage(StageData stageData)
    {
        if (stageData == null)
        {
            Debug.LogWarning("Cannot load a null StageData.");
            return;
        }

        LoadBoardText(stageData.boardText);
        GenerateBoard();
    }

    public bool TryGetBoardBounds(out Bounds bounds)
    {
        Transform root = boardRoot != null ? boardRoot : transform;
        CellView[] cells = root.GetComponentsInChildren<CellView>();
        bool hasBounds = false;
        bounds = default;

        foreach (CellView cell in cells)
        {
            if (cell == null || !cell.IsActive)
            {
                continue;
            }

            Renderer renderer = cell.GetComponentInChildren<Renderer>();
            Bounds cellBounds = renderer != null
                ? renderer.bounds
                : new Bounds(cell.transform.position, Vector3.one * cellSpacing);

            if (!hasBounds)
            {
                bounds = cellBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(cellBounds);
            }
        }

        return hasBounds;
    }

    public void ClearBoard()
    {
        Transform root = boardRoot != null ? boardRoot : transform;
        ClearBoardRoot(root);

        PuzzleManager puzzleManager = GetPuzzleManager();
        if (puzzleManager != null)
        {
            puzzleManager.SetCells(new List<CellView>());
            MarkObjectDirty(puzzleManager);
        }

        MarkObjectDirty(this);
        MarkSceneDirty();
    }

    private string[] GetBoardRows()
    {
        if (!string.IsNullOrWhiteSpace(boardText))
        {
            string normalizedText = boardText.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] splitRows = normalizedText.Split('\n');
            List<string> rows = new List<string>();

            foreach (string row in splitRows)
            {
                // Empty lines are ignored, so they do not create vertical gaps in the board.
                if (!string.IsNullOrWhiteSpace(row))
                {
                    rows.Add(row);
                }
            }

            return rows.ToArray();
        }

        return boardRows ?? new string[0];
    }

    private List<Vector2Int> GetActivePositions(string[] rows)
    {
        List<Vector2Int> activePositions = new List<Vector2Int>();

        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            string row = rows[rowIndex];

            if (string.IsNullOrEmpty(row))
            {
                continue;
            }

            int y = rows.Length - 1 - rowIndex;

            for (int x = 0; x < row.Length; x++)
            {
                if (ShouldGenerateCell(row[x]))
                {
                    activePositions.Add(new Vector2Int(x, y));
                }
            }
        }

        return activePositions;
    }

    private bool ShouldGenerateCell(char boardCharacter)
    {
        return boardCharacter == 'Z'
            || boardCharacter == 'z'
            || boardCharacter == '#'
            || IsExactCoverCell(boardCharacter);
    }

    private char GetBoardCharacter(string[] rows, Vector2Int gridPosition)
    {
        int rowIndex = rows.Length - 1 - gridPosition.y;

        if (rowIndex < 0 || rowIndex >= rows.Length)
        {
            return '\0';
        }

        string row = rows[rowIndex];

        if (gridPosition.x < 0 || gridPosition.x >= row.Length)
        {
            return '\0';
        }

        return row[gridPosition.x];
    }

    private void ApplyCoverRequirement(CellView cellView, char boardCharacter)
    {
        if (IsExactCoverCell(boardCharacter))
        {
            cellView.SetExactCoverRequirement(true, boardCharacter - '0');
            return;
        }

        cellView.SetExactCoverRequirement(false, 1);
    }

    private bool IsExactCoverCell(char boardCharacter)
    {
        return boardCharacter >= '1' && boardCharacter <= '5';
    }

    private Vector3 CalculateCenterOffset(List<Vector2Int> activePositions)
    {
        int minX = activePositions[0].x;
        int maxX = activePositions[0].x;
        int minY = activePositions[0].y;
        int maxY = activePositions[0].y;

        foreach (Vector2Int position in activePositions)
        {
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minY = Mathf.Min(minY, position.y);
            maxY = Mathf.Max(maxY, position.y);
        }

        return new Vector3(
            (minX + maxX) * 0.5f * cellSpacing,
            (minY + maxY) * 0.5f * cellSpacing,
            0f
        );
    }

    private GameObject GetCellPrefab(Vector2Int gridPosition)
    {
        bool useWhiteCell = (gridPosition.x + gridPosition.y) % 2 == 0;
        GameObject selectedPrefab = useWhiteCell ? whiteCellPrefab : blackCellPrefab;

        if (selectedPrefab != null)
        {
            return selectedPrefab;
        }

        if (cellPrefab != null)
        {
            Debug.LogWarning($"{(useWhiteCell ? "whiteCellPrefab" : "blackCellPrefab")} is not assigned. Falling back to cellPrefab.");
            return cellPrefab;
        }

        Debug.LogWarning($"{(useWhiteCell ? "whiteCellPrefab" : "blackCellPrefab")} is not assigned, and cellPrefab is also missing.");
        return null;
    }

    private Color GetCellColor(Vector2Int gridPosition)
    {
        return (gridPosition.x + gridPosition.y) % 2 == 0
            ? whiteCellColor
            : blackCellColor;
    }

    private GameObject InstantiateCellPrefab(GameObject prefab, Vector3 position, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject instance = null;

            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            }

            if (instance == null)
            {
                instance = Instantiate(prefab, parent);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Generate Board Cell");
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.identity;
            MarkObjectDirty(instance);
            return instance;
        }
#endif

        return Instantiate(prefab, position, Quaternion.identity, parent);
    }

    private void ClearBoardRoot(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(child);
            }
            else
#endif
            {
                Destroy(child);
            }
        }
    }

    private PuzzleManager GetPuzzleManager()
    {
        if (PuzzleManager.Instance != null)
        {
            return PuzzleManager.Instance;
        }

        return FindFirstObjectByType<PuzzleManager>();
    }

    private void MarkObjectDirty(Object targetObject)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && targetObject != null)
        {
            EditorUtility.SetDirty(targetObject);
        }
#endif
    }

    private void MarkSceneDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}
