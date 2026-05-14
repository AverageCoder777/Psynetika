using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class Shop : MonoBehaviour, IInteractable
{
    [SerializeField] private ShopList _shopList;
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private Inventory _inventory;
    [Header("UI")]
    [SerializeField] private GameObject _UIShop;
    [SerializeField] private Image _iconSprite;
    [SerializeField] private TMP_Text _name_item;
    [SerializeField] private TMP_Text _description_item;
    [SerializeField] private TMP_Text _cost_item;

    private int _iItem = 0;

    public void Awake()
    {
        ShowItem();
        _UIShop.SetActive(false);
    }

    public void Interact()
    {
        _UIShop.SetActive(true);
        Time.timeScale = 0;
        _playerInput.SwitchCurrentActionMap("UI");
        
    }

    public void ShowItem()
    {
        var Item = _shopList.ShoppingItems[_iItem];

        _iconSprite.sprite = Item.ability != null ? Item.ability.icon : null;
        _name_item.text = Item.ability != null ? Item.ability.displayName : string.Empty;
        _description_item.text = Item.ability != null ? Item.ability.description : string.Empty;
        _cost_item.text = $"{Item.amount}";
    }

    public void NextItem()
    {
        _iItem++;
        if (_iItem >= _shopList.ShoppingItems.Count) 
            _iItem = 0;
        ShowItem();
    }

    public void PreviousItem()
    {
        _iItem--;
        if (_iItem < 0)
            _iItem = _shopList.ShoppingItems.Count - 1;
        ShowItem();
    }

    public void ExitShop()
    {
        Time.timeScale = 1;
        _playerInput.SwitchCurrentActionMap("Player");
        _UIShop.SetActive(false);
    }

    public void BuyItem()
    {
        var item = _shopList.ShoppingItems[_iItem];
        if (item.ability == null)
        {
            Debug.LogWarning("Shop item ability is null");
            return;
        }

        if (_inventory.coins < item.amount)
        {
            Debug.Log("Not enough coins");
            return;
        }
        
        _inventory.RemoveCoins(item.amount);
        _inventory.ChangeAbility(item.ability, item.owner, item.slot);
    }
    
    
}
