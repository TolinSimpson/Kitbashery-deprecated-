using System;
using System.Collections.Generic;
using System.IO;
using Assimp;
using UnityEngine;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;

public class KitbasheryMeshImporter : MonoBehaviour
{
    [HideInInspector]
    public List<GameObject> imports = new List<GameObject>();
    [HideInInspector]
    public List<MeshFilter> filters = new List<MeshFilter>();

    [Space]
    public MeshInspector meshInspector;
    public ImportUIManager importUI;
    public BuildModeUI buildControls;

    public Material defaultMaterial;

    public enum kitbasheryUIMode { import, inspector, buildmode }
    public kitbasheryUIMode mode;


    #region Import Functions:

    public void ImportSingle(string path)
    {
        GameObject import = Load(path);
        if(import != null)
        {
            imports.Add(import);
            if (import.transform.childCount > 0)
            {
                filters.Add(import.GetComponentInChildren<MeshFilter>());
            }
            else
            {
                filters.Add(import.GetComponent<MeshFilter>());
            }

            UpdateKitbasheryUI();
        }
        else
        {
            Debug.LogError("Import Failed");
        }
    }

    public void ImportMultiple(string[] paths)
    {
        foreach (string path in paths)
        {
            GameObject import = Load(path);
            imports.Add(import);
            foreach (MeshFilter filter in import.GetComponentsInChildren<MeshFilter>())//Note: may be able to add to the filters list in the Load() function instead.
            {
                filters.Add(filter);
            }
        }

        UpdateKitbasheryUI();
    }

    #endregion

    /// <summary>
    /// Preforms UI actions once the import is complete.
    /// </summary>
    private void UpdateKitbasheryUI()
    {
        switch (mode)
        {
            case kitbasheryUIMode.import:

                if (imports.Count > 0)
                {
                    //Make sure all imported meshes are hidden and set to the world origin and have a valid name:
                    foreach (MeshFilter filter in filters)
                    {
                        filter.transform.parent = null;
                        filter.gameObject.SetActive(false);
                        MeshTransformToWorldOrigin(filter);

                        //Make sure no periods snuck in the name from children objects. (*Cough blender export* *Cough* *Cough*)
                        if (filter.gameObject.name.Contains("."))
                        {
                            filter.gameObject.name = filter.gameObject.name.Replace(".", "_");
                        }

                        importUI.imports.Add(filter.gameObject);
                    }

                    //Set current import:
                    importUI.current = filters[0].gameObject;
                    importUI.current.SetActive(true);
                    importUI.meshName.text = importUI.current.name;
                    importUI.index = (importUI.importIndex + 1).ToString();
                    importUI.importCount.text = "Import " + importUI.index + " of " + importUI.imports.Count;

                    ClearImports();
                }

                break;

            case kitbasheryUIMode.inspector:

                meshInspector.filter.sharedMesh = filters[0].sharedMesh;
                ClearImports();

                meshInspector.vertCount.text = "Vertices: " + meshInspector.filter.sharedMesh.vertexCount + " | Triangles: " + meshInspector.filter.sharedMesh.triangles.Length;

                meshInspector.browserUI.SetActive(false);
                meshInspector.orbitCam.enabled = true;
                meshInspector.ViewDefault();


                break;

            case kitbasheryUIMode.buildmode:

                buildControls.currentImport = imports[0];
                ClearImports();

                break;
        }
    }

    public void ClearImports()
    {
        if (imports.Count > 0)
        {
            for (int i = 0; i < imports.Count; i++)
            {
                GameObject import = imports[i];
                imports.Remove(import);
                Destroy(import);
            }
            filters.Clear();
        }
    }

    /// <summary>
    /// Sets the mesh's position and pivot to the world origin and freezes the parent transform.
    /// </summary>
    /// <param name="filter"></param>
    public void MeshTransformToWorldOrigin(MeshFilter filter)//Note: Is there an assimp post process for this?
    {
        //Correct scale:
        filter.transform.localScale = Vector3.one;

        //If the mesh is not centered to the world origin:
        Vector3 center = filter.transform.TransformPoint(filter.sharedMesh.bounds.center);
        if (center != Vector3.zero)
        {
            UnityEngine.Mesh newMesh = filter.sharedMesh;
            var vertices = newMesh.vertices;

            //Center transform pivot and force to world origin:
            Vector3 dir = (filter.transform.position - center);
            filter.transform.position = center;
            filter.transform.position -= filter.transform.TransformPoint(filter.transform.position);

            //Assign offset mesh:
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = filter.transform.TransformPoint(vertices[i]);
            }
            newMesh.vertices = vertices;

            //Clear transform:
            filter.transform.position = Vector3.zero;

            //Assign new mesh:
            filter.sharedMesh = newMesh;
            filter.sharedMesh.RecalculateBounds();

            //Center transform over new mesh:
            center = filter.transform.TransformPoint(filter.sharedMesh.bounds.center);
            filter.transform.position = center;
            filter.transform.position -= filter.transform.TransformPoint(filter.transform.position);
            filter.transform.position = Vector3.zero;
        }
    }

    #region Unity Mesh Importer:

    /* 
 *  MIT License
 *  
 *  Copyright (c) 2019 UnityMeshImporter - Dongho Kang
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */

    /*CHANGES BY KITBASHERY:
     * 
        -Enabled realtime postprocess by default.
        -Disabled loading textures by default.
        -Changed finding standard shader with a material definded in the editor.
        -Removed redundant GetComponent calls during component creation.
        -Disabled recive shadows.
    */


    private GameObject Load(string meshPath, float scaleX = 1, float scaleY = 1, float scaleZ = 1, bool loadTextures = false)
    {
        if (!File.Exists(meshPath))
            return null;

        AssimpContext importer = new AssimpContext();
        Scene scene = importer.ImportFile(meshPath, (PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.Triangulate | PostProcessSteps.SortByPrimitiveType));
        scene.SceneFlags = new SceneFlags();

        if (scene == null)
            return null;

        string parentDir = Directory.GetParent(meshPath).FullName;

        // Materials
        List<UnityEngine.Material> uMaterials = new List<Material>();
        if (scene.HasMaterials)
        {
            foreach (var m in scene.Materials)
            {
                UnityEngine.Material uMaterial = defaultMaterial;//new UnityEngine.Material(Shader.Find("Standard"));

                if(loadTextures == true)
                {
                    // Albedo
                    if (m.HasColorDiffuse)
                    {
                        Color color = new Color(
                            m.ColorDiffuse.R,
                            m.ColorDiffuse.G,
                            m.ColorDiffuse.B,
                            m.ColorDiffuse.A
                        );
                        uMaterial.color = color;
                    }

                    // Emission
                    if (m.HasColorEmissive)
                    {
                        Color color = new Color(
                            m.ColorEmissive.R,
                            m.ColorEmissive.G,
                            m.ColorEmissive.B,
                            m.ColorEmissive.A
                        );
                        uMaterial.SetColor("_EmissionColor", color);
                        uMaterial.EnableKeyword("_EMISSION");
                    }

                    // Reflectivity
                    if (m.HasReflectivity)
                    {
                        uMaterial.SetFloat("_Glossiness", m.Reflectivity);
                    }

                    // Texture
                    if (m.HasTextureDiffuse)
                    {
                        Texture2D uTexture = new Texture2D(2, 2);
                        string texturePath = Path.Combine(parentDir, m.TextureDiffuse.FilePath);

                        byte[] byteArray = File.ReadAllBytes(texturePath);
                        bool isLoaded = uTexture.LoadImage(byteArray);
                        if (!isLoaded)
                        {
                            throw new Exception("Cannot find texture file: " + texturePath);
                        }

                        uMaterial.SetTexture("_MainTex", uTexture);
                    }
                }
              
                uMaterials.Add(uMaterial);
            }
        }

        // Mesh
        List<MeshMaterialBinding> uMeshAndMats = new List<MeshMaterialBinding>();
        if (scene.HasMeshes)
        {
            foreach (var m in scene.Meshes)
            {
                List<Vector3> uVertices = new List<Vector3>();
                List<Vector3> uNormals = new List<Vector3>();
                List<Vector2> uUv = new List<Vector2>();
                List<int> uIndices = new List<int>();

                // Vertices
                if (m.HasVertices)
                {
                    foreach (var v in m.Vertices)
                    {
                        uVertices.Add(new Vector3(-v.X, v.Y, v.Z));
                    }
                }

                    // Normals
                if (m.HasNormals)
                {
                    foreach (var n in m.Normals)
                    {
                        uNormals.Add(new Vector3(-n.X, n.Y, n.Z));
                    }
                }

                // Triangles
                if (m.HasFaces)
                {
                    foreach (var f in m.Faces)
                    {
                        // Ignore non-triangle faces
                         if (f.IndexCount != 3)
                            continue;

                        uIndices.Add(f.Indices[2]);
                        uIndices.Add(f.Indices[1]);
                        uIndices.Add(f.Indices[0]);

                    }
                }

                // Uv (texture coordinate) 
                if (m.HasTextureCoords(0))
                {
                    foreach (var uv in m.TextureCoordinateChannels[0])
                    {
                        uUv.Add(new Vector2(uv.X, uv.Y));
                    }
                }

                UnityEngine.Mesh uMesh = new UnityEngine.Mesh();
                uMesh.vertices = uVertices.ToArray();
                uMesh.normals = uNormals.ToArray();
                uMesh.triangles = uIndices.ToArray();
                uMesh.uv = uUv.ToArray();

                uMeshAndMats.Add(new MeshMaterialBinding(m.Name, uMesh, uMaterials[m.MaterialIndex]));
            }
        }

        // Create GameObjects from nodes
        GameObject NodeToGameObject(Node node)
        {
            GameObject uOb = new GameObject(node.Name);

            // Set Mesh
            if (node.HasMeshes)
            {
                foreach (var mIdx in node.MeshIndices)
                {
                    var uMeshAndMat = uMeshAndMats[mIdx];

                    GameObject uSubOb = new GameObject(uMeshAndMat.MeshName);
                    MeshFilter filter = uSubOb.AddComponent<MeshFilter>();
                    MeshRenderer rend = uSubOb.AddComponent<MeshRenderer>();
                    uSubOb.AddComponent<MeshCollider>();

                    filter.mesh = uMeshAndMat.Mesh;
                    rend.material = uMeshAndMat.Material;
                    rend.receiveShadows = false;
                    uSubOb.transform.SetParent(uOb.transform, true);
                    uSubOb.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                }
            }

            // Transform
            // Decompose Assimp transform into scale, rot and translaction 
            Assimp.Vector3D aScale = new Assimp.Vector3D();
            Assimp.Quaternion aQuat = new Assimp.Quaternion();
            Assimp.Vector3D aTranslation = new Assimp.Vector3D();
            node.Transform.Decompose(out aScale, out aQuat, out aTranslation);

            // Convert Assimp transfrom into Unity transform and set transformation of game object 
            UnityEngine.Quaternion uQuat = new UnityEngine.Quaternion(aQuat.X, aQuat.Y, aQuat.Z, aQuat.W);
            var euler = uQuat.eulerAngles;
            uOb.transform.localScale = new UnityEngine.Vector3(aScale.X, aScale.Y, aScale.Z);
            uOb.transform.localPosition = new UnityEngine.Vector3(aTranslation.X, aTranslation.Y, aTranslation.Z);
            uOb.transform.localRotation = UnityEngine.Quaternion.Euler(euler.x, -euler.y, euler.z);

            if (node.HasChildren)
            {
                foreach (var cn in node.Children)
                {
                    var uObChild = NodeToGameObject(cn);
                    uObChild.transform.SetParent(uOb.transform, false);
                }
            }

            return uOb;
        }

        return NodeToGameObject(scene.RootNode); ;
    }

    #endregion
}


class MeshMaterialBinding
{
    private string meshName;
    private UnityEngine.Mesh mesh;
    private UnityEngine.Material material;

    private MeshMaterialBinding() { }    // Do not allow default constructor

    public MeshMaterialBinding(string meshName, Mesh mesh, Material material)
    {
        this.meshName = meshName;
        this.mesh = mesh;
        this.material = material;
    }

    public Mesh Mesh { get => mesh; }
    public Material Material { get => material; }
    public string MeshName { get => meshName; }
}