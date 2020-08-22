using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshCombiner
{
    //Note scale uv atlas elements by the bounding size of each mesh in the combine.

    //https://docs.unity3d.com/ScriptReference/Mesh.CombineMeshes.html

    //Note: probably should write a custom mesh combine rather than use Unity's builtin combiner.

    public static Mesh CombineMeshes(List<MeshFilter> filters)
    {
        Mesh m = new Mesh();

        List<CombineInstance> combine = new List<CombineInstance>();
        MaxRectsBinPack binPack = new MaxRectsBinPack(1, 1);
        List<Vector2> packedUV = new List<Vector2>();

        if (filters.Count > 0)
        {
            foreach (MeshFilter mf in filters)
            {
                if(mf != null)
                {
                    CombineInstance instance = new CombineInstance();
                    instance.mesh = mf.sharedMesh;
                    instance.transform = mf.transform.localToWorldMatrix;
                    combine.Add(instance);

                    #region Pack UVs:

                    Vector2[] uv = mf.sharedMesh.uv;//RoundUV(mf.sharedMesh.uv);
                    Rect uvRect = GetUVRect(uv);
                    Rect atlasRect = binPack.Insert(Mathf.RoundToInt(uvRect.width), Mathf.RoundToInt(uvRect.height), MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit);//Does rounding cause problems?

                    float distance = Vector2.Distance(uvRect.center, atlasRect.center);
                    float scale = atlasRect.width * atlasRect.height;
                    for (int i = 0; i < uv.Length; i++)
                    {
                        //Scale UV to the size of the rect fit to the atlas:
                        uv[i] *= scale;

                        //Translate UV points to their corrasponding point on the atlas:
                        packedUV.Add(Vector2.MoveTowards(uv[i], Rect.NormalizedToPoint(atlasRect, Rect.PointToNormalized(uvRect, uv[i])), distance));

                        //OLD TRANSLATE METHOD:
                        /*
                        //Get direction from the current point to the center of the current uv set:
                        Vector2 heading = uv[i] - uvRect.center;
                        Vector2 oldDirection = heading / heading.magnitude;
                        //Translate the point to the new center by the same direction and distance offset that it had around the old center:
                        packedUV.Add(new Vector2(oldDirection.x + distance, oldDirection.y + distance));
                        */

                    }

                    #endregion
                }
            }

            m.CombineMeshes(combine.ToArray(), true, true, false); //Note: why are normals sometimes flipped??

            m.SetUVs(0, packedUV.ToArray());
        }
        else
        {
            Debug.LogError("User tried combining nothing. Returning null!");
            return null;
        }

        return m;
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
            if(v2.x < xMin)
            {
                xMin = v2.x;
            }
            else if(v2.x > xMax)
            {
                xMax = v2.x;
            }

            if(v2.y < yMin)
            {
                yMin = v2.y;
            }
            else if(v2.y > yMax)
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

    public static Vector2[] RoundUV(Vector2[] uvs)
    {
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(Mathf.RoundToInt(uvs[i].x), Mathf.RoundToInt(uvs[i].y));
        }

        return uvs;
    }
}
