using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI buttons managed by the <see cref="BrowserManager"/> that represent .obj files in the StreamingAssets folder structure.
/// </summary>
[Serializable]
public class BrowserItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public string thumbPath;

    public string meshPath;

    public RawImage thumbnail;

    public GameObject tooltip;

    public TMP_Text tooltipText;

    [HideInInspector]
    public BrowserManager manager;

    private void Start()
    {
        ToggleTooltip();
    }

    public void ToggleTooltip()
    {
        tooltip.SetActive(!tooltip.activeSelf);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ToggleTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ToggleTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(manager.selectedItem != this)
        {
            manager.SelectItem(this);
        }
        else
        {
            manager.DeselectItem();
        }
    }
}
