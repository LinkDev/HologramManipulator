using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LinkDev
{
    public static class Vector3Extensions
    {
        public static Vector3 Divide(this Vector3 a, Vector3 b)
        {
            if (b.x == 0 || b.y == 0 || b.z == 0)
                throw new DivideByZeroException();
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        public static Vector3 MultiplyX(this Vector3 a, float b)
        {
            return new Vector3(a.x * b, a.y, a.z);
        }

        public static Vector3 MultiplyY(this Vector3 a, float b)
        {
            return new Vector3(a.x , a.y * b, a.z);
        }

        public static Vector3 MultiplyZ(this Vector3 a, float b)
        {
            return new Vector3(a.x, a.y, a.z * b);
        }

        public static float MinComponent (this Vector3 a)
        {
            return Mathf.Min(a.x, a.y, a.z);
        }

        public static float MidComponent(this Vector3 a)
        {
            if (a.x >= a.y && a.x <= a.z || a.x <= a.y && a.x >= a.z)
                return a.x;
            if (a.x >= a.y && a.x <= a.z || a.x <= a.y && a.x >= a.z)
                return a.y;
            return a.z;
        }

        public static float MaxComponent(this Vector3 a)
        {
            return Mathf.Max(a.x, a.y, a.z);
        }


    }
}