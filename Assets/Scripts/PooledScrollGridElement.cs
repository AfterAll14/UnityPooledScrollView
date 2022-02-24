using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledScrollGridElement : MonoBehaviour
{
    virtual public void OnGridPositionChange(int newIndex) { } // this is gonna be used to reload content of pooled element in child classes
}
