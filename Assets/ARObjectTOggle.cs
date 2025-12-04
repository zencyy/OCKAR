using UnityEngine;
using UnityEngine.Rendering;

public class ARObjectTOggle : MonoBehaviour
{
    [SerializeField]
    private MeshRenderer meshRendererToToggle;
    
    public void ToggleMeshRenderer()
    {
        meshRendererToToggle.enabled = !meshRendererToToggle.enabled;

    }

    // Update is called once per frame
    
}
