using System;
using System.Collections.Generic;
using System.Threading;

namespace GameResources
{
    public abstract class ResourceClient
    {
        protected ResourceManager resourceManager;
        
        protected ResourceClient()
        {
            ResourceManagerProvider.RegisterClient(this);
        }
        
        public void ReceiveResourceManager(ResourceManager manager)
        {
            resourceManager = manager;
        }
    }
}