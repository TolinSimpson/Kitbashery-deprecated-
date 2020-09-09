using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeGizmos;

namespace Kitbashery
{

    /// <summary>
    /// Creates Blender style keybinds for transformations along axis, attempts to integrate with <see cref="TransformGizmo"/>.
    /// </summary>
    public class TransformController : MonoBehaviour
    {
        public enum Axis { None, X, Y, Z, All }
        public Axis axis = Axis.None;
        public enum Transformation { None, Translate, Rotate, Scale }
        public Transformation transformation = Transformation.None;
        public string numericInput;
        public Transform target;
        public Transform mirroredTarget;

        [Header("Axis Gizmos:")]
        public TransformGizmo gizmo;
        public int renderDistance = 5000;
        public float gridSize = 1;
        public int gridLines = 100;
        public bool drawX = false;
        public bool drawY = false;
        public bool drawZ = false;
        public Color xColor = Color.red;
        public Color yColor = Color.green;
        public Color zColor = Color.blue;
        public Color gridColor = Color.gray;
        private Vector3 xStart;
        private Vector3 yStart;
        private Vector3 zStart;
        private int previousRenderDistance = 0;
        private Material lineMaterial;

        #region Initialization & Updates:


        private void Start()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);
        }


        // Update is called once per frame
        void Update()
        {
            if (renderDistance != previousRenderDistance)
            {
                xStart = Vector3.left * renderDistance;
                yStart = Vector3.up * renderDistance;
                zStart = Vector3.forward * renderDistance;

                previousRenderDistance = renderDistance;
            }

            if (target != null)
            {
                //Translate:
                if (Input.GetKeyUp(KeyCode.T))
                {
                    if (transformation == Transformation.Translate)
                    {
                        transformation = Transformation.None;
                        axis = Axis.None;
                    }
                    else
                    {
                        transformation = Transformation.Translate;
                        gizmo.transformType = TransformType.Move;
                    }
                }

                //Rotate:
                if (Input.GetKeyUp(KeyCode.R))
                {
                    if (transformation == Transformation.Rotate)
                    {
                        transformation = Transformation.None;
                        axis = Axis.None;
                    }
                    else
                    {
                        transformation = Transformation.Rotate;
                        gizmo.transformType = TransformType.Rotate;
                    }
                }

                //Scale:
                if (Input.GetKeyUp(KeyCode.S))
                {
                    if (transformation == Transformation.Scale)
                    {
                        transformation = Transformation.None;
                        axis = Axis.None;
                    }
                    else
                    {
                        transformation = Transformation.Scale;
                        gizmo.transformType = TransformType.Scale;
                    }
                }

                if (transformation != Transformation.None)
                {
                    GetAxis();

                    TransformTarget();
                }
            }
        }

        void OnRenderObject()
        {
            GL.PushMatrix();
            lineMaterial.SetPass(0);

            GL.Begin(GL.LINES);

            //Draw gridlines: //http://wiki.unity3d.com/index.php/DrawGizmoGrid //This whole script's development is paused, currently using probuilder's grid script but when/if transform controller get worked on add this gizmo grid.
            /*GL.Color(gridColor);
            for (int i = 0; i <= gridLines; i++)
            {
                Vector3 offset = Vector3.forward * (gridSize * i);
                GL.Vertex();
                GL.Vertex();

                GL.Vertex();
                GL.Vertex();
            }*/

            if (drawX == true)
            {
                GL.Color(xColor);
                GL.Vertex(xStart);
                GL.Vertex(-xStart);
            }

            if (drawY == true)
            {
                GL.Color(yColor);
                GL.Vertex(yStart);
                GL.Vertex(-yStart);
            }

            if (drawZ == true)
            {
                GL.Color(zColor);
                GL.Vertex(zStart);
                GL.Vertex(-zStart);
            }

            GL.End();

            GL.PopMatrix();
        }

        #endregion

        public void TransformTarget()
        {
            if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
            {
                if (transformation != Transformation.None && numericInput != string.Empty)
                {
                    switch (transformation)
                    {
                        case Transformation.Scale:

                            ScaleTransform(float.Parse(numericInput));

                            break;

                        case Transformation.Translate:

                            TranslateTransform(float.Parse(numericInput));

                            break;

                        case Transformation.Rotate:

                            RotateTransform(float.Parse(numericInput));

                            break;
                    }

                    numericInput = string.Empty;
                }
            }
        }

        private void GetAxis()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (axis == Axis.X)
                {
                    axis = Axis.None;
                    drawX = false;
                }
                else
                {
                    axis = Axis.X;
                    drawX = true;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                if (axis == Axis.Y)
                {
                    axis = Axis.None;
                    drawY = false;
                }
                else
                {
                    axis = Axis.Y;
                    drawY = true;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                if (axis == Axis.Z)
                {
                    axis = Axis.None;
                    drawZ = false;
                }
                else
                {
                    axis = Axis.Z;
                    drawZ = true;
                }
            }

            if (axis != Axis.None)
            {
                GetNumericInput();
            }
        }

        private void GetNumericInput()
        {
            if (Input.GetKeyUp(KeyCode.Minus) || Input.GetKeyUp(KeyCode.KeypadMinus))
            {
                if (!numericInput.StartsWith("-"))
                {
                    numericInput = "-" + numericInput;
                }
            }
            if (Input.GetKeyUp(KeyCode.Alpha0) || Input.GetKeyUp(KeyCode.Keypad0))
            {
                numericInput += "0";
            }
            if (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetKeyUp(KeyCode.Keypad1))
            {
                numericInput += "1";
            }
            if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Keypad2))
            {
                numericInput += "2";
            }
            if (Input.GetKeyUp(KeyCode.Alpha3) || Input.GetKeyUp(KeyCode.Keypad3))
            {
                numericInput += "3";
            }
            if (Input.GetKeyUp(KeyCode.Alpha4) || Input.GetKeyUp(KeyCode.Keypad4))
            {
                numericInput += "4";
            }
            if (Input.GetKeyUp(KeyCode.Alpha5) || Input.GetKeyUp(KeyCode.Keypad5))
            {
                numericInput += "5";
            }
            if (Input.GetKeyUp(KeyCode.Alpha6) || Input.GetKeyUp(KeyCode.Keypad6))
            {
                numericInput += "6";
            }
            if (Input.GetKeyUp(KeyCode.Alpha7) || Input.GetKeyUp(KeyCode.Keypad7))
            {
                numericInput += "7";
            }
            if (Input.GetKeyUp(KeyCode.Alpha8) || Input.GetKeyUp(KeyCode.Keypad8))
            {
                numericInput += "8";
            }
            if (Input.GetKeyUp(KeyCode.Alpha9) || Input.GetKeyUp(KeyCode.Keypad9))
            {
                numericInput += "9";
            }
            if (Input.GetKeyUp(KeyCode.Period) || Input.GetKeyUp(KeyCode.Period))
            {
                numericInput += ".";
            }
        }

        public void ScaleTransform(float scale)
        {
            switch (axis)
            {
                case Axis.X:

                    target.localScale = new Vector3(scale, target.localScale.y, target.localScale.z);

                    break;

                case Axis.Y:

                    target.localScale = new Vector3(target.localScale.x, scale, target.localScale.z);

                    break;

                case Axis.Z:

                    target.localScale = new Vector3(target.localScale.x, target.localScale.y, scale);

                    break;

                case Axis.All:

                    target.localScale += Vector3.one * scale;

                    break;
            }
        }

        public void TranslateTransform(float distance)
        {
            switch (axis)
            {
                case Axis.X:

                    target.position = new Vector3(distance, target.position.y, target.position.z);

                    break;

                case Axis.Y:

                    target.position = new Vector3(target.localScale.x, distance, target.position.z);

                    break;

                case Axis.Z:

                    target.position = new Vector3(target.position.x, target.position.y, distance);

                    break;

                case Axis.All:

                    target.position += Vector3.one * distance;

                    break;
            }
        }

        public void RotateTransform(float angle)
        {
            switch (axis)
            {
                case Axis.X:

                    target.localEulerAngles = new Vector3(angle, target.localEulerAngles.y, target.localEulerAngles.z);

                    break;

                case Axis.Y:

                    target.localEulerAngles = new Vector3(target.localEulerAngles.x, angle, target.localEulerAngles.z);

                    break;

                case Axis.Z:

                    target.localEulerAngles = new Vector3(target.localEulerAngles.x, target.localEulerAngles.y, angle);

                    break;

                case Axis.All:

                    target.localEulerAngles += Vector3.one * angle;

                    break;
            }
        }
    }

}