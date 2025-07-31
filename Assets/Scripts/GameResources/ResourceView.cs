using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using TMPro;
using UnityEngine;

namespace GameResources
{
    public class ResourceView : MonoBehaviour
    {
        [SerializedDictionary("Resource", "Amount")] [SerializeField]
        private SerializedDictionary<ResourceType, TMP_Text> resourceTexts;

        public void UpdateResourceUI(ResourceType type, int amount)
        {
            if (resourceTexts.TryGetValue(type, out TMP_Text text))
            {
                text.text = amount.ToString();
            }
        }
    }
}