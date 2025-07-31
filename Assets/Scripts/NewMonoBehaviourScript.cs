using System;
using System.Collections.Generic;
using GameResources;
using UnityEngine;

public enum Direction
{
    Up,
    Down,
    Left,
    Right, 
    BottomLeft,
    BottomRight,
    TopLeft,
    TopRight
}

public static class BoardUtils
{
    public static readonly Dictionary<Direction, Vector2Int> Offsets = new()
    {
        { Direction.Up, new Vector2Int(0, -1) },
        { Direction.Down, new Vector2Int(0, 1) },
        { Direction.Left, new Vector2Int(-1, 0) },
        { Direction.Right, new Vector2Int(1, 0) },
        { Direction.TopLeft, new Vector2Int(-1, -1) },
        { Direction.TopRight, new Vector2Int(1, -1) },
        { Direction.BottomLeft, new Vector2Int(-1, 1) },
        { Direction.BottomRight, new Vector2Int(1, 1) }
    };
    
    public static ResourceType NumberToResourceType(int number)
    {
        return number switch
        {
            0 => ResourceType.Zero,
            1 => ResourceType.One,
            2 => ResourceType.Two,
            3 => ResourceType.Three,
            4 => ResourceType.Four,
            5 => ResourceType.Five,
            6 => ResourceType.Six,
            7 => ResourceType.Seven,
            8 => ResourceType.Eight,
            _ => throw new ArgumentOutOfRangeException(nameof(number), number,
                "Invalid number for resource type")
        };
    }
}

public enum CellState
{
    Hidden,
    Revealed,
    Flagged,
}

public enum CellType
{
    None,
    Mine,
    Number,
}
