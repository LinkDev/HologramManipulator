using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LinkDev.HologramManipulator
{
    public class BoundaryLineController : MonoBehaviour
    {
        public Vector3 StartPoint;
        public Vector3 EndPoint;
        public Color RenderingColor
        {
            set
            {
                m_Renderer.material.color = value;
            }
        }
        private MeshRenderer m_Renderer;
        // Use this for initialization
        private void Awake()
        {
            m_Renderer = GetComponentInChildren<MeshRenderer>();
        }

        /// <summary>
        /// Sets the transform of the boundary box lines to fit between the points stored already
        /// provided to the two public Vector3 points
        /// </summary>
        /// <param name="width"></param>
        public void DrawTube(float width)
        {
            Vector3 center = (StartPoint + EndPoint) / 2;
            Vector3 length = new Vector3(width, (EndPoint - StartPoint).magnitude, width);
            transform.position = center;
            transform.localScale = length;
            transform.up = EndPoint - StartPoint;
        }

        /// <summary>
        /// Sets the transform of the boundary box lines to fit between two points
        /// </summary>
        /// <param name="first">The first point</param>
        /// <param name="second">The second point</param>
        /// <param name="width">The width of the line</param>
        public void DrawTube(Vector3 first, Vector3 second, float width)
        {
            StartPoint = first;
            EndPoint = second;

            Vector3 center = (StartPoint + EndPoint) / 2;
            Vector3 length = new Vector3(width, (EndPoint - StartPoint).magnitude, width);
            if (length.magnitude < 0.01f)
                gameObject.SetActive(false);
            else
            {
                gameObject.SetActive(true);
                transform.position = center;
                transform.localScale = length;
                transform.up = EndPoint - StartPoint;
            }
        }

        /// <summary>
        /// Check if the line provided by the arguments match the line already stored
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public bool CheckMatchingLine(Vector3 start, Vector3 end)
        {
            return ((start == StartPoint && end == EndPoint) || (start == EndPoint && end == StartPoint));
        }
    }
}