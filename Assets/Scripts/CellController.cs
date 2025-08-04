using System;
using GameResources;
using UnityEngine;

public class CellController : MonoBehaviour
{
    [SerializeField] private CellView cellViewPrefab;
    private CellData _cellData;
    private CellView _cellView;
    private TileResourceProducer _tileResourceProducer;

    private static CellView CreateCellView(Transform parent, CellView cellViewPrefab)
    {
        //TODO add pooling for CellView
        CellView cell = Instantiate(cellViewPrefab, parent.position, Quaternion.identity, parent);
        return cell;
    }

    public void Init(CellData cellData)
    {
        _cellData = cellData;
        _cellView = CreateCellView(transform, cellViewPrefab); 
        if(cellData == null)
        {
            throw new ArgumentNullException(nameof(cellData), "CellData cannot be null.");
        }
        _cellView.Init(_cellData);
        _tileResourceProducer = new TileResourceProducer(() => _cellData.ProductionMap, () =>cellData.ProductionInterval);
        _cellData.OnIsProducingChanged += _tileResourceProducer.GenerateResources;
    }
    
    private void SetState(CellState cellState)
    {
        _cellData.CellState = cellState;
        _cellData.IsProducing = cellState == CellState.Flagged;
        switch (cellState)
        {
            case CellState.Hidden:
                _cellView.ShowHidden();
                break;
            case CellState.Revealed:
                _cellView.Reveal();
                break;
            case CellState.Flagged:
                _cellView.ShowFlag();
                break;
        }
    }
    
    void OnDestroy()
    {
        ResourceManagerProvider.UnregisterProducer(_tileResourceProducer);
        if (_cellView != null)
        {
            _cellView.Unsubscribe(_cellData);
        }
    }
    
#region InputHandling
    
    private void OnMouseEnter()
    {
        _cellView.SetHovered();
    }

    private void OnMouseExit()
    {
        _cellView.SetDefault(_cellData.CellState);
    }
    
    private void OnMouseDown()
    {
        if (_cellData.CellState == CellState.Hidden)
        {
            SetState(CellState.Revealed);
        }
        else if (_cellData.CellState == CellState.Revealed)
        {
            // Handle logic for already revealed cells if needed
        }
    }
    
    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (_cellData.CellState == CellState.Hidden)
            {
                SetState(CellState.Flagged);
            }
            else if (_cellData.CellState == CellState.Flagged)
            {
                SetState(CellState.Hidden);
            }
        }
    }
    
#endregion
}