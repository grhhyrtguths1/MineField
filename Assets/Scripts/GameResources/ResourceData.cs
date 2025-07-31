using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace GameResources
{
    public enum ResourceType
    {
        Zero,
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
    }
    
    public class ResourceData
    {
        private Dictionary<ResourceType, int> _resources = new();
        private Dictionary<ResourceType, int> _maxLimits = new();

        [CanBeNull] public event Action<ResourceType, int> OnResourceChanged;

        public ResourceData(ResourceConfig config)
        {
            foreach (var (type, entry) in config.resources)
            {
                _resources[type] = entry.startAmount;
                _maxLimits[type] = entry.maxAmount;
            }
        }

        public void Add(ResourceType type, int amount)
        {
            if (!_resources.ContainsKey(type)) return;

            int current = _resources[type];
            int newAmount = Mathf.Clamp(current + amount, 0, _maxLimits[type]);

            if (newAmount == current) return;
            _resources[type] = newAmount;
            OnResourceChanged?.Invoke(type, newAmount);
        }

        public int Get(ResourceType type) => _resources.GetValueOrDefault(type, 0);
        public int GetMax(ResourceType type) => _maxLimits.GetValueOrDefault(type, 0);
    }
}