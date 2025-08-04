using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;


public class BoardData
{
    private List<List<CellData>> _boardCells;
    public int Width { get; private set; }
    public int Height { get; private set; }
    private int _minesCount;
    
    public BoardData(int width, int height, int minesCount)
    {
        Width = width;
        Height = height;
        _minesCount = minesCount;
        
        _boardCells = InitBoard(width, height);
        _boardCells = InitMines(_boardCells, minesCount);
        LinkCells();
    }
    
    public CellData GetCellData(int x, int y)
    {
        if (IsValidCell(x, y))
        {
            return _boardCells[y][x];
        }
        throw new System.ArgumentOutOfRangeException($"Cell position ({x}, {y}) is out of bounds.");
    }
    
    public void AddCellLineData(Direction direction = Direction.Right, int minesCount = 2)
    {
        minesCount = Mathf.Clamp(minesCount, 0, Width * Height - _minesCount);
        _minesCount += minesCount;
        switch (direction)
        {
            case Direction.Right:
                AddVerticalColumn( false, minesCount);
                Width += 1;
                break;

            case Direction.Left:
                AddVerticalColumn( true, minesCount);
                Width += 1;
                break;

            case Direction.Down:
                AddHorizontalRow( false, minesCount);
                Height += 1;
                break;

            case Direction.Up:
                AddHorizontalRow( true, minesCount);
                Height += 1;
                break;

            default:
                Debug.LogError("Invalid direction for adding a cell line.");
                break;
        }
        LinkCells();
        return;

        void AddVerticalColumn(bool atStart, int mines)
        {
            var newColumn = InitMines(InitBoard(1, Height), mines);
            for (int i = 0; i < Height; i++)
            {
                if (atStart)
                    _boardCells[i].Insert(0, newColumn[i][0]);
                else
                    _boardCells[i].Add(newColumn[i][0]);
            }
        }

        void AddHorizontalRow(bool atStart, int mines)
        {
            var newRow = InitMines(InitBoard(Width, 1), mines)[0];
            if (atStart)
                _boardCells.Insert(0, newRow);
            else
                _boardCells.Add(newRow);
        }
    }
    
    private void LinkCells()
    {
        foreach (List<CellData> row in _boardCells)
        {
            foreach (CellData cell in row)
            {
                foreach (var kvp in BoardUtils.Offsets)
                {
                    Vector2Int offset = kvp.Value;
                    int neighborX = row.IndexOf(cell) + offset.x;
                    int neighborY = _boardCells.IndexOf(row) + offset.y;

                    if (!IsValidCell(neighborX, neighborY)) continue;
                    CellData neighbor = _boardCells[neighborY][neighborX];
                    cell.SetNeighbor(kvp.Key, neighbor);
                }
            }
        }
    }

    private bool IsValidCell(int neighborX, int neighborY)
    {
        return neighborX >= 0 && neighborX < Width && neighborY >= 0 && neighborY < Height;
    }
    
    private static List<List<CellData>> InitBoard(int width, int height)
    {
        List<List<CellData>> board = new List<List<CellData>>(height);
        for (int i = 0; i < height; i++)
        {
            board.Add(new List<CellData>(width));
            for (int j = 0; j < width; j++)
            {
                board[i].Add(new CellData(CellType.Number));
            }
        }
        Debug.Log("Initialized board with dimensions: " + width + "x" + height);
        return board;
    }
    
    private static List<Vector2Int> GetRandomMinePositions(int width, int height, int count)
    {
        return Enumerable.Range(0, width * height)
            .OrderBy(_ => Random.value)
            .Take(count)
            .Select(i => new Vector2Int(i % width, i / width))
            .ToList();
    }
    
    private static List<List<CellData>> InitMines(List<List<CellData>> board, int mineCount)
    {
        var width = board[0].Count;
        var height = board.Count;
        List<Vector2Int> minePositions = GetRandomMinePositions(width, height, mineCount);
        
        foreach (var pos in minePositions)
        {
            board[pos.y][pos.x] = new CellData(CellType.Mine);
        }
        Debug.Log($"Initialized {mineCount} mines at random positions on the board.");
        return board;
    }
}
