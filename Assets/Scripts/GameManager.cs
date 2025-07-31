using System;
using GameResources;
using IDC;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameResources.ResourceConfig resourceConfig;
    [SerializeField] private ResourceView resourceView;
        
    private ResourceManager _resourceManager;
    private BoardManager _boardManager;
        
    private void Awake()
    {
        if (resourceConfig == null)
        {
            Debug.LogError("ResourceConfig is not assigned in GameManager.");
            return;
        }
            
        ResourceData resourceData = new ResourceData(resourceConfig);
        _resourceManager = new ResourceManager(resourceData, resourceView);
    }

    private void Start()
    {
        IDCUtils.IDC.AddClass(_resourceManager);
    }
}