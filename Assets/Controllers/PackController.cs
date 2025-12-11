using System.Collections;
using UnityEngine;

public class PackController : MonoBehaviour
{
    [Header("Pack Model")]
    [SerializeField] private Transform modelToAnimate; 
    
    [Header("Spawn Settings")]
    [SerializeField] private float floatHeight = 0.5f; 
    [SerializeField] private float floatSpeed = 2f; 
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tapSound;
    
    private ScanResult scanResult;
    private bool hasBeenTapped = false;
    private Vector3 startLocalPos;

    private void Start()
    {
        // 1. Auto-find model
        if (modelToAnimate == null)
        {
            if (transform.childCount > 0) modelToAnimate = transform.GetChild(0);
            else modelToAnimate = transform;
        }

        if (modelToAnimate != null) startLocalPos = modelToAnimate.localPosition;

        // 2. FORCE HUGE COLLIDER (The "Big Hit Box" Fix)
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(5f, 5f, 5f); // 5x bigger than the model
    }

    private void Update()
    {
        if (hasBeenTapped) return;

        // Simple Animation
        if (modelToAnimate != null)
        {
            float newY = startLocalPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            modelToAnimate.localPosition = new Vector3(startLocalPos.x, newY, startLocalPos.z);
        }
    }
    
    public void Initialize(ScanResult result)
    {
        scanResult = result;
    }
    
    // Called by ARImageTrackingManager
    public void TapPack()
    {
        if (hasBeenTapped) return;
        hasBeenTapped = true;
        
        if (audioSource != null && tapSound != null)
            audioSource.PlayOneShot(tapSound);
        
        SpawnFoodItem();
        
        if (modelToAnimate != null) modelToAnimate.gameObject.SetActive(false);
        Destroy(gameObject, 0.5f);
    }
    
    private void SpawnFoodItem()
    {
        if (scanResult == null) return;
        
        ItemModel item = ItemDatabase.Instance.GetItem(scanResult.itemId);
        if (item == null || item.itemPrefab == null) return;
        
        GameObject foodItem = Instantiate(item.itemPrefab, transform.position, Quaternion.identity);
        
        if (transform.parent != null)
        {
            foodItem.transform.parent = transform.parent;
            // Ensure food item is visible and positioned correctly
            foodItem.transform.localPosition = new Vector3(0, 0.05f, 0); 
            foodItem.transform.localScale = Vector3.one * 0.05f; 
        }
        
        FoodItemController foodController = foodItem.AddComponent<FoodItemController>();
        foodController.Initialize(scanResult);
    }
}