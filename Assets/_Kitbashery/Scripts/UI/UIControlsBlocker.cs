using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// Blocks painting controls when the mouse cursor is over a selectable UI element.
    /// </summary>
    public class UIControlsBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public PaintableObject paintable;

        public void Awake()
        {
            if(paintable == null)
            {
                paintable = GameObject.FindGameObjectsWithTag("Paintable")[0].GetComponent<PaintableObject>();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            paintable.lockControls = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            paintable.lockControls = false;
        }
    }
}