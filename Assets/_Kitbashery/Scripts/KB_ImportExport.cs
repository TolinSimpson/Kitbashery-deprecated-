using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Assimp;
using UnityEngine;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;
using Quaternion = UnityEngine.Quaternion;


//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    //TODO: Static methods in here might not have to be static since scripts that call them might all have refrence to this monoBehaviour.

    /// <summary>
    /// This class manages Importing and exporting meshes and images in Kitbashery.
    /// 
    /// This class contains the following:
    /// Modified version of "UnityMeshImporter" © 2019 by Dongho Kang (See licnece above implementation)
    /// Modified version of "OBJ Exporter" from the Unity Community Wiki: http://wiki.unity3d.com/index.php?title=ExportOBJ by DaveA, KeliHlodversson, tgraupmann, drobe 
    /// Custom Assimp wrapper functions and mesh editing tools.
    /// Custom Image importer & exporter.
    /// </summary>
    public class KB_ImportExport : MonoBehaviour
    {
        #region Variables:

        public KB_ObjectManager objectManager;

        /// <summary>
        /// Supported texture export formats.
        /// </summary>
        public enum SaveTextureFormat { png, jpg, tga, exr }

        [Space]

        public Material defaultMaterial;

        /// <summary>
        /// Mesh formats supported by Assimp.
        /// </summary>
        public string[] supportedImportFormats;

        #endregion

        #region Initialization & Updates:

        // Start is called before the first frame update
        void Start()
        {
            GetSupportedFileFilters();
        }

        #endregion

        #region Kitbashery Import Functions:

        /// <summary>
        /// Import an external mesh file into Kitbashery.
        /// </summary>
        /// <param name="paths"></param>
        public void ImportExternalMesh(string[] paths)
        {
            foreach (string path in paths)
            {
                Import import = Load(path);

                if(import != null)
                {
                    //Check if the root GO has a mesh:
                    MeshFilter mf = import.GO.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        //Make sure mesh is hidden, set to the world origin and has a valid name: 
                        import.GO.SetActive(false);
                        MeshTransformToWorldOrigin(mf);

                        if (import.GO.name.Contains("."))
                        {
                            import.GO.name = import.GO.name.Replace(".", "_");
                        }

                        objectManager.imports.Add(new Import(UnityMeshToAssimpScene(mf.sharedMesh), import.GO, mf, import.GO.GetComponent<MeshRenderer>()));
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

                        objectManager.imports.Add(new Import(UnityMeshToAssimpScene(filter.sharedMesh), filter.gameObject, filter, filter.gameObject.GetComponent<MeshRenderer>()));
                    }
                }

                objectManager.imports.Add(import);

                objectManager.currentImport = objectManager.imports[0];

                objectManager.currentImport.GO.SetActive(true);
                //importUI.meshName.text = objectManager.currentImport.GO.name;
                objectManager.importIndexString = (objectManager.importIndex + 1).ToString();
               // importUI.importCount.text = "Import " + importUI.index + " of " + importUI.imports.Count;
            }

        }//TODO check if the mesh bounds is larger or smaller than 1 and scale the mesh to fit using the scale params in the load function.

        /// <summary>
        /// Imports a mesh that has been saved to the library.
        /// </summary>
        /// <param name="path"></param>
        public void ImportFromLibrary(string path, bool inspect)
        {
            objectManager.ClearImports();
            Import import = Load(path, 1, 1, 1, false, true);
            objectManager.currentImport = import;
            if (inspect == true)
            {
                objectManager.inspected = import;
                objectManager.inspectedMeshStats = "Vertices: " + objectManager.inspected.filter.sharedMesh.vertexCount + " | Triangles: " + objectManager.inspected.filter.sharedMesh.triangles.Length / 3;
                objectManager.inspected.GO.SetActive(true);
            }
        }

        #endregion

        #region Unity Mesh Importer (Assimp Wrapper):

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


        private Import Load(string meshPath, float scaleX = 1, float scaleY = 1, float scaleZ = 1, bool loadTextures = false, bool fromLibrary = false)
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

            Import import = new Import(scene, NodeToGameObject(scene.RootNode), null, null);
            if (fromLibrary == true)
            {
                import.filter = import.GO.GetComponentInChildren<MeshFilter>();
                import.rend = import.GO.GetComponentInChildren<MeshRenderer>();
                import.GO = import.filter.gameObject;

                GameObject root = import.GO.transform.root.gameObject;
                import.GO.transform.SetParent(null);
                Destroy(root);
            }

            return import;
        }

        #endregion

        #region Custom Kitbashery Assimp Wrapper Conversions:

        /// <summary>
        /// Exports an assimp scene as a .obj formatted file to filepath.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="filepath"></param>
        public void ExportOBJ(Scene scene, string savePath)
        {
            if (scene != null && scene.HasMeshes == true)
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

        /// <summary>
        /// Converts a Unity mesh to an Assimp mesh.
        /// </summary>
        /// <param name="uMesh"></param>
        /// <returns></returns>
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

        #region Naitive OBJ Exporter:

        /*
Original from the Unity Community Wiki: http://wiki.unity3d.com/index.php?title=ExportOBJ by DaveA, KeliHlodversson, tgraupmann, drobe

Kitbashery changes: 
-Removed redundant code
-Removed UnityEditor dependancy
-Merged Start() and Stop() into ResetIndex()
-Renamed ambiguous variables
-Improved error checking
-Renamed Export() method to ExportOBJ()
*/

        private static int startIndex = 0;

        private static void ResetIndex()
        {
            startIndex = 0;
        }

        public static void ExportOBJ(GameObject go, string path)
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

        #endregion

        #region Kitbashery Mesh Functions:

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

        /// <summary>
        /// Combines mesh filters into a single mesh.
        /// </summary>
        /// <param name="filters"></param>
        public void CombineMeshes(List<MeshFilter> filters)
        {
            if (filters.Count > 1)
            {
                if (filters.Count > 0)
                {
                    Mesh combine = MeshCombiner.MeshCombiner.Combine(filters.ToArray(), true, 0.01f);

                    combine.Optimize();
                    combine.RecalculateBounds();

                    //Create part:
                    GameObject go = new GameObject("Combined Part");
                    KitbashPart part = go.AddComponent<KitbashPart>();
                    //part.transform.SetParent(kitbash.transform);

                    //Create renderer:
                    part.rend = part.gameObject.AddComponent<MeshRenderer>();
                    part.rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    part.rend.receiveShadows = false;
                    part.rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    //part.rend.material = defaultMat;
                    part.rend.allowOcclusionWhenDynamic = false;

                    //Assign mesh:
                    part.filter = go.AddComponent<MeshFilter>();
                    part.filter.sharedMesh = combine;
                }
                else
                {
                    Debug.LogError("No filters were added to combine!");
                }
            }
            else
            {
                Debug.Log("Not enough filters to combine.");
            }
        }

        #endregion

        #region Kitbashery Image Importer/Exporter:

        /// <summary>
        /// Loads material textures from a folder path.
        /// </summary>
        /// <param name="materialFolderPath"></param>
        public Texture2D[] ImportMaterial(string materialFolderPath)
        {
            /*string[] files = Directory.GetFiles(materialFolderPath);
            if (files.Length > 2)
            {
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (!fileName.Contains(".meta"))
                    {
                        if (fileName.Contains("_Normal"))
                        {
                            byte[] byteArray = File.ReadAllBytes(file);
                            Texture2D sampleTexture = new Texture2D(2, 2);
                            bool isLoaded = sampleTexture.LoadImage(byteArray, false);
                            if (isLoaded == true)
                            {
                                paintable.normalMap = sampleTexture;
                            }
                            else
                            {
                                Debug.LogError("Failed to load normal map at path: " + file);
                            }
                        }

                        if (fileName.Contains("_MatID"))
                        {
                            byte[] byteArray = File.ReadAllBytes(file);
                            Texture2D sampleTexture = new Texture2D(2, 2);
                            bool isLoaded = sampleTexture.LoadImage(byteArray, false);
                            if (isLoaded == true)
                            {
                                paintable.matID = sampleTexture;
                            }
                            else
                            {
                                Debug.LogError("Failed to load material ID at path: " + file);
                            }
                        }
                    }
                }
            }*/

            return null;
        }

        public static void SaveRenderTexture(RenderTexture rt, string texName, SaveTextureFormat format, string savePath)
        {
            SaveTexture2D(ToTexture2D(rt), texName, format, savePath);
        }

        public static void SaveTexture2D(Texture2D tex2D, string texName, SaveTextureFormat format, string savePath)
        {
            savePath += "/" + texName;

            byte[] _bytes = new byte[0];

            switch (format)
            {
                case SaveTextureFormat.jpg:

                    _bytes = tex2D.EncodeToJPG(100);

                    savePath += ".jpg";

                    break;

                case SaveTextureFormat.png:


                    _bytes = tex2D.EncodeToPNG();

                    savePath += ".png";

                    break;

                case SaveTextureFormat.tga:

                    _bytes = tex2D.EncodeToTGA();

                    savePath += ".tga";

                    break;

                case SaveTextureFormat.exr:

                    _bytes = tex2D.EncodeToEXR();

                    savePath += ".exr";

                    break;
            }

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
#if UNITY_EDITOR

                File.Delete(savePath + ".meta");
#endif
            }
            File.WriteAllBytes(savePath, _bytes);
        }

        public static void SaveTexture2DFullPath(Texture2D tex2D, string savePath)
        {
            string extension = Path.GetExtension(savePath);
            Debug.Log(extension);

            byte[] _bytes = new byte[0];

            switch (extension)
            {
                case ".jpg":

                    _bytes = tex2D.EncodeToJPG(100);

                    break;

                case ".png":

                    _bytes = tex2D.EncodeToPNG();

                    break;

                case ".tga":

                    _bytes = tex2D.EncodeToTGA();

                    break;

                case ".exr":

                    _bytes = tex2D.EncodeToEXR();

                    break;
            }

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
#if UNITY_EDITOR

                File.Delete(savePath + ".meta");
#endif
            }
            File.WriteAllBytes(savePath, _bytes);
        }

        public static Texture2D ToTexture2D(RenderTexture rTex)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            return tex;
        }

        #endregion 
    }

    /// <summary>
    /// This class defines an object imported into Kitbashery that keeps track of the Assimp Scene, the generated GameObject and the mesh filter component.
    /// </summary>
    public class Import
    {
        public GameObject GO;
        public Scene scene;
        public MeshFilter filter;
        public MeshRenderer rend;

        public Import(Scene s, GameObject go, MeshFilter f, MeshRenderer r)
        {
            scene = s;
            GO = go;
            filter = f;
            rend = r;
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