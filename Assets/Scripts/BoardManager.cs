using System.Collections.Generic;
using IDC;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private int startingWidth;
    [SerializeField] private int startingHeight;
    [SerializeField] private int minesCount;
    [SerializeField] private float cellPadding = 0.1f;
    [SerializeField] private CellController cellControllerPrefab;

    private BoardData _boardData;
    private List<List<CellController>> _cells;
    private void Awake()
    {
        _boardData = new BoardData(startingWidth, startingHeight, minesCount);
        GenerateCells();
    }
    
    private void Start()
    {
        IDCUtils.IDC.AddClass(this);
    }

    private void GenerateCells()
    {
        _cells = new List<List<CellController>>(startingHeight);
        for (int y = 0; y < startingHeight; y++)
        {
            List<CellController> row = new List<CellController>(startingWidth);
            for (int x = 0; x < startingWidth; x++)
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
        CellData cellData = _boardData.GetCellData(x, y);
        if (cellData != null)
        {
            cellController.Init(cellData);
        }

#if UNITY_EDITOR
        GameObject cellControllerGameObject = cellController.gameObject;
        cellControllerGameObject.hideFlags = HideFlags.DontSave;
        cellControllerGameObject.tag = "EditorOnly";
#endif
        return cellController;
    }

    private void OnValidate()
    {
        startingWidth = Mathf.Max(1, startingWidth);
        startingHeight = Mathf.Max(1, startingHeight);
        minesCount = Mathf.Clamp(minesCount, 1, startingWidth * startingHeight - 1);

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
        if (_boardData == null || _boardData.Width != startingWidth || _boardData.Height != startingHeight)
            _boardData = new BoardData(startingWidth, startingHeight, minesCount);

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
    private void AddCellLine(Direction direction = Direction.Right, int minesCount = 2)
    {
        _boardData.AddCellLine(direction, minesCount);
        switch (direction)
        {
            case Direction.Right:
                for (int i = 0; i < _boardData.Height; i++)
                {
                    CellController cellController = CreateCellController(_boardData.Width - 1, i);
                    if (cellController != null)
                    {
                        _cells[i].Add(cellController);
                    }
                }
                break;

            case Direction.Left:
                for (int i = 0; i < _boardData.Height; i++)
                {
                    CellController cellController = CreateCellController(0, i);
                    if (cellController != null)
                    {
                        _cells[i].Insert(0, cellController);
                    }
                }
                break;

            case Direction.Down:
                List<CellController> newRowDown = new List<CellController>(_boardData.Width);
                for (int x = 0; x < _boardData.Width; x++)
                {
                    CellController cellController = CreateCellController(x, _boardData.Height - 1);
                    if (cellController != null)
                    {
                        newRowDown.Add(cellController);
                    }
                }
                _cells.Add(newRowDown);
                break;

            case Direction.Up:
                List<CellController> newRowUp = new List<CellController>(_boardData.Width);
                for (int x = 0; x < _boardData.Width; x++)
                {
                    CellController cellController = CreateCellController(x, 0);
                    if (cellController != null)
                    {
                        newRowUp.Add(cellController);
                    }
                }
                _cells.Insert(0, newRowUp);
                break;

            default:
                Debug.LogError("Invalid direction for adding a cell line.");
                break;
        }
    }
}


