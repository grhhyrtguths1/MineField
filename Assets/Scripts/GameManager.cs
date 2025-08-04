using GameResources;
using IDC;
using UnityEngine;
using Shop;

public class GameManager : MonoBehaviour
{
    [SerializeField] private ResourceConfig resourceConfig;
    [SerializeField] private ResourceView resourceView;
    [SerializeField] private ShopManager shop;
        
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
        shop.OpenShop();
    }

    private void Start()
    {
        IDCUtils.IDC.AddClass(_resourceManager);
        IDCUtils.IDC.AddClass(shop);
    }
}