using System;
using IDC;
using UnityEngine;

namespace GameResources
{
    public class ResourceManager
    {
       private ResourceData _data;
        public ResourceManager(ResourceData data, ResourceView resourceView)
        {
            _data = data;
            BindView(resourceView);
        
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                resourceView.UpdateResourceUI(type, _data.Get(type));
            }
            ResourceManagerProvider.SetResourceManager(this);
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