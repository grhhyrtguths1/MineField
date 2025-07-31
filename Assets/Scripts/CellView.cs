using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CellView :MonoBehaviour
{
    [SerializeField] private GameObject minePrefab;
    [SerializeField] private List<GameObject> numberPrefab;
    [SerializeField] private TMP_Text numberText;
    private MeshRenderer _meshRenderer;
    
    public void Init(CellData cellData)
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        SetState(CellState.Hidden); 
        if (cellData.CellType == CellType.Mine)
        {
            Instantiate(minePrefab, transform);
        }
        else if (cellData.CellType == CellType.Number && cellData.Number >= 0)
        {
            numberText.text = cellData.Number.ToString();
        }
        else
        {
            Debug.LogError("CellView.Init called with invalid parameters: " +
                           $"cellType={cellData.CellType}, cellNumber={cellData.Number}");
        }
        cellData.OnStateChanged += SetState;
        cellData.OnNumberChanged += UpdateNumber;
    }
    

    public void SetHovered()
    {
        _meshRenderer.material.color = Color.green; 
    }

    public void SetDefault(CellState cellState = CellState.Hidden)
    {
        _meshRenderer.material.color = cellState == CellState.Flagged ? Color.yellow : Color.gray;
    }
    
    public void SetState(CellState cellState)
    {
        switch (cellState)
        {
            case CellState.Hidden:
                ShowHidden();
                break;
            case CellState.Revealed:
                Reveal();
                break;
            case CellState.Flagged:
                ShowFlag();
                break;
        }
    }
    private void Reveal()
    {
        _meshRenderer.enabled = false;
    }

    private void ShowFlag()
    {
        _meshRenderer.material.color = Color.yellow; 
    }

    private void ShowHidden()
    {
        _meshRenderer.enabled = true;
    }

    private void UpdateNumber(int num)
    {
        numberText.text = num.ToString();
    }
}