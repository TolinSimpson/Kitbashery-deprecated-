using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// Blocks controls when the mouse cursor is over a selectable UI element.
    /// </summary>
    public class UIControlsBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public KB_ObjectManager objectManager;

        public void Awake()
        {
            if(objectManager == null)
            {
                objectManager = GameObject.FindGameObjectsWithTag("Managers")[0].GetComponent<KB_ObjectManager>();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            objectManager.lockControls = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            objectManager.lockControls = false;
        }
    }
}