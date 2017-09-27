using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LinkDev.HologramManipulator
{
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


        [Tooltip("Rotating is done in increments to make rotation less cumbersome you control the threshold of rotation here")]
        public float RotationIncrement = 10f;

        public float ScaleFactor = 8f;
        public float RotateFactor = 800;
        public float TranslateFactor = 2;
        public float MinObjectSize = 0.2f ;
        public float MaxObjectSize = 3;


        [Header ("UI Elements")]
        [Tooltip("Minimum size of the hologram before the UI stops scaling down")]
        public float HologramMinimumSizeForUIScaling = 0.5f;
        [Tooltip("The scaling factor applied to UI relative to the Hologram size")]
        public float UIScalingFactor = 1 / 20f;
        [Tooltip("Minimum scale of UI")]
        public float UIMinimumScale = 1 / 60f;
        [Tooltip("Scale factor of the controller menu relative to regular controllers")]
        public float MenuScaleFactor = 2.5f;
        [Tooltip("Scale factor of the boundary box edges relative to regular controllers")]
        public float BoundaryBoxLineWidthFactor = 0.08f;

        public Color BoxDefaultColor = Color.cyan;
        public Color ActiveControllerColor = Color.yellow;
        public Color HighlightColor = Color.red;

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
            //Kind of hacky solution to find the default settings, since I don't like bloating existing projects
            //with tags and such, I try to find a "naked" gameObject that has ManipulationSettings and consider
            //that the default one
            if (m_DefaultSetting == null && GetComponent<HologramManipulator>() == null)
                m_DefaultSetting = this;
        }
    }
}