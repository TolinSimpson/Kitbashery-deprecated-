using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kitbashery
{

    [RequireComponent(typeof(Camera))]
    public class CameraOrbit : MonoBehaviour
    {
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

        private void Awake()
        {
            this.enabled = false;// Make sure this is disabled at start otherwise if the user imports a mesh before inspecting a mesh then the lighting can be rotated in the thumbnail preview (might be a useful bug).
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
    }

}