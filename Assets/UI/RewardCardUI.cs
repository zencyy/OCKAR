using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class RewardCardUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI rewardDescriptionText; // "$5 Off Voucher"
    [SerializeField] private TextMeshProUGUI progressText; // "5/10"
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Button claimButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private GameObject claimedOverlay; // Visual for "Already Claimed"

    private string _itemId;
    private Action<string> _onClaimAction;

    public void Setup(ItemModel item, int currentCount, bool isClaimed, Action<string> onClaimClicked)
    {
        _itemId = item.itemId;
        _onClaimAction = onClaimClicked;

        // Visuals
        if (itemIcon != null) itemIcon.sprite = item.itemSprite;
        if (rewardDescriptionText != null) rewardDescriptionText.text = item.rewardDescription;

        // Math
        int required = item.rewardThreshold;
        float progress = Mathf.Clamp01((float)currentCount / required);

        // Progress Bar
        if (progressSlider != null) progressSlider.value = progress;
        if (progressText != null) progressText.text = $"{currentCount}/{required}";

        // Button Logic
        if (isClaimed)
        {
            // State: Already Claimed
            claimButton.interactable = false;
            buttonText.text = "Claimed";
            if (claimedOverlay != null) claimedOverlay.SetActive(true);
        }
        else if (currentCount >= required)
        {
            // State: Ready to Claim
            claimButton.interactable = true;
            buttonText.text = "CLAIM NOW";
            if (claimedOverlay != null) claimedOverlay.SetActive(false);
            
            // Add listener
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => _onClaimAction?.Invoke(_itemId));
        }
        else
        {
            // State: Locked / In Progress
            claimButton.interactable = false;
            buttonText.text = "Locked";
            if (claimedOverlay != null) claimedOverlay.SetActive(false);
        }
    }
}