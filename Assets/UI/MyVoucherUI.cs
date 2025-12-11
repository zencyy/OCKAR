using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoucherUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI codeText; // The unique code (e.g., OCK-X7Y2)
    [SerializeField] private TextMeshProUGUI dateText;

    public void Setup(string description, string code, string date, Sprite icon)
    {
        descText.text = description;
        codeText.text = code;
        if(dateText != null) dateText.text = "Acquired: " + date;
        if(iconImage != null) iconImage.sprite = icon;
    }
}