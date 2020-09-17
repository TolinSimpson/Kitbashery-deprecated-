using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    public class UIStartPosition : MonoBehaviour
    {
        public RectTransform rectTrans;
        public Vector3 startPosition;

        private void Awake()
        {
            rectTrans.right =
            transform.position = startPosition;
        }
    }
}
