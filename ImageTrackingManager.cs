using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ImageTrackingManager : MonoBehaviour
{
    private ARTrackedImageManager trackedImageManager;
    public GameObject curryPuffPrefab;

    private Dictionary<string, GameObject> spawnedObjects = new();

    void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    [System.Obsolete]
    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    [System.Obsolete]
    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    [System.Obsolete]
    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            SpawnModel(trackedImage);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                UpdateModelPosition(trackedImage);
            }
        }

        foreach (var trackedImage in eventArgs.removed)
        {
            if (spawnedObjects.TryGetValue(trackedImage.referenceImage.name, out var obj))
                obj.SetActive(false);
        }
    }

    void SpawnModel(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (!spawnedObjects.ContainsKey(imageName))
        {
            GameObject newObj = Instantiate(curryPuffPrefab, trackedImage.transform.position, Quaternion.identity);
            newObj.name = imageName;
            spawnedObjects[imageName] = newObj;
        }
        else
        {
            spawnedObjects[imageName].SetActive(true);
        }
    }

    void UpdateModelPosition(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        if (spawnedObjects.TryGetValue(imageName, out var obj))
        {
            obj.transform.position = trackedImage.transform.position;
        }
    }
}
