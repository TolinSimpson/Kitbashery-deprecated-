using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Kitbashery
{
    ////https://stackoverflow.com/questions/14108553/get-border-edges-of-mesh-in-winding-order
    //https://answers.unity.com/questions/443633/finding-the-vertices-of-each-edge-on-mesh.html
    //https://www.researchgate.net/publication/322862034_Packing_Circles_and_Irregular_Polygons_using_Separation_Lines
    //https://www.researchgate.net/publication/263806597_Dealing_with_Nonregular_Shapes_Packing
    //https://github.com/cariquitanmac/2D-Bin-Pack-Binary-Search/blob/master/2D%20Binary%20Tree%20Bin-Pack/2D%20Binary%20Tree%20Bin-Pack/Packer.cs
    //https://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm


    public static class UVPacker
    {

        public static List<Box> PackUVs(List<MeshFilter> filters)
        {
            //Order meshes in decending order by size of their bounds:
            List<MeshFilter> decendingMeshes = filters.OrderByDescending(m => m.sharedMesh.bounds.size).ToList();

            List<Box> decendingBox = new List<Box>();
            foreach (MeshFilter mf in decendingMeshes)
            {
                decendingBox.Add(GetUVBox(mf.sharedMesh.uv));
                //decendingRect.Add(RectToBox(GetUVRect(mf.sharedMesh.uv)));
            }

            //Initialize root node:
            BoxNode rootBoxNode = new BoxNode { width = filters.Count, height = filters.Count };

            //Pack:
            foreach (Box box in decendingBox)
            {
                BoxNode node = FindBoxNode(rootBoxNode, box.width, box.height);
                if (node != null)
                {
                    //Split rectangles:
                    box.position = SplitBoxNode(node, box.width, box.height);
                }
            }

            return decendingBox;
        }

        public static Box RectToBox(Rect r)
        {
            Box b = new Box();
            b.width = r.width;
            b.height = r.height;
            b.volume = r.width * r.height;

            return b;
        }

        public static Rect BoxToRect(Box b)
        {
            Rect r = new Rect();

            r.width = b.width;
            r.height = b.height;
            r.position = new Vector2(b.position.pos_x, b.position.pos_y);

            return r;
        }

        public static Box GetUVBox(Vector2[] uvs)
        {
            Box b = new Box();

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

            b.width = xMax - xMin;
            b.height = yMax - yMin;
            b.volume = b.width * b.height;

            return b;
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

        private static BoxNode FindBoxNode(BoxNode rootBoxNode, float boxWidth, float boxHeight)
        {
            if (rootBoxNode.isOccupied)
            {
                var nextBoxNode = FindBoxNode(rootBoxNode.bottomBoxNode, boxWidth, boxHeight);

                if (nextBoxNode == null)
                {
                    nextBoxNode = FindBoxNode(rootBoxNode.rightBoxNode, boxWidth, boxHeight);
                }

                return nextBoxNode;
            }
            else if (boxWidth <= rootBoxNode.width && boxHeight <= rootBoxNode.height)
            {
                return rootBoxNode;
            }
            else
            {
                return null;
            }
        }

        private static BoxNode SplitBoxNode(BoxNode node, float boxWidth, float boxHeight)
        {
            node.isOccupied = true;
            node.bottomBoxNode = new BoxNode { pos_y = node.pos_y, pos_x = node.pos_x + boxWidth, height = node.height, width = node.width - boxWidth };
            node.rightBoxNode = new BoxNode { pos_y = node.pos_y + boxHeight, pos_x = node.pos_x, height = node.height - boxHeight, width = boxWidth };
            return node;
        }
    }

    public class BoxNode
    {
        public BoxNode rightBoxNode;
        public BoxNode bottomBoxNode;
        public float pos_x;
        public float pos_y;
        public float width;
        public float height;
        public bool isOccupied;
    }


    public class Box
    {
        public float width;
        public float height;
        public float volume;
        public BoxNode position;
    }



}