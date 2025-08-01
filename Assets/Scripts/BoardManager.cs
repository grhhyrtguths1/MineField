using System.Collections.Generic;
using IDC;
using UnityEditor;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int minesCount;
    [SerializeField] private float cellPadding = 0.1f;
    [SerializeField] private CellController cellControllerPrefab;

    private BoardData _boardData;
    private List<List<CellController>> _cells;
    private void Awake()
    {
        _boardData = new BoardData(width, height, minesCount);
        GenerateCells();
    }
    
    
    private void Start()
    {
        IDCUtils.IDC.AddClass(this);
    }

    private void GenerateCells()
    {
        _cells = new List<List<CellController>>(height);
        for (int y = 0; y < height; y++)
        {
            List<CellController> row = new List<CellController>(width);
            for (int x = 0; x < width; x++)
            {
                CellController cellController = CreateCellController(x, y);
                row.Add(cellController);
            }
            _cells.Add(row);
        }
    }

    private CellController CreateCellController(int x, int y)
    {
        float spacing = 1f + cellPadding;
        Vector3 position = new Vector3(x * spacing, -y * spacing, 0);
        CellController cellController = Instantiate(cellControllerPrefab,position, Quaternion.identity, transform);
        if (cellController == null)
        {
            Debug.LogError($"CellController component is missing on cell at ({x}, {y}).");
            return null;
        }
        cellController.name = $"Cell ({x},{y})";
        cellController.Init(_boardData.GetCellData(x, y));
                
#if UNITY_EDITOR
        GameObject cellControllerGameObject = cellController.gameObject;
        cellControllerGameObject.hideFlags = HideFlags.DontSave;
        cellControllerGameObject.tag = "EditorOnly";
#endif
        return cellController;
    }

    private void OnValidate()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        minesCount = Mathf.Clamp(minesCount, 1, width * height - 1);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () =>
            {
                if (this != null) RebuildBoard();
            };
        }
#endif
    }
    
    private void RebuildBoard()
    {
        if (_boardData == null || _boardData.Width != width || _boardData.Height != height)
            _boardData = new BoardData(width, height, minesCount);

        DestroyGrid();
        GenerateCells();
    }

    private void DestroyGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    [IDCCmd]
    private void AddCellLine(Direction direction, int minesCount = 2)
    {
        switch (direction)
        {
            case Direction.Down:
                height += 1;
                List<CellController> newRow = new List<CellController>(width);
                for (int i = 0; i < width; i++)
                {
                    CellController cellController = CreateCellController(i, height - 1);
                    if (cellController != null)
                    {
                        newRow.Add(cellController);
                    }
                }
                _cells.Add(newRow);
                break;
            case Direction.Up:
                height += 1;
                break;
            case Direction.Left:
                width += 1;
                // for (int i = 0; i < height; i++)
                // {
                //     CellController cellController = CreateCellController(0, i);
                //     if (cellController != null)
                //     {
                //         _cells[i].Insert(0, cellController);
                //     }
                // }
                break;
            case Direction.Right:
                width += 1;
                for (int i = 0; i < height; i++)
                {
                    CellController cellController = CreateCellController(width - 1, i);
                    if (cellController != null)
                    {
                        _cells[i].Add(cellController);
                    }
                }
                break;
            default:
                Debug.LogError("Invalid direction for adding a cell line.");
                break;
        }
    }
}


