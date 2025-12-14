using System.Collections;
using UnityEngine;

public class FoodItemController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float spawnScale = 0.01f;
    [SerializeField] private float targetScale = 0.05f;
    [SerializeField] private float scaleSpeed = 2f;
    [SerializeField] private float rotationSpeed = 60f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip collectSound; // <-- DRAG YOUR SOUND HERE

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem spawnEffect;
    [SerializeField] private ParticleSystem rarityEffect;
    
    private ScanResult scanResult;
    private ItemModel itemModel;
    private bool isCollecting = false;
    
    public void Initialize(ScanResult result)
    {
        scanResult = result;
        itemModel = ItemDatabase.Instance.GetItem(result.itemId);
        
        // --- AUTO-SETUP AUDIO SOURCE ---
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        // Ensure sound is 2D (heard clearly)
        audioSource.spatialBlend = 0.0f;
        audioSource.playOnAwake = false;
        // -------------------------------

        // Start at small scale
        transform.localScale = Vector3.one * spawnScale;
        
        // Show spawn animation
        StartCoroutine(SpawnAnimation());
        
        // Show notification immediately
        ShowItemNotification();
    }
    
    private IEnumerator SpawnAnimation()
    {
        // Create particle effect
        CreateRarityEffect();
        
        // Scale up animation
        float currentScale = spawnScale;
        while (currentScale < targetScale)
        {
            currentScale += Time.deltaTime * scaleSpeed;
            transform.localScale = Vector3.one * currentScale;
            
            // Rotate while scaling
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        transform.localScale = Vector3.one * targetScale;
        
        // Wait Indefinitely until user taps again
        while (!isCollecting)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }
    
    // Called by PackController when user taps the second time
    public void CollectItem()
    {
        if (isCollecting) return;
        
        isCollecting = true;
        
        // --- PLAY SOUND HERE ---
        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
        // -----------------------

        // Trigger the fly away animation
        StartCoroutine(CollectAnimation());
    }
    
    private IEnumerator CollectAnimation()
    {
        // Fly up and shrink
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Move up
            transform.position = startPos + Vector3.up * progress * 0.5f;
            
            // Shrink
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
            
            // Spin faster
            transform.Rotate(Vector3.up, rotationSpeed * 5f * Time.deltaTime);
            
            yield return null;
        }
        
        // Destroy
        Destroy(gameObject);
    }
    
    private void CreateRarityEffect()
    {
        if (rarityEffect != null && itemModel != null)
        {
            ParticleSystem effect = Instantiate(rarityEffect, transform.position, Quaternion.identity);
            effect.transform.parent = transform;
            
            var main = effect.main;
            main.startColor = itemModel.GetRarityColor();
            
            effect.Play();
            Destroy(effect.gameObject, 3f);
        }
    }
    
    private void ShowItemNotification()
    {
        if (scanResult != null && UIManager.Instance != null)
        {
            UIManager.Instance.ShowCollectionNotification(scanResult);
        }
    }
}