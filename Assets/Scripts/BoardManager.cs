using System.Collections.Generic;
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
        cellController.gameObject.hideFlags = HideFlags.DontSave;
        cellController.gameObject.tag = "EditorOnly";
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
}


