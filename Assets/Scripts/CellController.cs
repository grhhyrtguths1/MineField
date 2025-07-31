using System;
using System.Collections;
using System.Collections.Generic;
using GameResources;
using UnityEngine;
public class CellController : MonoBehaviour
{
    [SerializeField] private CellView cellViewPrefab;
    private CellData _cellData;
    private CellView _cellView;

    private static CellView CreateCellView(Transform parent, CellView cellViewPrefab)
    {
        CellView cell = Instantiate(cellViewPrefab, parent.position, Quaternion.identity, parent);
        return cell;
    }

    public void Init(CellData cellData)
    {
        _cellData = cellData;
        _cellView = CreateCellView(transform, cellViewPrefab); 
        if(cellData == null)
        {
            throw new System.ArgumentNullException(nameof(cellData), "CellData cannot be null.");
        }
        _cellView.Init(_cellData);
        _cellData.OnIsProducingChanged += GenerateResources;
    }
    
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
    
    private void SetState(CellState cellState)
    {
        _cellData.CellState = cellState;
        _cellView.SetState(cellState);

        _cellData.IsProducing = cellState == CellState.Flagged;
    }
    
    //Mayb cell currency class
    private Coroutine _resourceGenerationCoroutine;

    private void GenerateResources(bool enable)
    {
        if (_resourceGenerationCoroutine != null)
        {
            StopCoroutine(_resourceGenerationCoroutine);
            _resourceGenerationCoroutine = null;
        }

        if (!enable) return;

        if (!TryGetResourceType(out ResourceType resourceType))
        {
            Debug.LogError($"Invalid cell number: {_cellData.Number}");
            return;
        }

        _resourceGenerationCoroutine = StartCoroutine(GenerateResourcesCoroutine(resourceType));
    }

    private bool TryGetResourceType(out ResourceType resourceType)
    {
        try
        {
            resourceType = BoardUtils.NumberToResourceType(_cellData.Number);
            return true;
        }
        catch (Exception e)
        {
            resourceType = default;
            return false;
        }
    }

    private IEnumerator GenerateResourcesCoroutine(ResourceType resourceType)
    {
        while (true)
        {
            yield return new WaitForSeconds(_cellData.ProductionInterval);
            Dictionary<ResourceType, int> productionMap = _cellData.ProductionMap;
            foreach (var resource in productionMap)
            {
                ResourceManager.Instance.AddResource(resource.Key, resource.Value);
            }
        }
    }
}