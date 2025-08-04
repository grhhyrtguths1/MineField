using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class CellView :MonoBehaviour
{
    [SerializeField] private GameObject minePrefab;
    [SerializeField] private List<GameObject> numberPrefab;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private MeshRenderer meshRenderer;
    
    public void Init(CellData cellData)
    {
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
        meshRenderer.material.color = Color.green; 
    }

    public void SetDefault(CellState cellState = CellState.Hidden)
    {
        meshRenderer.material.color = cellState == CellState.Flagged ? Color.yellow : Color.gray;
    }

    public void Reveal()
    {
        meshRenderer.transform.DOScale(0f,0.35f).SetEase(Ease.InBounce);
    }

    public void ShowFlag()
    {
        meshRenderer.material.color = Color.yellow; 
    }

    public void ShowHidden()
    {
        meshRenderer.enabled = true;
    }

    private void UpdateNumber(int num)
    {
        numberText.text = num.ToString();
    }
}