using System;
using System.Collections.Generic;
using System.IO;
using Assimp;
using UnityEngine;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// Wrapped for Assimp import/export functions.
    /// </summary>
    public class KitbasheryMeshImporter : MonoBehaviour
    {
        [HideInInspector]
        public List<Import> rootImports = new List<Import>();

        [Space]
        public MeshInspector meshInspector;
        public ImportUIManager importUI;
        public BuildModeUI buildControls;

        public Material defaultMaterial;

        public enum kitbasheryUIMode { import, inspector, buildmode }
        public kitbasheryUIMode mode;

        public string[] supportedImportFormats;

        private void Start()
        {
            GetSupportedFileFilters();
        }

        /// <summary>
        /// Gets supported file extensions for filtering (removes the "." from the front).
        /// </summary>
        public void GetSupportedFileFilters()
        {
            AssimpContext context = new AssimpContext();

            List<string> supported = new List<string>();
            char[] period = ".".ToCharArray();
            foreach (string format in context.GetSupportedImportFormats())
            {
                supported.Add(format.TrimStart(period));
            }
            supportedImportFormats.Equals(supported.ToString());
        }


        #region Import/Export Functions:

        public void ImportSingle(string path)
        {
            Import import = Load(path);
            if (import != null)
            {
                rootImports.Add(import);

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
                Import import = Load(path);
                rootImports.Add(import);
            }

            UpdateKitbasheryUI();
        }

        /// <summary>
        /// Exports an assimp scene as a .obj formatted file to filepath.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="filepath"></param>
        public void ExportOBJ(Scene scene, string savePath)
        {
            if(scene != null && scene.HasMeshes == true)
            {
                AssimpContext exporter = new AssimpContext();
                exporter.ExportFile(scene, "savePath", "objnomtl");
                //ExportDataBlob blob = exporter.ExportToBlob(scene, "objnomtl");//"objnomtl"  "Wavefront OBJ format without material file"
               // Debug.Log(blob);
                //Debug.Log(savePath);
                //if(blob.HasData == true)
               // {
                    /* if (File.Exists(savePath))
                     {
                         File.Delete(savePath);
 #if UNITY_EDITOR

                         File.Delete(savePath + ".meta");
#endif
                    }
                    else
                    {
                        Debug.Log(true);
                    }*/
                     //File.Create(savePath);
                     //File.WriteAllBytes(savePath, blob.Data);
               // }
               // else
               // {
               //     Debug.LogError("No data to export.");
              //  }
            //}
           // else
           // {
           //     Debug.Log("Scene to export does not contain any meshes or is null.");
           }
        }

        #endregion

        #region Wrapper Conversions:

        public Assimp.Mesh UnityMeshToAssimpMesh(Mesh uMesh)
        {
            Assimp.Mesh m = new Assimp.Mesh();


            foreach (Vector3 v3 in uMesh.vertices)
            {
                m.Vertices.Add(UnityV3toAssimpV3(v3));
            }

            foreach (Vector3 v3 in uMesh.normals)
            {
                m.Normals.Add(UnityV3toAssimpV3(v3));
            }

            for (int i = 0; i < uMesh.triangles.Length; i += 3)
            {
                Face f = new Face();
                f.Indices.Add(i);
                f.Indices.Add(i + 1);
                f.Indices.Add(i + 2);
                m.Faces.Add(f);
                //Should order be 2,1,0
            }

            foreach (Vector2 v2 in uMesh.uv)
            {
                m.TextureCoordinateChannels[0].Add(UnityV2toAssimpV3(v2));
            }

            return m;
        }

        public Vector3D UnityV2toAssimpV3(Vector2 v2)
        {
            Vector3D v3d = new Vector3D();
            v3d.X = v2.x;
            v3d.Y = v2.y;
            //should x be negative?

            return v3d;
        }

        public Vector3D UnityV3toAssimpV3(Vector3 v3)
        {
            Vector3D v3d = new Vector3D();
            v3d.X = v3.x;
            v3d.Y = v3.y;
            v3d.Z = v3.z;
            //should x be negative?

            return v3d;
        }

        public Scene UnityMeshToAssimpScene(Mesh mesh)
        {
            Scene s = new Scene();
            s.Meshes.Add(UnityMeshToAssimpMesh(mesh));
            return s;
        }

        public Scene GameObjectToAssimpScene(GameObject go)
        {
            Scene s = new Scene();
            s.Meshes.Add(UnityMeshToAssimpMesh(go.GetComponent<MeshFilter>().sharedMesh));
            return s;
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

                    if (rootImports.Count > 0)
                    {
                        foreach(Import import in rootImports)
                        {
                            //Check if the root GO has a mesh:
                            MeshFilter mf = import.GO.GetComponent<MeshFilter>();
                            if(mf != null)
                            {
                                //Make sure mesh is hidden, set to the world origin and has a valid name: 
                                import.GO.SetActive(false);
                                MeshTransformToWorldOrigin(mf);

                                if (import.GO.name.Contains("."))
                                {
                                    import.GO.name = import.GO.name.Replace(".", "_");
                                }

                                importUI.imports.Add(new Import(UnityMeshToAssimpScene(mf.sharedMesh), import.GO, mf));
                            }
           
                            //Repeat for child objects:
                            foreach (MeshFilter filter in import.GO.GetComponentsInChildren<MeshFilter>())
                            {
                                filter.transform.parent = null;
                                filter.gameObject.SetActive(false);
                                MeshTransformToWorldOrigin(filter);

                                if (filter.gameObject.name.Contains("."))
                                {
                                    filter.gameObject.name = filter.gameObject.name.Replace(".", "_");
                                }

                                importUI.imports.Add(new Import(UnityMeshToAssimpScene(filter.sharedMesh), filter.gameObject, filter));
                            }
                        }

                        //Set current import:
                        importUI.current = importUI.imports[0];
                        importUI.current.GO.SetActive(true);
                        importUI.meshName.text = importUI.current.GO.name;
                        importUI.index = (importUI.importIndex + 1).ToString();
                        importUI.importCount.text = "Import " + importUI.index + " of " + importUI.imports.Count;

                        ClearImports();
                    }

                    break;

                case kitbasheryUIMode.inspector:

                    meshInspector.filter.sharedMesh = rootImports[0].GO.GetComponentInChildren<MeshFilter>().sharedMesh;
                    ClearImports();

                    meshInspector.vertCount.text = "Vertices: " + meshInspector.filter.sharedMesh.vertexCount + " | Triangles: " + meshInspector.filter.sharedMesh.triangles.Length;

                    meshInspector.browserUI.SetActive(false);
                    meshInspector.orbitCam.enabled = true;
                    meshInspector.ViewDefault();


                    break;

                case kitbasheryUIMode.buildmode:

                    buildControls.currentImport = rootImports[0];
                
                    ClearImports();

                    break;
            }
        }

        public void ClearImports()
        {
            if (rootImports.Count > 0)
            {
                for (int i = 0; i < rootImports.Count; i++)
                {
                    Import import = rootImports[i];
                    rootImports.Remove(import);
                    Destroy(import.GO);
                }
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
            -Added Import class.
            -Load function now returns an Import.
        */


        private Import Load(string meshPath, float scaleX = 1, float scaleY = 1, float scaleZ = 1, bool loadTextures = false)
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

                    if (loadTextures == true)
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

            return new Import(scene, NodeToGameObject(scene.RootNode), null);         
        }

        #endregion
    }

    public class Import
    {
        public GameObject GO;
        public Scene scene;
        public MeshFilter filters;

        public Import(Scene s, GameObject go, MeshFilter f)
        {
            scene = s;
            GO = go;
            filters = f;
        }
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
}