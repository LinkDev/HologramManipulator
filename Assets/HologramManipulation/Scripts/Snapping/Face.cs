using UnityEngine;

namespace LinkDev.HologramManipulator.SnappingModule
{
    class Face
    {
        public Vector3 Center { private set; get; }
        public Vector3 Normal { private set; get; }
        public float PerpendicularDepth { private set; get; }
        public float DiagnoalLength { private set; get; }
        public Vector3 ProjectedPivot { private set; get; }
        public Vector3[] CornerPoints { private set; get; }
        private float a, b, c, d;
        private Cuboid m_Parent;
        public Face(Vector3 A, Vector3 B, Vector3 C, Vector3 D, Vector3 cuboidCenter, Vector3 cuboidPivot, float depth, Cuboid parent)
        {
            Center = (A + B + C + D) / 4;
            CornerPoints = new Vector3[] { A, B, C, D };
            Normal = (Center - cuboidCenter).normalized;
            if (Mathf.Approximately(Normal.magnitude, 0))
                Normal = Vector3.Cross(A - B, B - C).normalized;
            a = Normal.x;
            b = Normal.y;
            c = Normal.z;
            d = -(a * A.x + b * A.y + c * A.z);
            ProjectedPivot = Face.ProjectPointOnPlane(Normal, Center, cuboidPivot);
            PerpendicularDepth = depth;
            DiagnoalLength = 0;
            m_Parent = parent;
        }

        public bool CheckPivotApproximation(Face that)
        {
            return false;
        }
        public static bool CheckSnapping(Face first, Face second, float snappingDistance)
        {
            var distance = Face.ParallelDistance(first, second);
            return (distance >= 0 && distance < snappingDistance);
        }

        public static bool CheckSnapping(Face first, Face second, float snappingDistance, out float distance)
        {
            distance = Face.ParallelDistance(first, second);
            return (distance >= 0 && distance < snappingDistance);
        }

        public static bool IsParallel(Face first, Face second)
        {
            return Mathf.Abs(Vector3.Dot(first.Normal, second.Normal)) > 0.96f;
        }

        public static bool IsProjectionInteresected(Face first, Face second)
        {
            var p1 = first.ProjectPointOnPlane(second.CornerPoints[0]);
            if (first.IsPointWithinBoundaries(p1))
                return true;
            var p2 = first.ProjectPointOnPlane(second.CornerPoints[1]);
            if (first.IsPointWithinBoundaries(p2))
                return true;
            var p3 = first.ProjectPointOnPlane(second.CornerPoints[2]);
            if (first.IsPointWithinBoundaries(p3))
                return true;
            var p4 = first.ProjectPointOnPlane(second.CornerPoints[3]);
            if (first.IsPointWithinBoundaries(p4))
                return true;
            return false;
        }

        private static Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
        {
            //Translate the point to form a projection
            return point + GetProjectionVector(planeNormal, planePoint, point);
        }

        public static float Distance(Face first, Face second)
        {
            return Vector3.Distance(first.Center, second.Center);
        }

        public static float ParallelDistance(Face first, Face second)
        {
            if (IsParallel(first, second))
            {
                if (IsProjectionInteresected(first, second) || IsProjectionInteresected(second, first))
                {
                    return Mathf.Abs(Vector3.Dot(new Vector3(first.a, first.b, first.c), second.Center) + first.d);
                }
            }
            return -1;
        }

        private static Vector3 GetProjectionVector(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
        {
            float distance;

            //First calculate the distance from the point to the plane:
            distance = SignedDistancePlanePoint(planeNormal, planePoint, point);

            //Reverse the sign of the distance
            distance *= -1;

            //Get a translation vector
            return SetVectorLength(planeNormal, distance);
        }

        private static float SignedDistancePlanePoint(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
        {
            return Vector3.Dot(planeNormal, (point - planePoint));
        }

        public bool IsPointWithinBoundaries(Vector3 point)
        {
            var A = point;
            var B = CornerPoints[0];
            var C = CornerPoints[1];
            var E = CornerPoints[3];
            ///The equation to determine whether point A is confined within the boundaries of rectangular points B, C, D, E
            ///⟨b,c−b⟩≤⟨a,c−b⟩≤⟨c,c−b⟩ and ⟨b,e−b⟩≤⟨a,e−b⟩≤⟨e,e−b⟩
            return Vector3.Dot(B, C - B) <= Vector3.Dot(A, C - B) && Vector3.Dot(A, C - B) <= Vector3.Dot(C, C - B) &&
                    Vector3.Dot(B, E - B) <= Vector3.Dot(A, E - B) && Vector3.Dot(A, E - B) <= Vector3.Dot(E, E - B);
        }

        public Vector3 ProjectPointOnPlane(Vector3 point)
        {
            return Face.ProjectPointOnPlane(Normal, this.Center, point);
        }

        public Vector3 GetProjectionVector(Face that)
        {
            return Face.GetProjectionVector(this.Normal, this.Center, that.Center);
        }

        public void ShowHighlightOnEntireObject()
        {
            m_Parent.ShowHighlight();
        }

        public void HideHighlightOnEntireObject()
        {
            m_Parent.HideHighlight();
        }
        
        private static Vector3 SetVectorLength(Vector3 vector, float size)
        {

            //normalize the vector
            Vector3 vectorNormalized = Vector3.Normalize(vector);

            //scale the vector
            return vectorNormalized *= size;
        }
    }
}