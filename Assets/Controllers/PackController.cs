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
    private Vector3 startLocalPos;
    
    // NEW: Track state and the spawned food item
    public bool IsOpened { get; private set; } = false;
    private FoodItemController spawnedFoodItem;

    private void Start()
    {
        if (modelToAnimate == null)
        {
            if (transform.childCount > 0) modelToAnimate = transform.GetChild(0);
            else modelToAnimate = transform;
        }

        if (modelToAnimate != null) startLocalPos = modelToAnimate.localPosition;

        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(5f, 5f, 5f); 
        }
    }

    private void Update()
    {
        // Only animate the box if it hasn't been opened yet
        if (!IsOpened && modelToAnimate != null)
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
        if (!IsOpened)
        {
            // --- STEP 1: OPEN PACK ---
            IsOpened = true;
            
            if (audioSource != null && tapSound != null)
                audioSource.PlayOneShot(tapSound);
            
            SpawnFoodItem();
            
            // Hide the pack visuals, but KEEP the GameObject active so we can detect the second tap
            if (modelToAnimate != null) modelToAnimate.gameObject.SetActive(false);
        }
        else
        {
            // --- STEP 2: COLLECT ITEM ---
            if (spawnedFoodItem != null)
            {
                spawnedFoodItem.CollectItem(); // Trigger fly away
            }
            
            // Destroy the empty pack object after a delay
            Destroy(gameObject, 0.5f);
        }
    }
    
    private void SpawnFoodItem()
    {
        if (scanResult == null) return;
        
        ItemModel item = ItemDatabase.Instance.GetItem(scanResult.itemId);
        if (item == null || item.itemPrefab == null) return;
        
        GameObject foodObj = Instantiate(item.itemPrefab, transform.position, Quaternion.identity);
        
        if (transform.parent != null)
        {
            foodObj.transform.parent = transform.parent;
            foodObj.transform.localPosition = new Vector3(0, 0.05f, 0); 
            foodObj.transform.localScale = Vector3.one * 0.05f; 
        }
        
        // Save reference to the controller so we can collect it later
        spawnedFoodItem = foodObj.AddComponent<FoodItemController>();
        spawnedFoodItem.Initialize(scanResult);
    }
}