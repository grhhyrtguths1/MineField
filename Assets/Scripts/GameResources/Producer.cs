using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameResources
{
    public class Producer
    {
        private ResourceManager _resourceManager;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Func<Dictionary<ResourceType, int>> _getProductionMap;
        private readonly Func<float> _interval;
    
        public Producer(Func<Dictionary<ResourceType, int>> getProductionMap, Func<float> interval)
        {
            _getProductionMap = getProductionMap;
            _interval = interval;
            ResourceManagerProvider.RegisterProducer(this);
        }
        
        public Producer(Dictionary<ResourceType, int> productionMap, float interval)
        {
            _getProductionMap = () => productionMap;
            _interval = () => interval;
            ResourceManagerProvider.RegisterProducer(this);
        }
    
        public void ReceiveResourceManager(ResourceManager manager)
        {
            _resourceManager = manager;
        }
    
        public void GenerateResources(bool enable)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;

            if (!enable) return;

            _cancellationTokenSource = new CancellationTokenSource();
            _ = GenerateResourcesAsync(_cancellationTokenSource.Token, _interval.Invoke());
        }
        private async UniTaskVoid GenerateResourcesAsync(CancellationToken token, float interval)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: token);
                    Dictionary<ResourceType, int> productionMap = _getProductionMap?.Invoke();
                    if (productionMap == null)
                    {
                        Debug.LogWarning("Production map is empty or null.");
                        return;
                    }
                    foreach (var resource in productionMap)
                    {
                        _resourceManager.AddResource(resource.Key, resource.Value);
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