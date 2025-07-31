using System;
using System.Collections.Generic;
using GameResources;
using UnityEditor;
using UnityEngine;

[Serializable]
public class CellData
{
    public CellType CellType 
    {
        get => _cellType;
        set => SetField(ref _cellType, value, OnTypeChanged);
    }
    private CellType _cellType;
    
    public int Number 
    { 
        get => _number;
        set => SetField(ref _number, value, OnNumberChanged);
    }
    private int _number;
    
    public CellState CellState
    { 
        get => _cellState;
        set => SetField(ref _cellState, value, OnStateChanged);
    }
    private CellState _cellState;
    
    public int ProductionAmount
    {
        get => _productionAmount;
        set => SetField(ref _productionAmount, value, OnProductionAmountChanged);
    }
    private int _productionAmount = 3;
    
    public float ProductionInterval
    {
        get => _productionInterval;
        set => SetField(ref _productionInterval, value, OnProductionIntervalChanged);
    }
    private float _productionInterval = 2f;
    
    public bool IsProducing
    {
        get => _isProducing;
        set
        {
            Debug.Log($"Setting IsProducing to {value} for cell with type {CellType}");
            SetField(ref _isProducing, value, OnIsProducingChanged); }
        
    }
    private bool _isProducing;

    private readonly Dictionary<Direction, CellData> _neighbors;

    public Dictionary<ResourceType, int> ProductionMap
    {
        get
        {
            Dictionary<ResourceType, int> productionMap = new();
            foreach (var neighbor in _neighbors.Values)
            {
                if (neighbor.CellType == CellType.Mine || 
                    neighbor.CellState != CellState.Revealed ) continue;
                var key = BoardUtils.NumberToResourceType(neighbor.Number);
                productionMap[key] = productionMap.GetValueOrDefault(key, 0) + neighbor.ProductionAmount;
            }
            return productionMap;
        }
    }

    public event Action<CellState> OnStateChanged;
    public event Action<int> OnNumberChanged;
    public event Action<CellType> OnTypeChanged;
    public event Action<int> OnProductionAmountChanged;
    public event Action<bool> OnIsProducingChanged; 
    public event Action<float> OnProductionIntervalChanged;  
    
    public CellData(CellType cellType)
    {
        CellType = cellType;
        CellState = CellState.Hidden;
        _neighbors = new Dictionary<Direction, CellData>();
    }
    
    public void SetNeighbor(Direction direction, CellData neighbor)
    {
        if(!_neighbors.TryAdd(direction, neighbor)) return;
        if(neighbor.CellType == CellType.Mine) Number++;
    }
    
    private void SetField<T>(ref T field, T value, Action<T> onChanged)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        onChanged?.Invoke(value);
    }
}

