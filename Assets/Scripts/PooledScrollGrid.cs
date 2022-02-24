using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PooledScrollGrid : MonoBehaviour
{
    [SerializeField] GameObject pooledElementPrefab = null;
    [SerializeField, Min(1)] int elementsCount = 10;
    [SerializeField, Min(1)] int columns = 5;
    [SerializeField, Min(1)] float CellSizeX = 200f;
    [SerializeField, Min(1)] float CellSizeY = 200f;
    [SerializeField, Min(0)] float SpacingX = 20f;
    [SerializeField, Min(0)] float SpacingY = 20f;

    Deque<PooledScrollGridElement> activeElements = new Deque<PooledScrollGridElement>();
    int currentMinRowId;
    int currentMaxRowId;
    Stack<PooledScrollGridElement> availableElements = new Stack<PooledScrollGridElement>();
    float currentCellSizeX;
    float currentCellSizeY;
    float currentSpacingX;
    float currentSpacingY;
    int currentColumns;
    int currentElements = -1;

    ScrollRect scrollRect;
    RectTransform scrollRectTransform;
    private void Awake()
    {
        scrollRect = GetComponentInChildren<ScrollRect>();
        scrollRectTransform = scrollRect.GetComponent<RectTransform>();
    }

    PooledScrollGridElement CreateNewPoolElement()
    {
        GameObject gameObj = Instantiate(pooledElementPrefab);
        gameObj.transform.SetParent(scrollRect.content, false);
        return gameObj.GetComponent<PooledScrollGridElement>();
    }

    void SetPoolElementPositionFromId(PooledScrollGridElement element, int id)
    {
        RectTransform rt = element.gameObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, CellSizeX);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, CellSizeY);
        int row = id / columns;
        int column = id % columns;
        rt.anchoredPosition = new Vector2(column * (CellSizeX + SpacingX), -row * (CellSizeY + SpacingY));
        element.OnGridPositionChange(id);
        element.gameObject.SetActive(true);
    }

    private void Update()
    {
        // reset pool if anything changes
        if ((currentColumns != columns || currentElements != elementsCount || currentCellSizeX != CellSizeX || currentCellSizeY != CellSizeY || currentSpacingX != SpacingX || currentSpacingY != SpacingY) && activeElements.Count > 0)
        {
            while (!activeElements.IsEmpty)
            {
                PooledScrollGridElement element = activeElements.RemoveBack();
                element.gameObject.SetActive(false);
                availableElements.Push(element);
            }
            currentColumns = columns;
            int maxElements = SpriteManager.instance.GetSpriteCount();
            if (elementsCount > maxElements)
            {
                Debug.LogWarning("Out of sprites, max count: " + maxElements + ". Requested: " + elementsCount);
                elementsCount = maxElements;
            }
            currentElements = elementsCount;
            currentMaxRowId = -1;
            currentMinRowId = -1;
            currentCellSizeX = CellSizeX;
            currentCellSizeY = CellSizeY;
            currentSpacingX = SpacingX;
            currentSpacingY = SpacingY;
        }

        float windowHeight = scrollRectTransform.GetHeight();
        float windowWidth = scrollRectTransform.GetWidth();
        float scrollPosition = scrollRect.content.offsetMax.y;

        // update content rect to reflect possible window size changes
        int rowsCount = (int)Mathf.Ceil((float)elementsCount / columns);
        float requiredContentHeight = CellSizeY * rowsCount + SpacingY * (rowsCount - 1) - windowHeight;
        scrollRect.content.SetBottom(-requiredContentHeight + scrollPosition);
        float requiredContentWidth = CellSizeX * columns + SpacingX * (columns - 1);
        float sideOffset = (windowWidth - requiredContentWidth) * 0.5f;
        scrollRect.content.SetLeft(sideOffset);
        scrollRect.content.SetRight(sideOffset);

        // calculate visible rows
        int minVisibleRowId = (int)Mathf.Ceil((Mathf.Max(0f, scrollPosition) + SpacingY) / (CellSizeY + SpacingY)) - 1;
        minVisibleRowId = Mathf.Max(minVisibleRowId, 0);
        int maxVisibleRowId = (int)Mathf.Floor((Mathf.Max(0f, scrollPosition) + windowHeight) / (CellSizeY + SpacingY));
        maxVisibleRowId = Mathf.Min(maxVisibleRowId, rowsCount - 1);
        
        // deactivate all invisible elements and return them to pool
        if (currentMinRowId != -1 && currentMaxRowId != -1)
        {
            while (currentMinRowId < minVisibleRowId)
            {
                int columnsToClear = Mathf.Min(activeElements.Count, columns);
                for (int i = 0; i < columnsToClear; i++)
                {
                    PooledScrollGridElement element = activeElements.RemoveFront();
                    element.gameObject.SetActive(false);
                    availableElements.Push(element);
                }
                currentMinRowId++;
            }
            
            while (currentMaxRowId > maxVisibleRowId)
            {
                if (activeElements.Count > 0)
                {
                    int unevenElements = activeElements.Count % columns;
                    int columnsToClear = unevenElements == 0 ? columns : unevenElements;
                    for (int i = 0; i < columnsToClear; i++)
                    {
                        PooledScrollGridElement element = activeElements.RemoveBack();
                        element.gameObject.SetActive(false);
                        availableElements.Push(element);
                    }
                }
                currentMaxRowId--;
            }
        }

        // add all elements that should be visible
        if (currentMinRowId == -1)
        {
            for (int i = minVisibleRowId; i <= maxVisibleRowId; i++)
            {
                for (int j = 0; j < columns && i * columns + j < elementsCount; j++)
                {
                    PooledScrollGridElement element = availableElements.Count > 0 ? availableElements.Pop() : CreateNewPoolElement();
                    SetPoolElementPositionFromId(element, i * columns + j);
                    activeElements.AddBack(element);
                }
            }
            currentMinRowId = minVisibleRowId;
            currentMaxRowId = maxVisibleRowId;
        }
        else
        {
            for (int i = currentMinRowId - 1; i >= minVisibleRowId; i--)
            {
                for (int j = 0; j < columns && i * columns + j < elementsCount; j++)
                {
                    PooledScrollGridElement element = availableElements.Count > 0 ? availableElements.Pop() : CreateNewPoolElement();
                    SetPoolElementPositionFromId(element, i * columns + j);
                    activeElements.AddFront(element);
                }
                currentMinRowId--;
            }

            for (int i = currentMaxRowId + 1; i <= maxVisibleRowId; i++)
            {
                for (int j = 0; j < columns && i * columns + j < elementsCount; j++)
                {
                    PooledScrollGridElement element = availableElements.Count > 0 ? availableElements.Pop() : CreateNewPoolElement();
                    SetPoolElementPositionFromId(element, i * columns + j);
                    activeElements.AddBack(element);
                }
                currentMaxRowId++;
            }
        }
    }
}
