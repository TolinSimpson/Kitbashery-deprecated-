using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kitbashery
{
    /// <summary>
    /// Manages the menu popup when the escape button is pressed.
    /// </summary>
    public class EscapeMenuManager : MonoBehaviour
    {

        public GameObject escapeMenuPopup;

        public GameObject saveKitbashPopup;

        public BuildModeUI buildControls;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                escapeMenuPopup.SetActive(!escapeMenuPopup.activeSelf);
            }
        }

        public void QuitApplication()
        {
            if(buildControls.kitbash.transform.childCount > 0)
            {
                escapeMenuPopup.SetActive(false);
                saveKitbashPopup.SetActive(true);
            }
            else
            {
                Application.Quit();
            }
        }
    }

}