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
        ShowHidden();
        switch (cellData.CellType)
        {
            case CellType.Mine:
                Instantiate(minePrefab, transform);
                break;
            case CellType.Number when cellData.Number >= 0:
                numberText.text = cellData.Number.ToString();
                break;
            default:
                Debug.LogError("CellView.Init called with invalid parameters: " +
                               $"cellType={cellData.CellType}, cellNumber={cellData.Number}");
                break;
        }
        cellData.OnNumberChanged += UpdateNumber;
    }
    
    public void Unsubscribe(CellData cellData)
    {
        if (cellData != null)
        {
            cellData.OnNumberChanged -= UpdateNumber;
        }
    }
    

    public void SetHovered()
    {
        _meshRenderer.material.color = Color.green; 
    }

    public void SetDefault(CellState cellState = CellState.Hidden)
    {
        _meshRenderer.material.color = cellState == CellState.Flagged ? Color.yellow : Color.gray;
    }
    
    // public void SetState(CellState cellState)
    // {
    //     switch (cellState)
    //     {
    //         case CellState.Hidden:
    //             ShowHidden();
    //             break;
    //         case CellState.Revealed:
    //             Reveal();
    //             break;
    //         case CellState.Flagged:
    //             ShowFlag();
    //             break;
    //     }
    // }

    public void Reveal()
    {
        _meshRenderer.enabled = false;
    }

    public void ShowFlag()
    {
        _meshRenderer.material.color = Color.yellow; 
    }

    public void ShowHidden()
    {
        _meshRenderer.enabled = true;
    }

    private void UpdateNumber(int num)
    {
        numberText.text = num.ToString();
    }
}