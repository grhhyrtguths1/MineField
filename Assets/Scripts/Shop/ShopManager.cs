using System.Collections.Generic;
using GameResources;
using IDC;
using UnityEngine;

namespace Shop
{
    public class ShopManager : MonoBehaviour
    {
        [SerializeField] private List<ShopItemSO> items;
        [SerializeField] private ItemView itemViewPrefab;
        private ShopTransactionManager _shopTransactionManager;
        private TileResourceProducer _tileResourceProducer;

        private void Start()
        {
            _shopTransactionManager = new ShopTransactionManager();
        }

        private bool BuyItem(ShopItemSO item)
        {
            if (_shopTransactionManager.ConsumeResources(item.costMap))
            {
                Debug.Log($"Bought item: {item.itemName}");
                _shopTransactionManager.AddResource(item.itemType, 1);
                return true;
            }

            Debug.Log($"Not enough resources to buy item: {item.itemName}");
            return false;
        }
        
        [IDCCmd]
        public void OpenShop()
        {
            foreach (ShopItemSO itemSO in items)
            {
                CreateItemView(itemSO);
            }
        }
        
        [IDCCmd]
        public void CloseShop()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        private void CreateItemView(ShopItemSO shopItem)
        {
            ItemView itemView = Instantiate(itemViewPrefab, transform);
            itemView.Init(shopItem, BuyItem);
            itemView.name = $"ShopItem_{shopItem.itemName}";
        }
    }
}