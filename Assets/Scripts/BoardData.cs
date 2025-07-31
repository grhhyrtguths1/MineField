using System.Collections.Generic;
using System.Linq;
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
        _boardCells = new List<List<CellData>>(height);
        _minesCount = minesCount;
        
        InitBoard();
        InitMines();
        LinkCells();
    }
    
    private void InitBoard()
    {
        for (int i = 0; i < Height; i++)
        {
            _boardCells.Add(new List<CellData>(Width));
            for (int j = 0; j < Width; j++)
            {
                _boardCells[i].Add(new CellData(CellType.Number));
            }
        }
    }
    
    private void InitMines()
    {
        List<Vector2Int> minePositions = GetRandomMinePositions(Width, Height, _minesCount);
        
        foreach (var pos in minePositions)
        {
            _boardCells[pos.y][pos.x] = new CellData(CellType.Mine);
        }
    }
    
    public CellData GetCellData(int x, int y)
    {
        if (IsValidCell(x, y))
        {
            return _boardCells[y][x];
        }
        else
        {
            throw new System.ArgumentOutOfRangeException($"Cell position ({x}, {y}) is out of bounds.");
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

                    if (IsValidCell(neighborX, neighborY))
                    {
                        CellData neighbor = _boardCells[neighborY][neighborX];
                        cell.SetNeighbor(kvp.Key, neighbor);
                    }
                }
            }
        }
    }

    private bool IsValidCell(int neighborX, int neighborY)
    {
        return neighborX >= 0 && neighborX < Width && neighborY >= 0 && neighborY < Height;
    }

    private List<Vector2Int> GetRandomMinePositions(int width, int height, int count)
    {
        return Enumerable.Range(0, width * height)
            .OrderBy(_ => Random.value)
            .Take(count)
            .Select(i => new Vector2Int(i % width, i / width))
            .ToList();
    }
}