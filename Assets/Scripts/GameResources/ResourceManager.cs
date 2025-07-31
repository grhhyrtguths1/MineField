using System;
using IDC;
using NaughtyAttributes;
using UnityEngine;

namespace GameResources
{
    public class ResourceManager
    {
       [SerializeField, ReadOnly] private ResourceData _data;
        
        public static ResourceManager Instance { get; private set; }

        public ResourceManager(ResourceData data, ResourceView resourceView)
        {
            if (Instance != null)
            {
                Debug.LogError("ResourceManager instance already exists!");
                return;
            }
            Instance = this;
            _data = data;
            BindView(resourceView);
        
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                resourceView.UpdateResourceUI(type, _data.Get(type));
            }
        }
        
        [IDCCmd]
        public void AddResource(ResourceType type, int amount)
        {
            _data.Add(type, amount);
        }

        public int GetResource(ResourceType type)
        {
            return _data.Get(type);
        }
        
        [IDCCmd]
        public void RemoveResource(ResourceType type, int amount)
        {
            _data.Add(type, -amount);
        }

        private void BindView(ResourceView view)
        {
            _data.OnResourceChanged += view.UpdateResourceUI;
        }

        private void UnbindView(ResourceView view)
        {
            _data.OnResourceChanged -= view.UpdateResourceUI;
        }
    }
}