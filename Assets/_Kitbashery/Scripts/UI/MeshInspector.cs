using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// UI controls for changing the appearence of an inspected mesh.
/// </summary>
public class MeshInspector : MonoBehaviour
{
    public CameraOrbit orbitCam;
    public GameObject browserUI;
    public KitbasheryMeshImporter importer;

    public MeshRenderer rend;
    public MeshFilter filter;


    [Header("Inspector UI:")]
    public TMP_Text vertCount;

    [Header("Materials:")]
    public Material defaultMat;
    public Material wireframeMat;
    public Material uvWireframeMat;
    public Material smoothingMat;
    public Material uvCheckerMat;

    /// <summary>
    /// Tells the manager to show the mesh inspector GUI and loads the mesh.
    /// </summary>
    public void InspectMesh(string meshPath)
    {
        if (!File.Exists(meshPath))
        {
            Debug.LogError("File doesn't exist.");
        }
        else
        {
            importer.mode = KitbasheryMeshImporter.kitbasheryUIMode.inspector;
            importer.ImportSingle(meshPath);
        }
    }

    public void EndMeshInspection()
    {
        orbitCam.enabled = false;
        orbitCam.transform.position = Vector3.zero;
        orbitCam.transform.rotation = Quaternion.identity;
        browserUI.SetActive(true);
        gameObject.SetActive(false);
        filter.sharedMesh = null;

        //Reset cam constraints:
        //orbitCam.transform.position = Vector3.zero;
        //orbitCam.transform.localRotation = Quaternion.identity;
        orbitCam.distance = 5;
        if (orbitCam.mouseYConstraint == 0)
        {
            orbitCam.mouseYConstraint = 360;
        }
    }

    public void ViewDefault()
    {
        SetCamerUVView(false);
        rend.material = defaultMat;
    }

    public void ViewWireframe()
    {
        SetCamerUVView(false);
        rend.material = wireframeMat;
    }

    public void ViewUVWireframe()
    {
        SetCamerUVView(true);
        rend.material = uvWireframeMat;
    }

    public void ViewSmoothing()
    {
        SetCamerUVView(false);
        rend.material = smoothingMat;
    }

    public void ViewUVChecker()
    {
        SetCamerUVView(false);
        rend.material = uvCheckerMat;
    }

    public void SetCamerUVView(bool uvView)
    {
        orbitCam.uvView = uvView;
        orbitCam.orbitCam.orthographic = uvView;

        if(uvView == true)
        {
            orbitCam.mouseYConstraint = 0;
            orbitCam.distance = 0.6f;
            if(filter.sharedMesh.uv.Length < 3)//Note: this might not ever happen if the exporter adds empty UV coords.
            {
                vertCount.text = "Mesh does not have UV0";//Tempting to change this to "Do you even unwrap bro?"
            }
            else
            {
                vertCount.text = "UV0";
            }
          
        }
        else
        {
            if(orbitCam.mouseYConstraint == 0)
            {
                orbitCam.distance = 5;
                orbitCam.mouseYConstraint = 360;
            }
            vertCount.text = "Vertices: " + filter.sharedMesh.vertexCount + " | Triangles: " + (filter.sharedMesh.triangles.Length / 3);
        }

    }
}
