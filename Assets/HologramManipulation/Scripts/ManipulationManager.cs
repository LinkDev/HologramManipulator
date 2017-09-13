using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LinkDev.HologramManipulator
{
    public enum Axis { X, Y, Z };
    public enum HologramType { _3D, _2D }
    public enum HologramState { Inactive, Focused, Active, Manipulating };
    public enum SnappingTarget { Off, SpatialOnly, HolographicOnly, SpatialAndHolographic};
    public enum SnappingMode { Pivot, Face, PivotAndFaces};
    public class ManipulationManager : Singleton<ManipulationManager>
    {
        /// <summary>
        /// List of current hologram currently in the scene
        /// </summary>
        //[HideInInspector]
        public List<HologramController> CurrentActiveControllers = new List<HologramController>();

        [Header ("Required components")]
        public GameObject RotateControllerPrefab;
        public GameObject ScaleControllerPrefab;
        public GameObject BoxLinePrefab;

        public float BoundaryBoxLineWidthFactor = 0.08f;
        public float ScaleFactor = 8f;
        public float RotateFactor = 800;
        public float TranslateFactor = 2;
        public float MinObjectSize = 0.2f;
        public float MaxObjectSize = 3;
        public float MenuScaleFactor = 2.5f;

        [Header("Optional components")]
        
        public SnappingTarget SnappingTarget = SnappingTarget.Off;
        public SnappingMode SnappingMode = SnappingMode.PivotAndFaces;
        public float SnappingDistance = 0.05f;

        [Tooltip("GameObject to contain all holograms, intended for organizing the scene hierarchy, null if you want no changes")]
        public GameObject HologramsParent;

        [Tooltip("GameObject to contain all controllers, intended for organizing the scene hierarchy, null if you want no changes")]
        public GameObject ControllersParent;

        public bool IsBaseDrawingEnabled = false;
        [Tooltip("Optional, required for IsBaseDrawingEnabled option")]
        public GameObject BaseLinePrefab;

        [Tooltip("Check comment within the script to proporly use this")]
        /// <summary>
        /// This only works correctly if the hierarchy is designed such as this <see cref="HologramController"/> 
        /// is put on a game object that wrappers the actual hologram to be able to keep the original pivot info
        /// since <see cref="HologramController"/> change the pivot to be able to scale around certain points
        /// and rotate the object around its center not its pivot
        /// </summary>
        public bool IsPivotDrawingEnabled = false;

        [Tooltip("Optional, required for IsPivotDrawingEnabled option")]
        public GameObject PivotPrefab;
        
        public GameObject MenuPrefab;
    }
}