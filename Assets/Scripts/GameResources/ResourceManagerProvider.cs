using System;

namespace GameResources
{
    public static class ResourceManagerProvider
    {
        private static event Action<ResourceManager> OnResourceManagerAvailable;
        private static ResourceManager _resourceManager;

        public static void RegisterProducer(Producer producer)
        {
            if (_resourceManager != null)
            {
                producer.ReceiveResourceManager(_resourceManager);
            }
            else
            {
                OnResourceManagerAvailable += producer.ReceiveResourceManager;
            }
        }
        
        public static void UnregisterProducer(Producer producer)
        {
            OnResourceManagerAvailable -= producer.ReceiveResourceManager;
        }

        public static void SetResourceManager(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            OnResourceManagerAvailable?.Invoke(resourceManager);
        }
    }
}