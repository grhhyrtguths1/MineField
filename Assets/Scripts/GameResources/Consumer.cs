using System;
using System.Collections.Generic;

namespace GameResources
{
    public class Consumer : ResourceClient
    {
        public Consumer(Func<Dictionary<ResourceType, int>> getResourceMap, Func<float> interval)
            : base(getResourceMap, interval) { }

        public Consumer(Dictionary<ResourceType, int> productionMap, float interval)
            : base(productionMap, interval) { }
    }
}