using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System;

//Original from the Unity Community Wiki: http://wiki.unity3d.com/index.php?title=ExportOBJ by DaveA, KeliHlodversson, tgraupmann, drobe

//Kitbashery: 
//-Class rename to OBJExport
//Removed redundant code
//Removed UnityEditor dependancy
//Merged Start() and Stop() into ResetIndex()
//Renamed ambiguous variables
//Imporved error checking
//Added normal flipper

/// <summary>
/// Exports .obj files.
/// </summary>
public static class OBJExport
{
    private static int startIndex = 0;

    private static void ResetIndex()
    {
        startIndex = 0;
    }

    public static void Export(GameObject go, string path)
    {
        if (go != null)
        {
            string meshName = go.name;
            string filePath = path + "/" + meshName + ".obj";

            ResetIndex();

            StringBuilder meshString = new StringBuilder();

            meshString.Append("#" + meshName + ".obj"
                                + "\n#" + DateTime.Now.ToLongDateString()
                                + "\n#" + DateTime.Now.ToLongTimeString()
                                + "\n#-------"
                                + "\n\n");

            Transform t = go.transform;

            Vector3 originalPosition = t.position;
            t.position = Vector3.zero;

            meshString.Append("g ").Append(t.name).Append("\n");
            meshString.Append(processTransform(t));

            WriteToFile(meshString.ToString(), filePath);

            t.position = originalPosition;

            ResetIndex();
            //Debug.Log("Exported Mesh: " + filePath);

/*#if UNITY_EDITOR
            filePath = "Assets/StreamingAssets/Mo" + filePath.TrimStart(Application.streamingAssetsPath.ToCharArray());
            UnityEditor.AssetDatabase.ImportAsset(filePath);
            UnityEditor.AssetDatabase.WriteImportSettingsIfDirty(filePath);
            UnityEditor.AssetDatabase.SaveAssets();
#endif*/
        }
        else
        {
            Debug.LogError("Export failed: GameObject cannot be null.");
        }
    }


    public static string MeshToString(MeshFilter mf, Transform t)//TODO: keep quads, may have to get verts from the original file instead of the imported unity mesh.
    {
        Vector3 s = t.localScale;
        Vector3 p = t.localPosition;
        Quaternion r = t.localRotation;


        int numVertices = 0;
        Mesh m = FlipNormals(mf.sharedMesh, true);//Flip normals because for whatever reason there are always inverted.
        if (!m)
        {
            return "####Error####";
        }
        Material[] mats = mf.gameObject.GetComponent<MeshRenderer>().sharedMaterials;

        StringBuilder sb = new StringBuilder();

        foreach (Vector3 vv in m.vertices)
        {
            Vector3 v = t.TransformPoint(vv);
            numVertices++;
            sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, -v.z));
        }
        sb.Append("\n");
        foreach (Vector3 nn in m.normals)
        {
            Vector3 v = r * nn;
            sb.Append(string.Format("vn {0} {1} {2}\n", -v.x, -v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.uv)
        {
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }
        for (int material = 0; material < m.subMeshCount; material++)
        {
            sb.Append("\n");
            sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            sb.Append("usemap ").Append(mats[material].name).Append("\n");

            int[] triangles = m.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1 + startIndex, triangles[i + 1] + 1 + startIndex, triangles[i + 2] + 1 + startIndex));
            }
        }

        startIndex += numVertices;
        return sb.ToString();
    }


    private static string processTransform(Transform t)
    {
        StringBuilder meshString = new StringBuilder();

        meshString.Append("#" + t.name
                        + "\n#-------"
                        + "\n");

        MeshFilter mf = t.GetComponent<MeshFilter>();
        if (mf)
        {
            meshString.Append(MeshToString(mf, t));
        }

        for (int i = 0; i < t.childCount; i++)
        {
            meshString.Append(processTransform(t.GetChild(i)));
        }

        return meshString.ToString();
    }

    private static void WriteToFile(string s, string filepath)
    {
        using (StreamWriter sw = new StreamWriter(filepath))
        {
            sw.Write(s);
        }
    }

    /// <summary>
    /// Flips Normals.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="flipTriangles"></param>
    /// <returns></returns>
    public static Mesh FlipNormals(Mesh mesh, bool flipTriangles)
    {
        Mesh newMesh = mesh;

        Vector3[] normals = newMesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }
        newMesh.normals = normals;

        if (flipTriangles == true)
        {
            for (int m = 0; m < newMesh.subMeshCount; m++)
            {
                int[] triangles = newMesh.GetTriangles(m);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int temp = triangles[i];
                    triangles[i] = triangles[i + 1];
                    triangles[i + 1] = temp;
                }
                newMesh.SetTriangles(triangles, m);
            }
        }

        return newMesh;
    }

}