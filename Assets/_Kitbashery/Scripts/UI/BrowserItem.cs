using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// UI buttons managed by the <see cref="BrowserManager"/> that represent .obj files in the StreamingAssets folder structure.
    /// </summary>
    [Serializable]
    public class BrowserItem : MonoBehaviour,  IPointerClickHandler
    {
        public string thumbPath;

        public string meshPath;

        public RawImage thumbnail;

        [HideInInspector]
        public BrowserManager manager;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (manager.selectedItem != this)
            {
                manager.SelectItem(this);
            }
            else
            {
                manager.DeselectItem();
            }
        }
    }

}