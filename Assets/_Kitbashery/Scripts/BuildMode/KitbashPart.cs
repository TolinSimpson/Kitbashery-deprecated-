using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KitbashPart : MonoBehaviour
{
    public MeshRenderer rend;
    public MeshFilter filter;
    public MeshCollider col;
    public Mesh original;
    public HierarchyItem ui;

    public void Decimate(int iterations)
    {
        //https://github.com/Whinarn/UnityMeshSimplifier
    }

    public void Subdivide(int iterations)
    {
        //https://github.com/mattatz/unity-subdivision-surface
        //http://wiki.unity3d.com/index.php/MeshHelper

    }
}

