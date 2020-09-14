////////////////////////////////////////////////////////////
/*
    TextureExtension.cs for Unity3D by Bunny83
    Tolerance added by Dchen05 using modified version of CanvasFloodFill
    Call like this:
        texture.FloodFillArea(var.x, var.y , fillColor, tolerance);
        //tolerance goes goes from 1-100
        // When you're done call Apply to commit your changes.
        texture.Apply();
*/
///////////////////////////////////////////////////////////


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class TextureExtension
{
    public struct Point
    {
        public short x;
        public short y;
        public Point(short aX, short aY) { x = aX; y = aY; }
        public Point(int aX, int aY) : this((short)aX, (short)aY) { }
    }

    public static void FloodFillArea(this Texture2D aTex, int aX, int aY, Color aFillColor, float tolerance)
    {

        float tol = tolerance / 100;
        int w = aTex.width;
        int h = aTex.height;
        Color[] colors = aTex.GetPixels();
        Color refCol = colors[aX + aY * w];
        Queue<Point> nodes = new Queue<Point>();
        nodes.Enqueue(new Point(aX, aY));
        while (nodes.Count > 0)
        {
            Point current = nodes.Dequeue();
            //this goes right
            for (int i = current.x; i < w; i++)
            {
                Color C = colors[i + current.y * w];
                if (ColorTest(refCol, C, tol) == false || C == aFillColor)
                    break;
                colors[i + current.y * w] = aFillColor;
                if (current.y + 1 < h)
                {
                    C = colors[i + current.y * w + w];
                    if (ColorTest(refCol, C, tol) == true && C != aFillColor)
                        nodes.Enqueue(new Point(i, current.y + 1));
                }
                if (current.y - 1 >= 0)
                {
                    C = colors[i + current.y * w - w];
                    if (ColorTest(refCol, C, tol) == true && C != aFillColor)
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }
            //this goes left
            for (int i = current.x - 1; i >= 0; i--)
            {
                Color C = colors[i + current.y * w];
                if (ColorTest(refCol, C, tol) == false || C == aFillColor)
                    break;
                colors[i + current.y * w] = aFillColor;
                if (current.y + 1 < h)
                {
                    C = colors[i + current.y * w + w];
                    if (ColorTest(refCol, C, tol) == true && C != aFillColor)
                        nodes.Enqueue(new Point(i, current.y + 1));
                }
                if (current.y - 1 >= 0)
                {
                    C = colors[i + current.y * w - w];
                    if (ColorTest(refCol, C, tol) == true && C != aFillColor)
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }
        }
        aTex.SetPixels(colors);
    }

    public static void FloodFillBorder(this Texture2D aTex, int aX, int aY, Color aFillColor, Color aBorderColor)
    {
        int w = aTex.width;
        int h = aTex.height;
        Color[] colors = aTex.GetPixels();
        byte[] checkedPixels = new byte[colors.Length];
        Color refCol = aBorderColor;
        Queue<Point> nodes = new Queue<Point>();
        nodes.Enqueue(new Point(aX, aY));
        while (nodes.Count > 0)
        {
            Point current = nodes.Dequeue();

            for (int i = current.x; i < w; i++)
            {
                if (checkedPixels[i + current.y * w] > 0 || colors[i + current.y * w] == refCol)
                    break;
                colors[i + current.y * w] = aFillColor;
                checkedPixels[i + current.y * w] = 1;
                if (current.y + 1 < h)
                {
                    if (checkedPixels[i + current.y * w + w] == 0 && colors[i + current.y * w + w] != refCol)
                        nodes.Enqueue(new Point(i, current.y + 1));
                }
                if (current.y - 1 >= 0)
                {
                    if (checkedPixels[i + current.y * w - w] == 0 && colors[i + current.y * w - w] != refCol)
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }
            for (int i = current.x - 1; i >= 0; i--)
            {
                if (checkedPixels[i + current.y * w] > 0 || colors[i + current.y * w] == refCol)
                    break;
                colors[i + current.y * w] = aFillColor;
                checkedPixels[i + current.y * w] = 1;
                if (current.y + 1 < h)
                {
                    if (checkedPixels[i + current.y * w + w] == 0 && colors[i + current.y * w + w] != refCol)
                        nodes.Enqueue(new Point(i, current.y + 1));
                }
                if (current.y - 1 >= 0)
                {
                    if (checkedPixels[i + current.y * w - w] == 0 && colors[i + current.y * w - w] != refCol)
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }
        }
        aTex.SetPixels(colors);
    }


    public static bool ColorTest(Color c1, Color c2, float tol)
    {
        float diffRed = Mathf.Abs(c1.r - c2.r);
        float diffGreen = Mathf.Abs(c1.g - c2.g);
        float diffBlue = Mathf.Abs(c1.b - c2.b);
        //Those values you can just divide by the amount of difference saturations (255), and you will get the difference between the two.

        float pctDiffRed = (float)diffRed / 255;
        float pctDiffGreen = (float)diffGreen / 255;
        float pctDiffBlue = (float)diffBlue / 255;

        //After which you can just find the average color difference in percentage.
        float diffPercentage = (pctDiffRed + pctDiffGreen + pctDiffBlue) / 3 * 100;

        if (diffPercentage >= tol)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

}