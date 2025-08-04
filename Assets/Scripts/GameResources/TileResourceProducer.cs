using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameResources
{
    public class TileResourceProducer : ResourceClient
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Func<Dictionary<ResourceType, int>> _getResourceMap;
        private readonly Func<float> _interval;

        public TileResourceProducer(Func<Dictionary<ResourceType, int>> getResourceMap, Func<float> interval)
            : base()
        {
            _getResourceMap = getResourceMap ?? throw new ArgumentNullException(nameof(getResourceMap),
                "Resource map function cannot be null.");
            _interval = interval ?? throw new ArgumentNullException(nameof(interval),
                "Interval function cannot be null.");
        }

        public TileResourceProducer(Dictionary<ResourceType, int> productionMap, float interval)
            : base()
        {
            _getResourceMap = () => productionMap ?? throw new ArgumentNullException(nameof(productionMap),
                "Production map cannot be null.");
            _interval = () => interval > 0 ? interval : throw new ArgumentOutOfRangeException(nameof(interval),
                "Interval must be greater than zero.");
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
                    Dictionary<ResourceType, int> productionMap = _getResourceMap?.Invoke();
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