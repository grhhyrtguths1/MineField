using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using GameResources;
using UnityEngine;

namespace Shop
{
    [CreateAssetMenu(fileName = "ShopItem", menuName = "Shop/ShopItem")]
    public class ShopItemSO : ScriptableObject
    {
        public string itemName;
        public string itemDescription;
        public ResourceType itemType;
        [SerializedDictionary("Resource", "Cost")]
        public SerializedDictionary<ResourceType, int> costMap;
    }
}