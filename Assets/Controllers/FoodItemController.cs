using System.Collections;
using UnityEngine;

public class FoodItemController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float spawnScale = 0.01f;
    [SerializeField] private float targetScale = 0.05f;
    [SerializeField] private float scaleSpeed = 2f;
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private float displayDuration = 3f; // How long to show before collecting
    
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
        
        // Start at small scale
        transform.localScale = Vector3.one * spawnScale;
        
        // Show spawn animation
        StartCoroutine(SpawnAnimation());
        
        // Show notification
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
        
        // Continue rotating
        StartCoroutine(RotateItem());
        
        // Auto-collect after display duration
        yield return new WaitForSeconds(displayDuration);
        
        if (!isCollecting)
        {
            CollectItem();
        }
    }
    
    private IEnumerator RotateItem()
    {
        while (!isCollecting)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            yield return null;
        }
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
    
    // User taps to collect early
    private void OnMouseDown()
    {
        if (!isCollecting)
        {
            CollectItem();
        }
    }
    
    private void CollectItem()
    {
        if (isCollecting) return;
        
        isCollecting = true;
        
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
            transform.localScale = startScale * (1f - progress);
            
            // Spin faster
            transform.Rotate(Vector3.up, rotationSpeed * 3f * Time.deltaTime);
            
            yield return null;
        }
        
        // Destroy
        Destroy(gameObject);
    }
}