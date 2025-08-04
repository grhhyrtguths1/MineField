using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameResources
{
    public class Producer : ResourceClient
    {
        public Producer(Func<Dictionary<ResourceType, int>> getResourceMap, Func<float> interval)
            : base(getResourceMap, interval) { }

        public Producer(Dictionary<ResourceType, int> productionMap, float interval)
            : base(productionMap, interval){ }
    
        public void GenerateResources(bool enable)
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = null;

            if (!enable) return;

            cancellationTokenSource = new CancellationTokenSource();
            _ = GenerateResourcesAsync(cancellationTokenSource.Token, interval.Invoke());
        }
        
        private async UniTaskVoid GenerateResourcesAsync(CancellationToken token, float interval)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: token);
                    Dictionary<ResourceType, int> productionMap = getResourceMap?.Invoke();
                    if (productionMap == null)
                    {
                        Debug.LogWarning("Production map is empty or null.");
                        return;
                    }
                    foreach (var resource in productionMap)
                    {
                        resourceManager.AddResource(resource.Key, resource.Value);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful cancellation
            }
        }
    }
}