using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemEntry
{
    [SerializeField] private int _itemId;
    [SerializeField] private string _codeName;

    public int ItemId => _itemId;
    public string CodeName => _codeName;
}

// TODO: 서버 연결 전까지 쓰는 임시 데이터. 원본은 SlimeTD-Data의 Items.csv다
[CreateAssetMenu(fileName = "ItemCatalog", menuName = "SlimeTD/Item Catalog")]
public class ItemCatalog : ScriptableObject
{
    public const string Coin = "Coin";
    public const string Diamond = "Diamond";
    public const string PackTicket = "PackTicket";
    public const string Energy = "Energy";

    [SerializeField] private List<ItemEntry> _items = new List<ItemEntry>();

    public IReadOnlyList<ItemEntry> Items => _items;

    public bool TryGetItemId(string codeName, out int itemId)
    {
        foreach (var item in _items)
        {
            if (item.CodeName == codeName)
            {
                itemId = item.ItemId;
                return true;
            }
        }

        itemId = 0;
        return false;
    }

    public string GetCodeName(int itemId)
    {
        foreach (var item in _items)
        {
            if (item.ItemId == itemId)
                return item.CodeName;
        }

        return null;
    }
}