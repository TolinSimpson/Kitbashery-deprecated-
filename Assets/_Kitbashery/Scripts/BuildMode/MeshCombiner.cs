using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    public static class MeshCombiner
    {
        /// <summary>
        /// Combines meshes and packs UVs into an atlas:
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="weldVerts"></param>
        /// <returns></returns>
        public static Mesh CombineMeshes(List<MeshFilter> filters, bool weldVerts)
        {
            //Note: scale uv atlas elements by the bounding size of each mesh in the combine. Bigger meshes should have more space on the atlas.

            Mesh m = new Mesh();

            if(weldVerts == true) //COMBINE USING A CUSTOM MESH COMBINER THAT PRESERVES WELDED VERTICES:
            {
                List<Vector3> verts = new List<Vector3>();
                List<Vector2> uvs = new List<Vector2>();
                List<Vector3> normals = new List<Vector3>();
                List<int> tris = new List<int>();

                if (filters.Count > 0)
                {
                    //Pack UVs:
                    Rect[] packedUVs = PackUVs(filters.ToArray());

                    for (int i = 0; i < filters.Count; i++)
                    {
                        if (filters[i] != null)
                        {
                            filters[i].mesh.SetUVs(0, FitUVsToRect(filters[i].sharedMesh.uv, GetUVRect(filters[i].sharedMesh.uv), packedUVs[i]));

                            Debug.Log(filters[i]);
                            for (int v = 0; v <= filters[i].mesh.vertices.Length - 1; v++)
                            {
                                if (!verts.Contains(filters[i].mesh.vertices[v]))//if duplicate vert...
                                {
                                    verts.Add(filters[i].mesh.vertices[v]);
                                    uvs.Add(filters[i].mesh.uv[v]);
                                    normals.Add(filters[i].mesh.normals[v]);

                                    //Note may have to add tris.Count to the index as an offset.
                                    tris.Add(filters[i].mesh.triangles[v]);
                                    tris.Add(filters[i].mesh.triangles[v + 1]);
                                    tris.Add(filters[i].mesh.triangles[v + 2]);

                                    //should triangles be solved by a triangulator?
                                }
                                //else don't add duplicates...
                            }
                        }
                    }
                    m.SetVertices(verts);
                    m.SetUVs(0, FitUVs(uvs.ToArray()));
                    m.SetNormals(normals);
                    m.SetTriangles(tris, 0);
                }
                else
                {
                    Debug.LogError("User tried combining nothing. Returning null!");
                    return null;
                }
            }
            else //COMBINE USING UNITY'S BUILT-IN MESH COMBINE:
            {
                //https://docs.unity3d.com/ScriptReference/Mesh.CombineMeshes.html
                List<CombineInstance> combine = new List<CombineInstance>();

                if (filters.Count > 0)
                {
                    Rect[] packedUVs = PackUVs(filters.ToArray());
                    for (int i = 0; i < filters.Count; i++)
                    {
                        if (filters[i] != null)
                        {
                            CombineInstance instance = new CombineInstance();
                            instance.mesh = filters[i].sharedMesh;
                            instance.transform = filters[i].transform.localToWorldMatrix;
                            instance.mesh.SetUVs(0, FitUVsToRect(filters[i].sharedMesh.uv, GetUVRect(filters[i].sharedMesh.uv), packedUVs[i]));
                            combine.Add(instance);
                        }
                    }

                    m.CombineMeshes(combine.ToArray(), true, true, false); //Note: why are normals sometimes flipped??
                    //xatlas.xatlas.Unwrap(m, 1);
                    m.SetUVs(0, FitUVs(m.uv));

                    //Note: Unity's mesh combiner removes welded vertices, there will get rewelded by probuilder when combined via BuildModeUI.cs.
                }
                else
                {
                    Debug.LogError("User tried combining nothing. Returning null!");
                    return null;
                }
            }

           

            return m;

        }

        #region Pack UVs:

        /// <summary>
        /// Packs UVs into a grid. TODO: replace with a proper bin or uv pack type thing preferably with meshes sorted by the size of their bounds having larger UV squares for larger bounds.
        /// </summary>
        /// <returns></returns>
        public static Rect[] PackUVs(MeshFilter[] filters)
        {
            Rect[] packedUVs = new Rect[filters.Length];

            if (filters.Length > 1)
            {
                //Get the first square that can contain the amount of filters:
                int num = 2;
                int square = num * num;
                while (square < filters.Length)
                {
                    square = num * num;
                    num++;
                }

                float halfScale = 1f / num;//Find the size a cell would need to be to fit in uv space in a row/collum.

                float quarterScale = halfScale / 2f;

                float quarterHalf = quarterScale / num;

                //Iterate through used cells in the grid left to right bottom up:
                int x = 0;
                int y = 1;
                float xOffset = 0;
                float yOffset = 0;
                Vector2 center = Vector2.zero;
                Vector2 size = Vector2.zero;
                for (int i = 0; i < filters.Length; i++)
                {
                    if (num > 3)//3x3+ grid:  //NOTE: 3X3+ grids drift out of bounds with large amounts of squares.
                    {
                        if (x == num - 1)
                        {
                            x = 0;
                            xOffset = 0;
                            y++;
                            yOffset += halfScale + quarterHalf;
                        }
                        center = new Vector2(xOffset + halfScale - quarterHalf, yOffset + halfScale - quarterHalf);
                        size = new Vector2(halfScale + quarterHalf, halfScale + quarterHalf);
                        xOffset += halfScale + quarterHalf;
                    }
                    else//2x2 grid:
                    {
                        if (x == num)
                        {
                            x = 0;
                            xOffset = 0;
                            y++;
                            yOffset += halfScale;
                        }
                        center = new Vector2(xOffset + quarterScale, yOffset + quarterScale);
                        size = new Vector2(halfScale, halfScale);
                        xOffset += halfScale;
                    }

                    //Create new UV rect:
                    Rect r = new Rect();
                    r.center = center;
                    r.size = size;
                    packedUVs[i] = r;

                    x++;
                }
            }

            return packedUVs;
        }

        /// <summary>
        /// Fits a Vector2[] to a rect:
        /// </summary>
        /// <param name="uvs"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static Vector2[] FitUVsToRect(Vector2[] uvs, Rect uvRect, Rect packRect)
        { 
            for (int i = 0; i < uvs.Length; i++)
            {

                uvs[i].x = packRect.x + uvs[i].x * packRect.width;
                uvs[i].y = packRect.y + uvs[i].y * packRect.height;
            }

            return uvs;
        }

        public static Rect GetUVRect(Vector2[] uvs)
        {
            Rect r = new Rect();

            float xMin = Mathf.Infinity;
            float xMax = -Mathf.Infinity;
            float yMin = Mathf.Infinity;
            float yMax = -Mathf.Infinity;

            foreach (Vector2 v2 in uvs)
            {
                if (v2.x < xMin)
                {
                    xMin = v2.x;
                }
                else if (v2.x > xMax)
                {
                    xMax = v2.x;
                }

                if (v2.y < yMin)
                {
                    yMin = v2.y;
                }
                else if (v2.y > yMax)
                {
                    yMax = v2.y;
                }

            }

            r.xMin = xMin;
            r.xMax = xMax;
            r.yMax = yMax;
            r.yMin = yMin;

            return r;
        }

        /// <summary>
        /// (From ProBuilder)
        /// Returns normalized UV values for a mesh uvs (0,0) - (1,1)
        /// </summary>
        /// <param name="uvs"></param>
        /// <returns></returns>
        public static Vector2[] FitUVs(Vector2[] uvs)
        {
            // shift UVs to zeroed coordinates
            Vector2 smallestVector2 = SmallestVector2(uvs);

            int i;
            for (i = 0; i < uvs.Length; i++)
            {
                uvs[i] -= smallestVector2;
            }

            float scale = LargestValue(LargestVector2(uvs));

            for (i = 0; i < uvs.Length; i++)
            {
                uvs[i] /= scale;
            }

            return uvs;
        }

        /// <summary>
        /// (From ProBuilder)
        /// The smallest X and Y value found in an array of Vector2. May or may not belong to the same Vector2.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 SmallestVector2(Vector2[] v)
        {
            int len = v.Length;
            Vector2 l = v[0];
            for (int i = 0; i < len; i++)
            {
                if (v[i].x < l.x) l.x = v[i].x;
                if (v[i].y < l.y) l.y = v[i].y;
            }
            return l;
        }

        /// <summary>
        /// (From Probuilder)
        /// The largest X and Y value in an array.  May or may not belong to the same Vector2.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 LargestVector2(Vector2[] v)
        {
            int len = v.Length;
            Vector2 l = v[0];
            for (int i = 0; i < len; i++)
            {
                if (v[i].x > l.x) l.x = v[i].x;
                if (v[i].y > l.y) l.y = v[i].y;
            }
            return l;
        }

        /// <summary>
        /// (From ProBuilder)
        /// Return the largest axis in a Vector2.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static float LargestValue(Vector2 v)
        {
            return (v.x > v.y) ? v.x : v.y;
        }

        #endregion
    }
}