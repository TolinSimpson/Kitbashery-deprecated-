using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class PluginInstaller : MonoBehaviour
{
    public GameObject pluginInstallPopup;

    public TMP_Dropdown pluginsDropdown;

    public GameObject installCompletePopup;

    public TMP_Text installCompleteText;

    public void ShowConfigurePluginsPopup()
    {
        pluginInstallPopup.SetActive(true);
    }

    public void InstallPlugin()
    {
        if (pluginsDropdown.value == 0)
        {
            //Set default install location for each platform:
            //See: https://docs.unity3d.com/Manual/PlatformDependentCompilation.html

            //Blender Ref:https://docs.blender.org/manual/en/latest/advanced/blender_directory_layout.html

#if UNITY_STANDALONE_WIN

           string installPath = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\Roaming\Blender Foundation\Blender\2.83\scripts\addons\KitbasheryBlenderLiveLink.py");
#endif

#if UNITY_STANDALONE_OSX
            string installPath = @"\Users\$USER\LibraryApplication Support\Blender\2.83\scripts\addons\KitbasheryBlenderLiveLink.py";
#endif

#if UNITY_STANDALON_LINUX
            string installPath = @"%USERPROFILE%\AppData\Roaming\Blender Foundation\Blender\2.83\scripts\addons\KitbasheryBlenderLiveLink.py";
#endif
            Debug.Log(installPath);
            if(Directory.Exists(Path.GetDirectoryName(installPath)))
            {
                File.Copy(Application.streamingAssetsPath + "/LiveLinkPlugins/KitbasheryBlenderLiveLink.py", installPath, true);
                installCompleteText.text = "Successfully installed the Kitbashery LiveLink Add-on for Blender 2.83. Be sure to enable the plugin in Blender";
            }
            else
            {
                installCompleteText.text = "Failed to install the Kitbashery LiveLink Add-on for Blender 2.83. Be sure Blender 2.83 is installed.";
            }
        }

        pluginInstallPopup.SetActive(false);
        installCompletePopup.SetActive(true);
    }
}