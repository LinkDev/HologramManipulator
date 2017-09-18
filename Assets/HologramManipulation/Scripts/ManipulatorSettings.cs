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
    public class ManipulatorSettings : MonoBehaviour
    {
        /// <summary>
        /// List of current hologram currently in the scene
        /// </summary>

        private static ManipulatorSettings m_DefaultSetting;
        public static ManipulatorSettings DefaultSetting
        {
            get
            {
                if (m_DefaultSetting == null)
                    Debug.LogError("No DefaultSetting exist in the scene and no ManipulationSettings attached to object");
                return m_DefaultSetting;
            }
        }

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

        public bool EnableBaseDrawing = false;
        [Tooltip("Optional, required for IsBaseDrawingEnabled option")]
        public GameObject BaseLinePrefab;

        [Tooltip("Check comment within the script to proporly use this")]
        /// <summary>
        /// This only works correctly if the hierarchy is designed such as this <see cref="HologramManipulator"/> 
        /// is put on a game object that wrappers the actual hologram to be able to keep the original pivot info
        /// since <see cref="HologramManipulator"/> change the pivot to be able to scale around certain points
        /// and rotate the object around its center not its pivot
        /// </summary>
        public bool EnablePivotDrawing = false;

        [Tooltip("Optional, required for IsPivotDrawingEnabled option")]
        public GameObject PivotPrefab;

        [Tooltip("Toggles the usage of controller menu on a hologram by hologram basis")]
        public bool EnableControllerMenu = true;
        public GameObject ControllerMenuPrefab;


        [Tooltip("Toggles the registeration of a hologram in a list that tracks all of them")]
        public bool RegisterInHologramsList = true;

        public void Awake()
        {
            if (m_DefaultSetting == null && GetComponent<HologramManipulator>() == null)
                m_DefaultSetting = this;
        }
    }
}