using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MyScrollGridElement : PooledScrollGridElement
{
    int myID;

    public override void OnGridPositionChange(int newIndex)
    {
        GetComponent<Image>().sprite = SpriteManager.instance.GetSprite(newIndex.ToString());
        GetComponentInChildren<Text>().text = newIndex.ToString();
        myID = newIndex;
    }

    public void OnClick()
    {
        Debug.Log("Thumbnail clicked: " + myID);
    }
}
