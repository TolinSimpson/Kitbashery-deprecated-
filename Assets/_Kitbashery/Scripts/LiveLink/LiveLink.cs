using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Sends a json filepath for a mesh over the local port our live link integrations listen for.
/// </summary>
public class LiveLink : MonoBehaviour
{
    /// <summary>
    /// An arbitrary port number, livelink integrations listen in on this port, they must match.
    /// </summary>
    [Tooltip("An arbitrary port number, external livelink integrations listen in on this port, they must match.")]
    public int port = 26738;

    /// <summary>
    /// The path of the mesh to send over the port.
    /// </summary>
    [HideInInspector]
    public string meshPath = "";

    /// <summary>
    /// Sends the path of the mesh we want to be picked up by the live link plugins as a json formatted string over the locals host. (Port numbers must match in both softwares).
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public IEnumerator Link()
    {
        WWWForm form = new WWWForm();
        form.AddField("jsonData", "{ " + @"""" + "meshPath" + @"""" + ": " + @"""" + meshPath + @"""" + " }");

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost:[" + port + "]", form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Debug.Log("Form upload complete!");
            }
        }
    }

    /// <summary>
    /// Debugs what the json output of our mesh path looks like, useful when creating livelink integrations.
    /// </summary>
    /// <param name="item"></param>
    public void DebugJson(string meshPath)
    {
        Debug.Log("{ " + @"""" + "meshPath" + @"""" + ": " + @"""" + meshPath + @"""" + " }");
    }
}
