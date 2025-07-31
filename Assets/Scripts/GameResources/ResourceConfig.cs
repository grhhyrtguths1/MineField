using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace GameResources
{
    [CreateAssetMenu(fileName = "NewResourceConfig", menuName = "Game/Resource Config")]
    public class ResourceConfig : ScriptableObject
    {
        [System.Serializable]
        public struct ResourceEntry
        {
            [Min(0)] public int startAmount;
            [Min(0)] public int maxAmount;
        }
        
        [SerializedDictionary("ResourceType", "Data")]
        public SerializedDictionary<ResourceType, ResourceEntry> resources;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            var keys = new List<ResourceType>(resources.Keys);
            foreach (var key in keys)
            {
                var entry = resources[key];
                entry.startAmount = Mathf.Clamp(entry.startAmount, 0, entry.maxAmount);
                resources[key] = entry; // reassign to update the dictionary
            }
        }
#endif
    }
}