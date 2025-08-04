using System;

namespace GameResources
{
    public static class ResourceManagerProvider
    {
        private static event Action<ResourceManager> OnResourceManagerAvailable;
        private static ResourceManager _resourceManager;

        public static void RegisterClient(ResourceClient client)
        {
            if (_resourceManager != null)
            {
                client.ReceiveResourceManager(_resourceManager);
            }
            else
            {
                OnResourceManagerAvailable += client.ReceiveResourceManager;
            }
        }
        
        public static void UnregisterProducer(TileResourceProducer tileResourceProducer)
        {
            OnResourceManagerAvailable -= tileResourceProducer.ReceiveResourceManager;
        }

        public static void SetResourceManager(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            OnResourceManagerAvailable?.Invoke(resourceManager);
        }
    }
}