using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// Holds refrence to GameObject components and contains methods for modifying the mesh and material.
    /// </summary>
    public class KitbashPart : MonoBehaviour
    {
        public MeshRenderer rend;
        public MeshFilter filter;
        public MeshCollider col;
        public Mesh original;
        public HierarchyItem ui;
        public Texture2D unwrapTex;//TODO: defined in the object manager...needs refactor.

        public void SetMaterial(Material mat)
        {
            rend.material = mat;
        }

        public void SetMaterialProperty(int value)
        {
            //rend.material.SetInt
        }

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
}