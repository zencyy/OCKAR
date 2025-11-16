using UnityEngine;
using UnityEngine.UI;

public class CurryPuff : MonoBehaviour
{
    private Canvas funFactCanvas;
    private Text funFactText;
    private bool factShown = false;

    [System.Obsolete]
    void Start()
    {
        // Find the Canvas and Text in the scene dynamically
        funFactCanvas = FindObjectOfType<Canvas>(true); // true = include inactive
        funFactText = funFactCanvas.GetComponentInChildren<Text>(true);

        if (funFactCanvas != null)
            funFactCanvas.enabled = false;
    }

    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.IsChildOf(transform) && !factShown)
                {
                    ShowFunFact();
                }
                else if (factShown)
                {
                    HideFunFact();
                }
            }
        }
    }

    void ShowFunFact()
    {
        factShown = true;
        Debug.Log("Fun fact triggered!");

        if (funFactCanvas != null)
        {
            funFactCanvas.enabled = true;
            if (funFactText != null)
            {
                funFactText.text =
                    "🍛 Old Chang Kee began as a humble curry puff stall near Rex Cinema in 1956.\n\n" +
                    "Their golden-crisp pastry and rich curried potato filling became a local favourite!";
            }
        }
    }

    void HideFunFact()
    {
        factShown = false;
        if (funFactCanvas != null)
            funFactCanvas.enabled = false;
    }
}
/*/
*/