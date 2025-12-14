using System.Collections;
using UnityEngine;

public class PackController : MonoBehaviour
{
    [Header("Pack Model")]
    [Tooltip("Drag the Child Cube/Mesh here (NOT the parent)")]
    [SerializeField] private Transform modelToAnimate; 
    
    [Header("Spawn Settings")]
    [SerializeField] private float floatHeight = 0.5f; 
    [SerializeField] private float floatSpeed = 2f; 
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tapSound;
    
    private ScanResult scanResult;
    private Vector3 startLocalPos;
    
    // Track state
    public bool IsOpened { get; private set; } = false;
    private FoodItemController spawnedFoodItem;

    private void Start()
    {
        // 1. Setup Model Reference
        if (modelToAnimate == null)
        {
            if (transform.childCount > 0) modelToAnimate = transform.GetChild(0);
            else modelToAnimate = transform;
        }

        if (modelToAnimate != null) startLocalPos = modelToAnimate.localPosition;

        // 2. Setup Collider (Essential for Tapping)
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(5f, 5f, 5f); 
        }

        // --- FIX: AUTO-SETUP AUDIO SOURCE ---
        // If you forgot to drag it in, we find it or create it.
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // IMPORTANT FOR MOBILE: 
        // 0.0f = 2D Sound (Plays clearly in headset/speakers)
        // 1.0f = 3D Sound (Might be silent if camera is "far" from object)
        audioSource.spatialBlend = 0.0f; 
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        // Only animate if closed
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
    
    public void TapPack()
    {
        if (!IsOpened)
        {
            // --- STEP 1: OPEN PACK ---
            IsOpened = true;
            
            // Play Sound safely
            PlaySound();
            
            // Hide Pack Visuals
            if (modelToAnimate != null) 
                modelToAnimate.gameObject.SetActive(false);

            SpawnFoodItem();
        }
        else
        {
            // --- STEP 2: COLLECT ITEM ---
            // If you want sound on collection too, call PlaySound() here as well!
            // PlaySound(); 

            if (spawnedFoodItem != null)
            {
                spawnedFoodItem.CollectItem(); 
            }
            
            Destroy(gameObject, 0.5f);
        }
    }

    private void PlaySound()
    {
        if (audioSource != null && tapSound != null)
        {
            audioSource.PlayOneShot(tapSound);
        }
        else
        {
            Debug.LogWarning("AudioSource or TapSound is MISSING on PackController!");
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
        
        spawnedFoodItem = foodObj.AddComponent<FoodItemController>();
        spawnedFoodItem.Initialize(scanResult);
    }
}