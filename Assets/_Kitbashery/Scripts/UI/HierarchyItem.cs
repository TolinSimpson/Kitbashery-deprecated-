using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// A UI item in the hierarchy of the Kitbash in build mode.
    /// </summary>
    public class HierarchyItem : MonoBehaviour
    {
        public TMP_Text itemName;

        public KitbashPart part;
        [HideInInspector]
        public BuildModeUI buildControls;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ToggleSelection()
        {
            /*if(buildControls.selectedParts.Contains(part))
            {
               // buildControls.DeselectParts();
            }
            else
            {
                buildControls.selectedParts.Add(part);
            }*/
        }

        public void SetText()
        {
            itemName.text = part.name;
        }
    }

}