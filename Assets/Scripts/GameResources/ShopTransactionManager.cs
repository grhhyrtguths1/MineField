using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameResources
{
    public class ShopTransactionManager : ResourceClient
    { 
        private bool CanConsume(Dictionary<ResourceType, int> costMap)
        {
            if (costMap == null || costMap.Count == 0)
            {
                Debug.LogError("Cost map is null or empty.");
                return false;
            }

            foreach (var cost in costMap)
            {
                if (!resourceManager.HasResource(cost.Key, cost.Value))
                {
                    return false;
                }
            }
            return true;
        }

        public bool ConsumeResources(Dictionary<ResourceType, int> costMap)
        {
            if (!CanConsume(costMap))
            {
                return false;
            }

            foreach (KeyValuePair<ResourceType, int> cost in costMap)
            {
                resourceManager.RemoveResource(cost.Key, cost.Value);
            }
            return true; 
        }
        
        public void AddResource(ResourceType resourceType, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogError("Amount must be greater than zero.");
                return;
            }

            resourceManager.AddResource(resourceType, amount);
        }
    }
}