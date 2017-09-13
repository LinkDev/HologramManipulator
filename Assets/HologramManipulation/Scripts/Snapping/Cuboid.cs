using UnityEngine;

namespace LinkDev.HologramManipulator.SnappingModule
{
    class Cuboid
    {
        public Vector3 Pivot { private set; get; }
        public Face[] Faces { private set; get; }

        private IHighlightable m_Target;
        private Vector3[] m_CornerPoints;

        public Cuboid(float[] extendsLimits, IHighlightable target, Vector3? pivot = null)
        {
            m_CornerPoints = new Vector3[8];
            Faces = new Face[6];
            float minX = extendsLimits[0];
            float maxX = extendsLimits[1];
            float minY = extendsLimits[2];
            float maxY = extendsLimits[3];
            float minZ = extendsLimits[4];
            float maxZ = extendsLimits[5];
            float midX = (minX + maxX) / 2;
            float midY = (minY + maxY) / 2;
            float midZ = (minZ + maxZ) / 2;
            ///0, 1, 2, 3
            ///4, 5, 6, 7
            ///2, 3, 4, 5
            ///6, 7, 0, 1
            ///1, 2, 5, 6
            ///0, 3, 4, 7
            var p0 = new Vector3(maxX, maxY, maxZ);
            var p1 = new Vector3(maxX, maxY, minZ);
            var p2 = new Vector3(maxX, minY, minZ);
            var p3 = new Vector3(maxX, minY, maxZ);
            var p4 = new Vector3(minX, minY, maxZ);
            var p5 = new Vector3(minX, minY, minZ);
            var p6 = new Vector3(minX, maxY, minZ);
            var p7 = new Vector3(minX, maxY, maxZ);
            m_CornerPoints = new Vector3[] { p0, p1, p2, p3, p4, p5, p6, p7 };
            var c = new Vector3(midX, midY, midZ);

            Pivot = pivot.HasValue ? pivot.Value : c;
            Faces[0] = new Face(p0, p1, p2, p3, c, Pivot, maxX - minX, this);
            Faces[1] = new Face(p4, p5, p6, p7, c, Pivot, maxX - minX, this);
            Faces[2] = new Face(p2, p3, p4, p5, c, Pivot, maxY - minY, this);
            Faces[3] = new Face(p6, p7, p0, p1, c, Pivot, maxY - minY, this);
            Faces[4] = new Face(p1, p2, p5, p6, c, Pivot, maxZ - minZ, this);
            Faces[5] = new Face(p0, p3, p4, p7, c, Pivot, maxZ - minZ, this);

            m_Target = target;
        }

        public Cuboid(Vector3[] edgePoints, IHighlightable target, Vector3? pivot = null)
        {
            m_CornerPoints = new Vector3[8];
            Faces = new Face[6];

            ///0, 1, 3, 2
            ///4, 5, 7, 6
            ///2, 3, 7, 6
            ///0, 1, 5, 4
            ///1, 5, 7, 3
            ///0, 4, 6, 2

            m_CornerPoints = edgePoints;
            Vector3 center = Vector3.zero;
            for (int i = 0; i < m_CornerPoints.Length; i++)
                center += m_CornerPoints[i];
            center /= m_CornerPoints.Length;
            Pivot = pivot.HasValue ? pivot.Value : center;
            float xDepth = ((edgePoints[0] + edgePoints[1] + edgePoints[3] + edgePoints[2]) / 4 - (edgePoints[4] + edgePoints[5] + edgePoints[7] + edgePoints[6]) / 4).magnitude;
            float yDepth = ((edgePoints[2] + edgePoints[3] + edgePoints[7] + edgePoints[6]) / 4 - (edgePoints[0] + edgePoints[1] + edgePoints[5] + edgePoints[4]) / 4).magnitude;
            float zDepth = ((edgePoints[1] + edgePoints[5] + edgePoints[7] + edgePoints[3]) / 4 - (edgePoints[0] + edgePoints[4] + edgePoints[6] + edgePoints[2]) / 4).magnitude;
            Faces[0] = new Face(edgePoints[0], edgePoints[1], edgePoints[3], edgePoints[2], center, Pivot, xDepth, this);
            Faces[1] = new Face(edgePoints[4], edgePoints[5], edgePoints[7], edgePoints[6], center, Pivot, xDepth, this);
            Faces[2] = new Face(edgePoints[2], edgePoints[3], edgePoints[7], edgePoints[6], center, Pivot, yDepth, this);
            Faces[3] = new Face(edgePoints[0], edgePoints[1], edgePoints[5], edgePoints[4], center, Pivot, yDepth, this);
            Faces[4] = new Face(edgePoints[1], edgePoints[5], edgePoints[7], edgePoints[3], center, Pivot, zDepth, this);
            Faces[5] = new Face(edgePoints[0], edgePoints[4], edgePoints[6], edgePoints[2], center, Pivot, zDepth, this);

            m_Target = target;
        }

        public Cuboid(Bounds bounds)
        {
            float minX = bounds.min.x;
            float minY = bounds.min.y;
            float minZ = bounds.min.z;
            float maxX = bounds.max.x;
            float maxY = bounds.max.y;
            float maxZ = bounds.max.z;
            float midX = (minX + maxX) / 2;
            float midY = (minY + maxY) / 2;
            float midZ = (minZ + maxZ) / 2;

            var p0 = new Vector3(maxX, maxY, maxZ);
            var p1 = new Vector3(maxX, maxY, minZ);
            var p2 = new Vector3(maxX, minY, minZ);
            var p3 = new Vector3(maxX, minY, maxZ);
            var p4 = new Vector3(minX, minY, maxZ);
            var p5 = new Vector3(minX, minY, minZ);
            var p6 = new Vector3(minX, maxY, minZ);
            var p7 = new Vector3(minX, maxY, maxZ);
            m_CornerPoints = new Vector3[] { p0, p1, p2, p3, p4, p5, p6, p7 };

            Pivot = new Vector3(midX, midY, midZ);

            Faces = new Face[6];
            Faces[0] = new Face(p0, p1, p2, p3, Pivot, Pivot, maxX - minX, this);
            Faces[1] = new Face(p4, p5, p6, p7, Pivot, Pivot, maxX - minX, this);
            Faces[2] = new Face(p2, p3, p4, p5, Pivot, Pivot, maxY - minY, this);
            Faces[3] = new Face(p6, p7, p0, p1, Pivot, Pivot, maxY - minY, this);
            Faces[4] = new Face(p1, p2, p5, p6, Pivot, Pivot, maxZ - minZ, this);
            Faces[5] = new Face(p0, p3, p4, p7, Pivot, Pivot, maxZ - minZ, this);

            m_Target = null;
        }
        public static float Distance(Cuboid first, Cuboid second)
        {
            return Vector3.Distance(first.Pivot, second.Pivot);
        }

        public static float GetClosedFaces(Cuboid first, Cuboid second, float snappingDistance, out int firstFace, out int secondFace)
        {
            float minDistance = float.MaxValue, currentDistance = 0;
            firstFace = -1;
            secondFace = -1;
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 6; j++)
                    if (Face.CheckSnapping(first.Faces[i], second.Faces[j], snappingDistance, out currentDistance))
                        if (currentDistance < minDistance)
                        {
                            minDistance = currentDistance;
                            firstFace = i;
                            secondFace = j;
                        }

            return minDistance;
        }

        public static float GetClosedFaces(Face face, Cuboid cuboid, float snappingDistance, out int snappingFaceID)
        {
            float minDistance = float.MaxValue, currentDistance = 0;
            snappingFaceID = -1;
            for (int i = 0; i < 6; i++)
                if (Face.CheckSnapping(face, cuboid.Faces[i], snappingDistance, out currentDistance))
                    if (currentDistance < minDistance)
                    {
                        minDistance = currentDistance;
                        snappingFaceID = i;
                    }
            return minDistance;
        }


        public void GetClosestFace(Cuboid that, out int firstFace, out int secondFace)
        {
            float minDistance = float.MinValue, currentDistance = float.MinValue;
            firstFace = -1;
            secondFace = -1;
            for (int i = 0; i < 6; i++)
            {
                //face.Distance(that.Faces[i])
                firstFace = this.Faces.IndexOfMinBy(face => Face.Distance(face, that.Faces[i]), out currentDistance);
                if (currentDistance < minDistance)
                {
                    minDistance = currentDistance;
                    secondFace = i;
                }
            }
        }

        public void ShowHighlight()
        {
            if (m_Target != null)
                m_Target.ShowHighlight();
        }

        public void HideHighlight()
        {
            if (m_Target != null)
                m_Target.HideHighlight();
        }

        public void ChangeHighlightColor(Color newColor)
        {
            if (m_Target != null)
                m_Target.ChangeHighlightColor(newColor);
        }

        public void HighlightFace(Face target, Color highlightFaceColor, Color normalFaceColor)
        {
            m_Target.HighlightFace(target.CornerPoints, Color.red, Color.cyan);
        }

        public void DebugDraw()
        {
            Faces[0].DebugDraw(Color.green);
            Faces[1].DebugDraw(Color.green);
            Faces[2].DebugDraw(Color.green);
            Faces[3].DebugDraw(Color.green);
            Faces[4].DebugDraw(Color.green);
            Faces[5].DebugDraw(Color.green);
        }
    }
}