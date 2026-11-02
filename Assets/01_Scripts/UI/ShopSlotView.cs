using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ShopSlotView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text bundleCountText;

    private Action selected;

    private void Awake()
    {
        button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(ShopOffer offer, Action onSelected)
    {
        selected = onSelected;

        iconImage.sprite = offer.Item.Icon;
        iconImage.enabled = offer.Item.Icon != null;

        nameText.text = offer.Item.DisplayName;
        priceText.text = offer.UnitPrice.ToString();
        bundleCountText.text = $"{offer.BundleSize}";
        // Keep sold-out items selectable so their stock is visible in details.
        button.interactable = true;
    }

    private void HandleClick() => selected?.Invoke();
}
