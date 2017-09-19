using HoloToolkit.Unity;
using System;
using System.Linq;
using UnityEngine;

namespace LinkDev.HologramManipulator
{
    /// <summary>
    /// Internal struct to represent the box that the hologram occupies in space and provide
    /// many helper function to facilitate the tracking of the new transforms of each of the
    /// controllers of the hologram
    /// </summary>
    public class BoundaryBox
    {

        private BoxCollider Collider;
        private HologramType HologramType;

        /// <summary>
        /// The position of the hologram controllers
        /// It is different form <see cref="EdgePointsPosition"/> when the hologram is 2D since
        /// it have 4 controllers while the box have 8 edges
        /// from being too small 
        /// </summary>
        public Vector3[] ControllerPointsPosition;
        /// <summary>
        /// The position of the actual hologram edges <para/>
        /// It is different form <see cref="ControllerPointsPosition"/> when the hologram is 2D since
        /// it have 4 controllers while the box have 8 edges
        /// </summary>
        public Vector3[] EdgePointsPosition;
        public Transform Target;

        public float MinimumBoxExtendInWorldSpace;

        public BoundaryBox(Transform target, HologramType hologramType)
        {
            HologramType = hologramType;
            Target = target;
            var collider = Target.EnsureComponent<BoxCollider>();
            Collider = collider;
            collider.isTrigger = true;

            EdgePointsPosition = new Vector3[8];
            switch (HologramType)
            {
                case HologramType._2D:
                    ControllerPointsPosition = new Vector3[4];
                    break;
                case HologramType._3D:
                    ControllerPointsPosition = new Vector3[8];
                    break;
                default:
                    ControllerPointsPosition = new Vector3[8];
                    break;
            }
        }
        public Vector3 Center
        {
            get
            {
                return (Collider) ? Collider.center : Vector3.zero;
            }
            set
            {
                Collider.center = value;
            }
        }
        public Vector3 BottomCenter
        {
            get
            {
                return (Collider) ? Collider.center - new Vector3(0, Collider.size.y / 2, 0) : Vector3.zero;
            }
        }
        public Vector3 TopCenter
        {
            get
            {
                return (Collider) ? Collider.center + new Vector3(0, Collider.size.y / 2, 0) : Vector3.zero;
            }
        }
        public Vector3 Size
        {
            get
            {
                return (Collider) ? Collider.size : Vector3.zero;
            }
            set
            {
                Collider.size = value;
            }
        }
        
        public Vector3 GetContainerBoxSize()
        {
            var maxX = EdgePointsPosition.Max(v => v.x);
            var maxY = EdgePointsPosition.Max(v => v.y);
            var maxZ = EdgePointsPosition.Max(v => v.z);
            var minX = EdgePointsPosition.Min(v => v.x);
            var minY = EdgePointsPosition.Min(v => v.y);
            var minZ = EdgePointsPosition.Min(v => v.z);
            return new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
        }
        public float[] GetContainerBoxLimits()
        {
            var maxX = EdgePointsPosition.Max(v => v.x);
            var maxY = EdgePointsPosition.Max(v => v.y);
            var maxZ = EdgePointsPosition.Max(v => v.z);
            var minX = EdgePointsPosition.Min(v => v.x);
            var minY = EdgePointsPosition.Min(v => v.y);
            var minZ = EdgePointsPosition.Min(v => v.z);
            return new float[] { minX, maxX, minY, maxY, minZ, maxZ };
        }
        public Vector3[] GetContainerBoxEdges()
        {
            return EdgePointsPosition;
        }

        public void UpdateEdgePoints()
        {
            Vector3 boxCenterInObjectSpace = Center;
            Vector3 boxExtendsInObjectSpace = Size / 2;
            Vector3 boxExtendsInWorldSpace = Vector3.Scale(Size, Target.localScale);

            //Get minimum box extends to scale the UI accordingly
            switch (HologramType)
            {
                case HologramType._2D:
                    MinimumBoxExtendInWorldSpace = Get2DHologramMinExtend(boxExtendsInWorldSpace);
                    break;
                case HologramType._3D:
                    MinimumBoxExtendInWorldSpace = Get3DHologramMinExtend(boxExtendsInWorldSpace);
                    break;
            }
            
            
            for (int i = 0; i < EdgePointsPosition.Length; i++)
            {
                Vector3 currentControllerPosInObjectSpace = boxCenterInObjectSpace + Vector3.Scale(boxExtendsInObjectSpace, new Vector3((i & 1) == 0 ? 1 : -1, (i & 2) == 0 ? 1 : -1, (i & 4) == 0 ? 1 : -1));
                EdgePointsPosition[i] = Target.TransformPoint(currentControllerPosInObjectSpace);
            }

            switch (HologramType)
            {
                case HologramType._2D:
                    ControllerPointsPosition = GetEffectiveSquareFromCube(EdgePointsPosition);
                    break;
                case HologramType._3D:
                    ControllerPointsPosition = EdgePointsPosition.Select(x=>x).ToArray();
                    break;
            }
        }

        private float Get2DHologramMinExtend(Vector3 boxSize)
        {
            //This is a 2D hologram, we need skip the smallest dimension of the cube since it will be almost 0
            //The relevant dimension is the second smallest one
            boxSize = new Vector3(Mathf.Abs(boxSize.x), Mathf.Abs(boxSize.y), Mathf.Abs(boxSize.z));
            if (boxSize.x <= boxSize.y && boxSize.x >= boxSize.z || boxSize.x >= boxSize.y && boxSize.x <= boxSize.z)
                return boxSize.x;
            else if (boxSize.y <= boxSize.x && boxSize.y >= boxSize.z || boxSize.y >= boxSize.x && boxSize.y <= boxSize.z)
                return boxSize.y;
            else
                return boxSize.z;
        }

        private float Get3DHologramMinExtend(Vector3 boxSize)
        {
            return Mathf.Min(Mathf.Abs(boxSize.x), Mathf.Abs(boxSize.y), Mathf.Abs(boxSize.z));
        }
        

        /// <summary>
        /// In 2D holograms, one dimension is ignored; this function collapses the 3D cuboid into 2D rectangle
        /// </summary>
        /// <param name="cubeEdges"> 8 points of the cuboid</param>
        /// <returns>4 points of the rectangle</returns>
        private Vector3[] GetEffectiveSquareFromCube(Vector3[] cuboidEdges)
        {
            Vector3[] squarePoints = null;
            //The cuboid has three main face {0, 1, 3, 2}, {0, 2, 6, 4}, {0, 1, 5, 4}
            var A = Vector3.Distance(cuboidEdges[0], cuboidEdges[1]);
            var B = Vector3.Distance(cuboidEdges[0], cuboidEdges[2]);
            var C = Vector3.Distance(cuboidEdges[0], cuboidEdges[4]);
            
            if (A * B >= B * C && A * B >= A * C)
                squarePoints = new Vector3[] { cuboidEdges[0], cuboidEdges[1], cuboidEdges[3], cuboidEdges[2] };
            else if (B * C >= A * B && B * C >= A * C)
                squarePoints = new Vector3[] { cuboidEdges[0], cuboidEdges[2], cuboidEdges[6], cuboidEdges[4] };
            else
                squarePoints = new Vector3[] { cuboidEdges[0], cuboidEdges[1], cuboidEdges[5], cuboidEdges[4] };

            return squarePoints;
        }

        /// <summary>
        /// Get the world position of specific edge of the box
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="edgeID"></param>
        /// <returns></returns>
        public Vector3 GetEdgePointPosition(Transform parent, int edgeID)
        {
            Vector3 boxColliderCenter = Center;
            Vector3 boxColliderExtents = Size / 2;

            //get one of vertices offset from center
            Vector3 ext = boxColliderExtents;
            ext.Scale(new Vector3((edgeID & 1) == 0 ? 1 : -1, (edgeID & 2) == 0 ? 1 : -1, (edgeID & 4) == 0 ? 1 : -1));
            //calc local vertice position
            Vector3 vertPositionLocal = boxColliderCenter + ext;
            //move controller to global vertice position
            Vector3 currentPos = Vector3.zero;
            currentPos = parent.TransformPoint(vertPositionLocal);

            return currentPos;
        }

        /// <summary>
        /// Calculate the projection of the hologram box on the Y-Axis
        /// </summary>
        /// <param name="cornerPoints">The four points that defines the rectangular projection</param>
        /// <param name="centerPoints">The four points in the center of the edges of the rectangle</param>
        /// <param name="padding">Padding to make the projection slightly larger to prevent overlapping with hologram itself</param>
        public void CalculateProjection(out Vector3[] cornerPoints, out Vector3[] centerPoints, float padding)
        {
            var maxX = EdgePointsPosition.Max(v => v.x);
            var maxZ = EdgePointsPosition.Max(v => v.z);
            var minX = EdgePointsPosition.Min(v => v.x);
            var minZ = EdgePointsPosition.Min(v => v.z);
            var minY = EdgePointsPosition.Min(v => v.y);
            var p1 = new Vector3(maxX, minY, maxZ) + new Vector3(+padding, 0, +padding);
            var p2 = new Vector3(maxX, minY, minZ) + new Vector3(+padding, 0, -padding);
            var p3 = new Vector3(minX, minY, minZ) + new Vector3(-padding, 0, -padding);
            var p4 = new Vector3(minX, minY, maxZ) + new Vector3(-padding, 0, +padding);

            cornerPoints = new Vector3[] { p1, p2, p3, p4 };

            centerPoints = new Vector3[] { (p1 + p2) / 2, (p2 + p3) / 2, (p3 + p4) / 2, (p4 + p1) / 2 };
        }
    }
}