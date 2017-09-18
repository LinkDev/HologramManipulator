using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using LinkDev.HologramManipulator.InputModule;

namespace LinkDev.HologramManipulator
{

    /// <summary>
    /// HologramController is responsible for creating the hologram controllers and adjusting
    /// its transform, recommended to put this script on a game object that wraps the actual hologram
    /// because it change the position to certain key location of hologram (its center/edges) to be
    /// able to scale around edges or rotate around the center point
    /// Dependant on <see cref="ManipulatorSettings"/> existing on the scene<Ma>
    /// </summary>
    public class HologramManipulator : MonoBehaviour, IFocusable, IInputClickHandler, IHighlightable
    {
        public static List<HologramManipulator> CurrentActiveHolograms = new List<HologramManipulator>();
        private static HologramManipulator m_CurrentActiveHologram;

        public HologramType HologramType;


        private float m_MinScaleFactor;
        private float m_MaxScaleFactor;

        private bool m_IsInitialized = false;

        private HologramState m_HologramState;

        private GameObject m_MenuInstance;
        private IMenuController m_MenuController;
        private GameObject m_PivotInstance;
        private HandScaling[] m_ScaleControllerInstances;
        private HandRotating[] m_RotateControllerInstances;
        private HandTranslating m_TranslateControllerInstance;
        private GameObject[] m_Children;
        private Renderer[] m_Renderers;
        private BoundaryLineController[] m_BoundaryLines;
        private BoundaryLineController[] m_BoundaryBase;
        private BoundaryBox m_BoundaryBox;
        private List<IEnumerable<int>> m_CubeFaces = new List<IEnumerable<int>>();

        private ManipulatorSettings m_ManipulationSettings;
        /// <summary>
        /// Parent of all controllers of the hologram to contain all of them in a tidier way
        /// </summary>
        private GameObject m_ManipulatorsContainer;
        
        
        public Vector3 GetBoundsSize()
        {
            return m_BoundaryBox.GetContainerBoxSize();
        }

        public Vector3 OriginalPivot()
        {
            return transform.GetChild(0).position;
        }
        public float[] GetBoundaryLimits()
        {
            return m_BoundaryBox.GetContainerBoxLimits();
        }

        public Vector3[] GetBoundaryEdges()
        {
            return m_BoundaryBox.GetContainerBoxEdges();
        }

        private void Start()
        {
            InitHologram(HologramType);
        }

        public void InitHologram(HologramType contentType)
        {
            HologramType = contentType;

            m_ManipulationSettings = GetComponent<ManipulatorSettings>();
            if (m_ManipulationSettings == null)
                m_ManipulationSettings = ManipulatorSettings.DefaultSetting;


            if (!m_IsInitialized && m_ManipulationSettings != null)
            {
                SetChildrenAndRenderers();
                InitBoundaries();
                SetHologramFaces();
                SetParentGameObjects();
                InitControllers();
                UpdateParentPosition(transform.TransformPoint(m_BoundaryBox.BottomCenter));
                
                UpdateObjectManipulators(false);
                PlaceMenu();
                SetHologramState(m_HologramState);

                RegisterHologramInActiveHologramList();

                m_IsInitialized = true;
            }
            
        }

        /// <summary>
        /// Register the Hologram in the <see cref="ManipulatorSettings.CurrentActiveControllers"/>
        /// used mainly for snapping operations
        /// </summary>
        private void RegisterHologramInActiveHologramList()
        {
            if (m_ManipulationSettings.RegisterInHologramsList)
                HologramManipulator.CurrentActiveHolograms.Add(this);
        }
        
        /// <summary>
        /// First main part of the initialization process; it initializes the boundary box, 
        /// set the hologram to appropriate size if it exceeds/falls short of a certain threshold 
        /// </summary>
        private void InitBoundaries()
        {
            m_BoundaryBox = new BoundaryBox(transform, HologramType);

            switch (HologramType)
            {
                case HologramType._2D:
                    Init2DHologramBoundaries();
                    break;
                case HologramType._3D:
                    Init3DHologramBoundaries();
                    break;
            }
            CalculateBoundaries();
            AdjustBoundarySize();
            
            //if (m_ScaleFactor == null)
               //m_ScaleFactor = transform.localScale;
        }
        
        private void Init2DHologramBoundaries()
        {
            m_ScaleControllerInstances = new HandScaling[4];
            m_RotateControllerInstances = new HandRotating[4];
            m_BoundaryLines = new BoundaryLineController[4];
            m_BoundaryBase = new BoundaryLineController[4];
        }

        private void Init3DHologramBoundaries()
        {
            m_ScaleControllerInstances = new HandScaling[8];
            m_RotateControllerInstances = new HandRotating[12];
            m_BoundaryLines = new BoundaryLineController[12];
            m_BoundaryBase = new BoundaryLineController[4];
        }
        
        /// <summary>
        /// Save this GameObject children, to be able to change the parent object position easily 
        /// </summary>
        private void SetChildrenAndRenderers()
        {
            m_Children = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                m_Children[i] = transform.GetChild(i).gameObject;
            m_Renderers = GetComponentsInChildren<MeshRenderer>(true);
        }

        /// <summary>
        /// Construct array of points that represent the face of the hologram, used for applying operation on a specific face
        /// </summary>
        private void SetHologramFaces()
        {
            m_CubeFaces = new List<IEnumerable<int>>();

            switch (HologramType)
            {
                case HologramType._2D:
                    Set2DHologramFaces();
                    break;
                case HologramType._3D:
                    Set3DHologramFaces();
                    break;
            }
        }

        private void Set2DHologramFaces()
        {
            m_CubeFaces.Add(new[] { 0, 1, 2, 3 });
        }

        private void Set3DHologramFaces()
        {
            m_CubeFaces.Add(new[] { 0, 1, 3, 2 });
            m_CubeFaces.Add(new[] { 3, 2, 6, 7 });
            m_CubeFaces.Add(new[] { 6, 7, 5, 4 });
            m_CubeFaces.Add(new[] { 5, 4, 0, 1 });
            m_CubeFaces.Add(new[] { 1, 5, 7, 3 });
            m_CubeFaces.Add(new[] { 0, 4, 6, 2 });
        }
        
        /// <summary>
        /// Set the transform of the hologram/its manipulator parent transform to make the scene
        ///explorer in unity tidier and avoid strange cases of input cascading
        ///Remove this code or edit it at leisure
        /// </summary>
        private void SetParentGameObjects()
        {
            m_ManipulatorsContainer = new GameObject("Manipulators Container");
            
            if (m_ManipulationSettings.HologramsParent)
                transform.SetParent(m_ManipulationSettings.HologramsParent.transform);
            if (m_ManipulationSettings.ControllersParent)
                m_ManipulatorsContainer.transform.SetParent(m_ManipulationSettings.ControllersParent.transform, false);
        }
        /// <summary>
        /// Second part of the initialization, it creates the appropriate number of controllers (4/8)
        /// the hologram menu if provided, and place them in their correct initial position
        /// </summary>
        private void InitControllers()
        {
            //Init Pivot
            if (m_ManipulationSettings.PivotPrefab != null)
                m_PivotInstance = Instantiate(m_ManipulationSettings.PivotPrefab, m_ManipulatorsContainer.transform);

            //Init Menu Controller
            if (m_ManipulationSettings.EnableControllerMenu && m_ManipulationSettings.ControllerMenuPrefab)
            {
                m_MenuInstance = Instantiate(m_ManipulationSettings.ControllerMenuPrefab, m_ManipulatorsContainer.transform);
                if (m_MenuInstance)
                {
                    m_MenuController = m_MenuInstance.GetComponentInChildren<IMenuController>(true);
                    if (m_MenuController != null)
                        m_MenuController.Init(gameObject);
                }
            }

            //Init Hologram Base
            if (m_ManipulationSettings.EnableBaseDrawing && m_ManipulationSettings.BaseLinePrefab != null)
                for (int i = 0; i < 4; i++)
                    m_BoundaryBase[i] = Instantiate(m_ManipulationSettings.BaseLinePrefab, m_ManipulatorsContainer.transform).GetComponent<BoundaryLineController>();

            //Init Translate controllers
            m_TranslateControllerInstance = gameObject.AddComponent<HandTranslating>();
            m_TranslateControllerInstance.Init(DragStartedEventHandler, DragEndedEventHandler, HandTranslating.RotationModeEnum.LockObjectRotation, true, this, m_ManipulationSettings);

            //Init Rotation and Scale controllers
            switch (HologramType)
            {
                case HologramType._2D:
                    Init2DHologramControllers();
                    break;
                case HologramType._3D:
                    Init3DHologramControllers();
                    break;
            }
        }
        private void Init2DHologramControllers()
        {
            for (int i = 0; i < 4; i++)
            {
                var currentScaleController = Instantiate(m_ManipulationSettings.ScaleControllerPrefab, m_ManipulatorsContainer.transform).GetComponentInChildren<HandScaling>();
                currentScaleController.Init(ScaleStartedEventHandler, ScaleEndedEventHandler, transform, m_ManipulationSettings.ScaleFactor, i, (i + 2) % 4, m_MinScaleFactor, m_MaxScaleFactor);
                m_ScaleControllerInstances[i] = (currentScaleController);

                var currentRotateController = Instantiate(m_ManipulationSettings.RotateControllerPrefab, m_ManipulatorsContainer.transform).GetComponentInChildren<HandRotating>();
                currentRotateController.Init(RotateStartedEventHandler, RotateEndedEventHandler, transform, m_ManipulationSettings.RotateFactor);
                m_RotateControllerInstances[i] = (currentRotateController);
                if (i % 2 == 1)
                    m_RotateControllerInstances[i].RotationAxis = Axis.X;
                else
                    m_RotateControllerInstances[i].RotationAxis = Axis.Y;

                m_BoundaryLines[i] = Instantiate(m_ManipulationSettings.BoxLinePrefab, m_ManipulatorsContainer.transform).GetComponent<BoundaryLineController>();
            }

        }

        private void Init3DHologramControllers()
        {
            for (int i = 0; i < 12; i++)
            {
                if (i < 8)
                {
                    var currentScaleController = Instantiate(m_ManipulationSettings.ScaleControllerPrefab, m_ManipulatorsContainer.transform).GetComponentInChildren<HandScaling>();
                    currentScaleController.Init(ScaleStartedEventHandler, ScaleEndedEventHandler, transform, m_ManipulationSettings.ScaleFactor, i, 7 - i, m_MinScaleFactor, m_MaxScaleFactor);
                    m_ScaleControllerInstances[i] = (currentScaleController);
                }
                var currentRotateController = Instantiate(m_ManipulationSettings.RotateControllerPrefab, m_ManipulatorsContainer.transform).GetComponentInChildren<HandRotating>();
                currentRotateController.Init(RotateStartedEventHandler, RotateEndedEventHandler, transform, m_ManipulationSettings.RotateFactor);
                m_RotateControllerInstances[i] = (currentRotateController);
                if (i < 4)
                    m_RotateControllerInstances[i].RotationAxis = Axis.X;
                else if (i < 8)
                    m_RotateControllerInstances[i].RotationAxis = Axis.Y;
                else
                    m_RotateControllerInstances[i].RotationAxis = Axis.Z;
                m_BoundaryLines[i] = Instantiate(m_ManipulationSettings.BoxLinePrefab, m_ManipulatorsContainer.transform).GetComponent<BoundaryLineController>(); ;
            }

        }

        /// <summary>
        /// Function to set the stat the hologram and update its UI to reflect the updated state
        /// </summary>
        /// <param name="value">Current state of the hologram</param>
        public void SetHologramState(HologramState value)
        {
            bool isBaseEnabled = false, isMenuEnabled = false, isControllersEnabled = false, isBoundaryEnabled = false, isPivotEnabled;

            switch (value)
            {
                case HologramState.Inactive:
                    SetHologramStateFlagsForInactive(out isBaseEnabled, out isMenuEnabled, out isControllersEnabled, out isBoundaryEnabled);
                    break;
                case HologramState.Focused:
                    SetHologramStateFlagsForFocused(out isBaseEnabled, out isMenuEnabled, out isControllersEnabled, out isBoundaryEnabled);
                    break;
                case HologramState.Active:
                    SetHologramStateFlagsForActive(out isBaseEnabled, out isMenuEnabled, out isControllersEnabled, out isBoundaryEnabled);
                    break;
                
            }

            isPivotEnabled = m_ManipulationSettings && m_ManipulationSettings.EnablePivotDrawing;
            isBaseEnabled &= m_ManipulationSettings && m_ManipulationSettings.EnableBaseDrawing;

            SetControllerState(isBaseEnabled, isMenuEnabled, isControllersEnabled, isBoundaryEnabled, isPivotEnabled);
           
            m_HologramState = value;
        }

        private void SetHologramStateFlagsForInactive(out bool isBaseEnabled, out bool isMenuEnabled, out bool isControllersEnabled, out bool isBoundaryEnabled)
        {
            isBaseEnabled = false;
            isMenuEnabled = false;
            isControllersEnabled = false;
            isBoundaryEnabled = false;
        }
        private void SetHologramStateFlagsForFocused(out bool isBaseEnabled, out bool isMenuEnabled, out bool isControllersEnabled, out bool isBoundaryEnabled)
        {
            isBaseEnabled = true;
            isMenuEnabled = false;
            isControllersEnabled = false;
            isBoundaryEnabled = true;
        }
        private void SetHologramStateFlagsForActive(out bool isBaseEnabled, out bool isMenuEnabled, out bool isControllersEnabled, out bool isBoundaryEnabled)
        {
            isBaseEnabled = false;
            isMenuEnabled = true;
            isControllersEnabled = true;
            isBoundaryEnabled = true;
        }

        private void SetControllerState (bool isBaseEnabled, bool isMenuEnabled, bool isControllersEnabled, bool isBoundaryEnabled, bool isPivotEnabled)
        {
            if (m_MenuInstance)
                m_MenuInstance.SetActive(isMenuEnabled);
            foreach (var instance in m_ScaleControllerInstances)
                if (instance)
                    instance.gameObject.SetActive(isControllersEnabled);
            foreach (var instance in m_RotateControllerInstances)
                if (instance)
                    instance.gameObject.SetActive(isControllersEnabled);
            if (m_TranslateControllerInstance)
                m_TranslateControllerInstance.enabled = isControllersEnabled;
            foreach (var instance in m_BoundaryLines)
                if (instance)
                    instance.gameObject.SetActive(isBoundaryEnabled);

            if (m_PivotInstance)
                m_PivotInstance.SetActive(isPivotEnabled);

            foreach (var obj in m_BoundaryBase)
                if (obj)
                    obj.gameObject.SetActive(isBaseEnabled);
        }

        #region ControllersToggles
        /// <summary>
        /// Enable or disable the hologram controllers, used mainly for two scenarios:
        /// <para/>
        /// -Disable the non used controllers on the hologram when manipulation starts on one of them
        /// and enable them again after manipulation is finished
        /// <para/>
        /// -Disable other holograms' controllers entirely when manipulation starts on one of them and
        /// enable them again after manipulation is finished
        /// <para/> This done to avoid finicky input when manipulation hologram to be on top of each others
        /// </summary>
        /// <param name="state"></param>
        public void SetHologramControllersState(bool state)
        {
            if (state)
            {
                foreach (var obj in m_RotateControllerInstances)
                    if (obj != null)
                        obj.enabled = true;
                foreach (var obj in m_ScaleControllerInstances)
                    if (obj != null)
                        obj.enabled = true;
                if (m_TranslateControllerInstance)
                    m_TranslateControllerInstance.enabled = true;
            }
            else
            { 
                if (!m_TranslateControllerInstance.isDragging)
                    m_TranslateControllerInstance.enabled = false;
                foreach (var instance in m_RotateControllerInstances)
                    if (!instance.IsManipulating)
                        instance.enabled = false;
                foreach (var instance in m_ScaleControllerInstances)
                    if (!instance.IsManipulating)
                        instance.enabled = false;
            }
        }

        /// <summary>
        /// Used to disable other active holograms, used mainly to disable them while manipulating
        /// another hologram to avoid input handling error
        /// </summary>
        /// <param name="state"></param>
        public void SetOtherActiveHologramsState(bool state)
        {
            if (HologramManipulator.CurrentActiveHolograms != null)
            {
                foreach (var obj in HologramManipulator.CurrentActiveHolograms)
                {
                    if (obj != this && obj.m_HologramState == HologramState.Active)
                    {
                        obj.SetHologramControllersState(state);
                        obj.SetHologramState(HologramState.Inactive);
                    }
                }
            }
        }
        #endregion

        #region BoundaryCalculation
        /// <summary>
        /// Calcuate the current boundary of the hologram using its <see cref="MeshRenderer"/> boundaries
        /// and update the underlying <see cref="BoundaryBox"/> class
        /// </summary>
        private void CalculateBoundaries()
        {
            var combinedBounds = GetBoundsFromMesh(false);

            m_BoundaryBox.Center = combinedBounds.center;
            m_BoundaryBox.Size = combinedBounds.size;
        }

        /// <summary>
        /// Get the collective bounds of the meshes of the object
        /// </summary>
        /// <param name="respectScale">Flag to indicate whether to get the bounds size at scale (1, 1, 1) or respect the current scale of object</param>
        /// <returns></returns>
        private Bounds GetBoundsFromMesh(bool respectScale)
        {
            Vector3 currentPosition = transform.position;
            Quaternion currentRotation = transform.rotation;
            Vector3 currentScale = transform.localScale;

            SetTransfromValue(transform, Vector3.zero, Quaternion.identity, (respectScale) ? (Vector3?)null : Vector3.one);

            Bounds combinedBounds = m_Renderers[0].bounds;
            for (int i = 1; i < m_Renderers.Length; i++)
                combinedBounds.Encapsulate(m_Renderers[i].bounds);

            
            SetTransfromValue(transform, currentPosition, currentRotation, currentScale);

            return combinedBounds;
        }

        /// <summary>
        /// Reset hologram to its native size
        /// </summary>
        public void ResetToOriginalScale()
        {
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Adjust the size of the hologram to not exceed or fall off short a certain size
        /// defined in <see cref="ManipulatorSettings"/>
        /// </summary>
        private void AdjustBoundarySize()
        {
            var combinedBounds = GetBoundsFromMesh(true);

            float scaleFactor = 1;
            var maxBoundSide = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
            //var minBoundSize = Mathf.Min(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
            m_MinScaleFactor = m_ManipulationSettings.MinObjectSize / maxBoundSide;
            m_MaxScaleFactor = m_ManipulationSettings.MaxObjectSize / maxBoundSide;
            if (maxBoundSide < m_ManipulationSettings.MinObjectSize)
                scaleFactor = m_ManipulationSettings.MinObjectSize / maxBoundSide;
            else if (maxBoundSide > m_ManipulationSettings.MaxObjectSize)
                scaleFactor = m_ManipulationSettings.MaxObjectSize / maxBoundSide;
            transform.localScale *= scaleFactor;
        }
        #endregion BoundaryCalculation
        
        #region Update Functions
        private void LateUpdate()
        {
            if (m_IsInitialized)
            {
                if (m_HologramState != HologramState.Inactive)
                {
                    UpdateObjectManipulators(true);
                    CalculateBoundaries();
                }
                PlaceMenu();
            }
        }

        /// <summary>
        /// Change the location of the pivot of the hologram, useful for scaling and rotating
        /// because scaling requires scaling around a specifc point (the opposite edge of the controller being used)
        /// and rotating requires rotating around the center of the object
        /// </summary>
        /// <param name="newPosition"></param>
        private void UpdateParentPosition(Vector3 newPosition)
        {
            if (Vector3.Distance(newPosition, transform.position) == 0f)
                return;
            foreach (GameObject child in m_Children)
                child.transform.parent = null;
            Vector3 oldWorldSpaceColliderCenterPosition = transform.TransformPoint(m_BoundaryBox.Center);
            transform.position = newPosition;
            m_BoundaryBox.Center = transform.InverseTransformPoint(oldWorldSpaceColliderCenterPosition);
            foreach (GameObject child in m_Children)
                child.transform.SetParent(transform, true);
        }

        /// <summary>
        /// Update the location of the internal boundary box the reflect the current transform of the hologram
        /// and use that info to update the hologram controller transform if necessary
        /// </summary>
        /// <param name="drawingEnabled">Indicate whether the controller are to be updated</param>
        private void UpdateObjectManipulators(bool drawingEnabled)
        {
            m_BoundaryBox.UpdateEdgePoints();

            SetTransfromValue(m_PivotInstance.transform, OriginalPivot(), transform.rotation, Vector3.one * m_BoundaryBox.EdgePointsScale);

            for (int i = 0; i < m_ScaleControllerInstances.Length; i++)
                SetTransfromValue(m_ScaleControllerInstances[i].transform, m_BoundaryBox.ControllerPointsPosition[i], m_BoundaryBox.EdgePointsQuaternion[i], Vector3.one * m_BoundaryBox.EdgePointsScale);

            if (drawingEnabled)
            {
                switch (HologramType)
                {
                    case HologramType._2D:
                        PlaceBoundingBoxFor2DHologram();
                        break;
                    case HologramType._3D:
                        PlaceBoundingBoxFor3DHologram();
                        break;
                }
            }
        }
        
        private void PlaceBoundingBoxFor2DHologram()
        {
            for (int i = 0; i < m_ScaleControllerInstances.Length; i++)
            {
                m_BoundaryLines[i].DrawTube(m_BoundaryBox.ControllerPointsPosition[i], m_BoundaryBox.ControllerPointsPosition[(i + 1) % m_ScaleControllerInstances.Length], m_BoundaryBox.ScaleFactor * m_ManipulationSettings.BoundaryBoxLineWidthFactor);
                SetTransfromValue(m_RotateControllerInstances[i].transform, m_BoundaryLines[i].transform.position, m_BoundaryBox.EdgePointsQuaternion[i], Vector3.one * m_BoundaryBox.EdgePointsScale);
            }
        }

        private void PlaceBoundingBoxFor3DHologram()
        {
            for (int i = 0, j = 0, x = 0, y = 0, z = 2; i < m_ScaleControllerInstances.Length; i++)
            {
                //First Pair of horizontal points {(0, 1), (2, 3), (4, 5), (6, 7)}
                if (i % 2 == 0)
                {
                    m_BoundaryLines[j].DrawTube(m_BoundaryBox.ControllerPointsPosition[i], m_BoundaryBox.ControllerPointsPosition[i + 1], m_BoundaryBox.ScaleFactor * m_ManipulationSettings.BoundaryBoxLineWidthFactor);
                    SetTransfromValue(m_RotateControllerInstances[x].transform, m_BoundaryLines[j].transform.position, m_BoundaryBox.EdgePointsQuaternion[i], Vector3.one * m_BoundaryBox.EdgePointsScale);
                    j++;
                    x++;
                }

                //Vertical Pairs { (0, 2), (1, 3), (4, 6), (5, 7)}
                if (i % 4 < 2)
                {
                    m_BoundaryLines[j].DrawTube(m_BoundaryBox.ControllerPointsPosition[i], m_BoundaryBox.ControllerPointsPosition[i + 2], m_BoundaryBox.ScaleFactor * m_ManipulationSettings.BoundaryBoxLineWidthFactor);
                    //These are the vertical tube, so we draw the Y rotation controller in there center
                    SetTransfromValue(m_RotateControllerInstances[y + 4].transform, m_BoundaryLines[j].transform.position, m_BoundaryBox.EdgePointsQuaternion[i], Vector3.one * m_BoundaryBox.EdgePointsScale);
                    j++;
                    y++;
                }

                // Second direction of horizontal Pairs { (0, 4), (1, 5), (2, 6), (3, 7)}
                if (i < 4)
                {
                    m_BoundaryLines[j].DrawTube(m_BoundaryBox.ControllerPointsPosition[i], m_BoundaryBox.ControllerPointsPosition[i + 4], m_BoundaryBox.ScaleFactor * m_ManipulationSettings.BoundaryBoxLineWidthFactor);
                    SetTransfromValue(m_RotateControllerInstances[z + 8].transform, m_BoundaryLines[j].transform.position, m_BoundaryBox.EdgePointsQuaternion[i], Vector3.one * m_BoundaryBox.EdgePointsScale);
                    j++;
                    z = ++z % 4;
                }
            }
        }
        
        /// <summary>
        /// Set the transform of the menu
        /// The location of the menu is determined by projecting the boundaries of the hologram
        /// on the Y-Axis to get get a rectangle that represents its base, then we calculate the
        /// four points in the middle of the rectangle corners and place the menu in the location
        /// of the closest one to the viewer, the menu is oriented to always face the user but
        /// it is always perpendicular on the XZ plane
        /// </summary>
        private void PlaceMenu()
        {
            Vector3[] centerPoints = null, cornerPoints = null;
            if ((m_MenuInstance != null && m_MenuInstance.activeSelf) || m_ManipulationSettings.EnableBaseDrawing)
                m_BoundaryBox.CalculateProjection(out cornerPoints, out centerPoints, m_BoundaryBox.EdgePointsScale);

            if ((m_MenuInstance != null && m_MenuInstance.activeSelf))
            {
                var index = centerPoints.IndexOfMinBy(v => Vector3.Distance(v, Camera.main.transform.position));
                m_MenuInstance.transform.position = centerPoints[index];
                m_MenuInstance.transform.localScale = Vector3.one * m_BoundaryBox.EdgePointsScale * m_ManipulationSettings.MenuScaleFactor;

                var f = centerPoints[index] - (centerPoints[mod(index + 1, 4)] + centerPoints[mod(index - 1, 4)]) / 2;
                f = Vector3.Dot(f, Camera.main.transform.forward) < 0 ? f : -f;
                m_MenuInstance.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            }
            if (m_ManipulationSettings.EnableBaseDrawing)
                DrawBaseProjection(cornerPoints, m_BoundaryBox.EdgePointsScale / 2, m_BoundaryBox.ScaleFactor * m_ManipulationSettings.BoundaryBoxLineWidthFactor);
        }

        /// <summary>
        /// Draw the projection of the hologram below it
        /// </summary>
        /// <param name="p">The four points of projection</param>
        /// <param name="thickness">The width of the lines used to draw the projection</param>
        /// <param name="height">The height of the lines used to draw the projection</param>
        private void DrawBaseProjection(Vector3[] p, float thickness, float height)
        {
            for (int i = 0; i < 4; i++)
            {
                m_BoundaryBase[i].DrawTube(p[i], p[(i + 1) % 4], thickness);
            }
        }
        #endregion
        
        #region ManipulatorEventHandlers
        private void ScaleStartedEventHandler(int edgePressed)
        {
            ManipulationStarted();
            int target = m_ScaleControllerInstances[edgePressed].AdjacentPointID;
            UpdateParentPosition(m_ScaleControllerInstances[target].transform.position);
        }
        private void ScaleEndedEventHandler()
        {
            ManipulationEnded();
            UpdateParentPosition(transform.TransformPoint(m_BoundaryBox.BottomCenter));
        }
        private void RotateStartedEventHandler()
        {
            ManipulationStarted();
            UpdateParentPosition(transform.TransformPoint(m_BoundaryBox.Center));
        }
        private void RotateEndedEventHandler()
        {
            ManipulationEnded();
            UpdateParentPosition(transform.TransformPoint(m_BoundaryBox.BottomCenter));
        }
        private void DragStartedEventHandler()
        {
            ManipulationStarted();
        }
        private void DragEndedEventHandler()
        {
            ManipulationEnded();
        }

        private void ManipulationStarted()
        {
            if (m_MenuController != null)
                m_MenuController.SetState(false);
            SetOtherActiveHologramsState(false);
            SetHologramControllersState(false);

            ///TODO: Fire any events that would trigger when manipulation of this object starts here
        }
        private void ManipulationEnded()
        {
            if (m_MenuController != null)
                m_MenuController.SetState(true);
            SetHologramControllersState(true);
            ///TODO: Fire any events that would trigger when manipulation of this object ends here
        }
        #endregion ManipulatorEventHandlers

        #region Objects Cleanup
        private void OnDestroy()
        {
            if (m_IsInitialized)
            {
                if (HologramManipulator.CurrentActiveHolograms != null && m_ManipulationSettings.RegisterInHologramsList)
                    HologramManipulator.CurrentActiveHolograms.Remove(this);
                
                Destroy(m_ManipulatorsContainer);
            }
        }

        private void OnDisable()
        {
            if (m_IsInitialized)
                SetHologramState(HologramState.Inactive);
        }
        private void OnEnable()
        {
            if (m_IsInitialized)
                SetHologramState(HologramState.Inactive);
        }
        #endregion

        #region Interaction Event Handlers
        void IFocusable.OnFocusEnter()
        {
            if (m_IsInitialized && m_HologramState == HologramState.Inactive)
                SetHologramState(HologramState.Focused);
        }

        void IFocusable.OnFocusExit()
        {
            if (m_IsInitialized && m_HologramState == HologramState.Focused)
                SetHologramState(HologramState.Inactive);
        }

        void IInputClickHandler.OnInputClicked(InputClickedEventData eventData)
        {
            if (m_IsInitialized && m_HologramState == HologramState.Focused)
            {
                SetHologramState(HologramState.Active);
                m_TranslateControllerInstance.IsDraggingEnabled = true;

                ///TODO: Fire any events that would trigger when this object is clicked here
                m_CurrentActiveHologram = this;
                foreach (var hologram in CurrentActiveHolograms)
                    if (hologram != this)
                        hologram.SetHologramState(HologramState.Inactive);
            }
        }
        #endregion

        #region Highlight Handlers
        void IHighlightable.ChangeHighlightColor(Color newColor)
        {
            foreach (var instance in m_BoundaryLines)
                if (instance)
                    instance.RenderingColor = newColor;
        }
        void IHighlightable.ShowHighlight()
        {
            foreach (var instance in m_BoundaryLines)
                if (instance && instance.gameObject)
                    instance.gameObject.SetActive(true);
        }

        void IHighlightable.HideHighlight()
        {
            foreach (var instance in m_BoundaryLines)
                if (instance && instance.gameObject)
                    instance.gameObject.SetActive(false);
        }

        void IHighlightable.HighlightFace(Vector3[] pointsArray, Color highlightFaceColor, Color normalFaceColor)
        {
            if (pointsArray.Length != 4)
                throw new ArgumentException();
            var points = pointsArray.ToList();
            var lines = m_BoundaryLines.ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                for (int j = 0; j < points.Count; j++)
                    if (lines[i].CheckMatchingLine(points[j], points[(j + 1) % points.Count]))
                    {
                        lines[i].RenderingColor = highlightFaceColor;
                        lines.RemoveAt(i--);
                        break;
                        //points.RemoveAt(j--);
                    }
            }
            for (int i = 0; i < lines.Count; i++)
                lines[i].RenderingColor = normalFaceColor;
        }
        #endregion

        #region Helper Functions
        /// <summary>
        /// Set the transform of the hologram using provided info
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="sca"></param>
        public void SetTargetTransform(Vector3 pos, Quaternion rot, Vector3 sca)
        {
            transform.localPosition = pos;
            transform.localRotation = rot;
            transform.localScale = sca;
        }
        /// <summary>
        /// Set the transform of the hologram using provided info
        /// Can provide nullable info to the function
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="sca"></param>
        public void SetTargetTransform(Vector3? pos, Quaternion? rot, Vector3? sca)
        {
            if (pos.HasValue)
                transform.localPosition = pos.Value;
            if (rot.HasValue)
                transform.localRotation = rot.Value;
            if (sca.HasValue)
                transform.localScale = sca.Value;
        }
        
        /// <summary>
        /// retrieve the current transform data of the hologram
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="sca"></param>
        public void GetTargetTranform(out Vector3 pos, out Quaternion rot, out Vector3 sca)
        {
            pos = transform.localPosition;
            rot = transform.localRotation;
            sca = transform.localScale;
        }

        private int mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        private void SetTransfromValue (Transform target, Vector3? pos, Quaternion? rot, Vector3? sca)
        {
            if (pos.HasValue)
                target.localPosition = pos.Value;
            if (rot.HasValue)
                target.localRotation = rot.Value;
            if (sca.HasValue)
                target.localScale = sca.Value;
        }

        #endregion

    }
}