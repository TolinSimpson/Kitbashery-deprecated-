using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Kitbashery © 2020 Tolin Simpson 
//Code released under GNU General Public License v3.0 see licnese file for full details.

namespace Kitbashery
{
    /// <summary>
    /// Manages camera movement, raycasts and rendering.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class KB_CameraManager : MonoBehaviour
    {
        #region Variables:

        [Header("Other Managers:")]
        public KB_UIManager uiManager;
        public KB_ObjectManager objectManager;

        [Header("Camera Orbit Settings:")]
        public Camera orbitCam;
        public Transform target;

        public bool uvView = false;
        public Vector3 uvViewOffset = new Vector3(0.45f, 0.5f, -1f);
        public Quaternion uvViewRot = new Quaternion(0, 0, 0, 0);

        [Range(0.5f, 1000)]
        public float distance = 5;

        public float speed = 5;

        public float mouseX = 0;
        public float mouseY = 0;

        [HideInInspector]
        [Range(-360, 360)]
        public float mouseYConstraint = 360;


        [Header("Camera Render Settings:")]
        public Camera renderCam;
        public int workingResolution = 1024;

        [Header("Camera Raycast Settings:")]
        public LayerMask mask = 8;
        private RaycastHit hit;


        #endregion

        #region Initialization & Updates:

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void LateUpdate()
        {
            if (orbitCam.enabled == true)
            {
                Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0);
                float scroll = Input.GetAxis("Mouse ScrollWheel");

                distance = Mathf.Clamp(distance - scroll * Mathf.Max(1.0f, distance), 0.5f, 1000);
                Vector3 reverse = Vector3.forward * -distance;
                Vector3 finalPos = rotation * reverse;

                if (uvView == true)
                {
                    orbitCam.orthographicSize = distance;

                    transform.rotation = uvViewRot;
                    if (target != null)
                    {
                        transform.position = target.position + uvViewOffset;
                    }
                    else
                    {
                        transform.position = uvViewOffset;
                    }

                }
                else
                {
                    if (Input.GetMouseButton(1) == true)
                    {
                        Cursor.visible = false;

                        float deltaX = Input.GetAxis("Mouse X") * speed;
                        float deltaY = Input.GetAxis("Mouse Y") * speed;

                        mouseX += deltaX;
                        mouseY -= deltaY;

                        if (mouseY < -360)
                        {
                            mouseY += 360;
                        }
                        if (mouseY > 360)
                        {
                            mouseY -= 360;
                        }
                        mouseY = Mathf.Clamp(mouseY, -mouseYConstraint, mouseYConstraint);
                    }
                    else
                    {
                        Cursor.visible = true;
                    }

                    /*if (target != null)
                    {
                        finalPos += target.position;
                    }*/

                    transform.rotation = rotation;
                    transform.position = finalPos;
                }
            }
        }

        #endregion

        #region Core Functions:

        public RaycastHit Raycast()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
            {

                //Debug.Log("Pixel Coordinates: ( " + Mathf.RoundToInt(hit.textureCoord.x * workingResolution) + ", " + Mathf.RoundToInt(hit.textureCoord.y * workingResolution) + " )");

            }
            else
            {
                // hit nothing...
            }

            return hit;
        }

        public void FitCameraToMeshBounds(MeshFilter filter, float offset)
        {
            distance = filter.sharedMesh.bounds.size.x + offset;
        }

        public void ToggleUVView(bool uvViewToggle)
        {
            uvView = uvViewToggle;
            orbitCam.orthographic = uvViewToggle;

            if (uvViewToggle == true)
            {
                mouseYConstraint = 0;
                distance = 0.6f;
            }
            else
            {
                if (mouseYConstraint == 0)
                {
                    FitCameraToMeshBounds(objectManager.inspected.filter, 5);
                    mouseYConstraint = 360;
                }
            }

        }

        #endregion
    }
}