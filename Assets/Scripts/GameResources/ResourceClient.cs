using System;
using System.Collections.Generic;
using System.Threading;

namespace GameResources
{
    public abstract class ResourceClient
    {
        protected ResourceManager resourceManager;
        protected CancellationTokenSource cancellationTokenSource;
        protected readonly Func<Dictionary<ResourceType, int>> getResourceMap;
        protected readonly Func<float> interval;

        protected ResourceClient(Func<Dictionary<ResourceType, int>> getResourceMap, Func<float> interval)
        {
            this.getResourceMap = getResourceMap;
            this.interval = interval;
            ResourceManagerProvider.RegisterClient(this);
        }

        protected ResourceClient(Dictionary<ResourceType, int> productionMap, float interval)
        {
            getResourceMap = () => productionMap;
            this.interval = () => interval;
            ResourceManagerProvider.RegisterClient(this);
        }
        
        public void ReceiveResourceManager(ResourceManager manager)
        {
            resourceManager = manager;
        }
    }
}