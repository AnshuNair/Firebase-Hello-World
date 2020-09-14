using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemData
{
    public bool itemTaken = false;
    public string itemOwner = "";

    public ItemData(string owner)
    {
        itemOwner = owner;
        itemTaken = true;
    }
}
