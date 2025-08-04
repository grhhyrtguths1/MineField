using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public class ItemView : MonoBehaviour
    {
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescriptionText;
        
        private ShopItemSO _itemSO;
        private Func<ShopItemSO, bool> _onClickAction;

        public void Init(ShopItemSO itemSO, Func<ShopItemSO, bool> onClickAction)
        {
            _itemSO = itemSO;
            _onClickAction = onClickAction;

            itemNameText.text = itemSO.itemName;
            itemDescriptionText.text = itemSO.itemDescription;

            buyButton.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            bool success = _onClickAction?.Invoke(_itemSO) ?? false;
            itemNameText.color = success ? Color.green : Color.red;
        }
    }
}