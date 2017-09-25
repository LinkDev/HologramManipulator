// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using LinkDev.HologramManipulator.SnappingModule;

namespace LinkDev.HologramManipulator.InputModule
{
    /// <summary>
    /// This is <see cref="HandDraggable"/> from MRToolKit with slight modification, we had 
    /// a sperate copy for consistency and we had made some modification in our project for 
    /// this script
    /// <para/>
    /// Component that allows dragging an object with your hand on HoloLens.
    /// Dragging is done by calculating the angular delta and z-delta between the current and previous hand positions,
    /// and then repositioning the object based on that.
    /// </summary>
    public class HandTranslating : MonoBehaviour,
                                     IFocusable,
                                     IInputHandler,
                                     ISourceStateHandler
    {
        /// <summary>
        /// Event triggered when dragging starts.
        /// </summary>
        public event Action StartedDragging;

        /// <summary>
        /// Event triggered when dragging stops.
        /// </summary>
        public event Action StoppedDragging;

        private Transform HostTransform;
        
        /// <summary>
        /// Scale by which hand movement in z is multipled to move the dragged object, determined 
        /// from <see cref="ManipulatorSettings.ScaleFactor"/> 
        /// </summary>
        public float DistanceScale = 2f;

        public enum RotationModeEnum
        {
            Default,
            LockObjectRotation,
            OrientTowardUser,
            OrientTowardUserAndKeepUpright
        }

        public RotationModeEnum RotationMode = RotationModeEnum.Default;

        [Range(0.01f, 1.0f)]
        public float PositionLerpSpeed = 0.2f;

        [Range(0.01f, 1.0f)]
        public float RotationLerpSpeed = 0.2f;

        public bool IsDraggingEnabled = true;

        private Camera mainCamera;
        public bool isDragging;
        private bool isSnapping;
        private bool isGazed;
        private Vector3 objRefForward;
        private Vector3 objRefUp;
        private float objRefDistance;
        private Quaternion gazeAngularOffset;
        private float handRefDistance;
        private Vector3 objRefGrabPoint;

        private Vector3 draggingPosition;
        private Quaternion draggingRotation;

        private IInputSource currentInputSource = null;
        private uint currentInputSourceId;

        #region Snapping Variables
        private ManipulatorSettings m_Settings;
        private HologramManipulator m_BoundaryController;
        private int? m_PeerIndex;
        private List<Cuboid> m_NormalPeers;
        private List<Cuboid> m_SpatialPeers;
        public int? thisFaceIndex;
        private Face thatFace;
        private Face thatAdjacentFace
        {
            set
            {
                if (thatFace != null)
                    thatFace.HideHighlightOnEntireObject();
                thatFace = value;
                if (thatFace != null)
                    thatFace.ShowHighlightOnEntireObject();
            }
            get
            {
                return thatFace;
            }
        }
        #endregion

        public void Init(Action startedDragging, Action stoppedDragging, RotationModeEnum rotationMode, bool isDraggingEnabled, HologramManipulator hologram, ManipulatorSettings manipulationSettings)
        {
            StartedDragging += startedDragging;
            StoppedDragging += stoppedDragging;
            RotationMode = rotationMode;
            IsDraggingEnabled = isDraggingEnabled;
            DistanceScale = manipulationSettings.TranslateFactor;
            m_Settings = manipulationSettings;
            m_BoundaryController = GetComponent<HologramManipulator>();
        }

        private void Start()
        {
            if (HostTransform == null)
            {
                HostTransform = transform;
            }

            mainCamera = Camera.main;
        }


        private void OnDestroy()
        {
            if (isDragging)
            {
                StopDragging();
            }

            if (isGazed)
            {
                OnFocusExit();
            }
        }

        private void Update()
        {
            if (IsDraggingEnabled && isDragging)
            {
                UpdateDragging();
            }
        }

        /// <summary>
        /// Starts dragging the object.
        /// </summary>
        public void StartDragging()
        {
            if (!IsDraggingEnabled)
            {
                return;
            }

            if (isDragging)
            {
                return;
            }

            // Add self as a modal input handler, to get all inputs during the manipulation
            InputManager.Instance.PushModalInputHandler(gameObject);

            isDragging = true;
            //GazeCursor.Instance.SetState(GazeCursor.State.Move);
            //GazeCursor.Instance.SetTargetObject(HostTransform);

            Vector3 gazeHitPosition = GazeManager.Instance.HitInfo.point;
            Vector3 handPosition;
            currentInputSource.TryGetPosition(currentInputSourceId, out handPosition);

            Vector3 pivotPosition = GetHandPivotPosition();
            handRefDistance = Vector3.Magnitude(handPosition - pivotPosition);
            objRefDistance = Vector3.Magnitude(gazeHitPosition - pivotPosition);

            Vector3 objForward = HostTransform.forward;
            Vector3 objUp = HostTransform.up;

            // Store where the object was grabbed from
            objRefGrabPoint = mainCamera.transform.InverseTransformDirection(HostTransform.position - gazeHitPosition);

            Vector3 objDirection = Vector3.Normalize(gazeHitPosition - pivotPosition);
            Vector3 handDirection = Vector3.Normalize(handPosition - pivotPosition);

            objForward = mainCamera.transform.InverseTransformDirection(objForward);       // in camera space
            objUp = mainCamera.transform.InverseTransformDirection(objUp);                 // in camera space
            objDirection = mainCamera.transform.InverseTransformDirection(objDirection);   // in camera space
            handDirection = mainCamera.transform.InverseTransformDirection(handDirection); // in camera space

            objRefForward = objForward;
            objRefUp = objUp;

            // Store the initial offset between the hand and the object, so that we can consider it when dragging
            gazeAngularOffset = Quaternion.FromToRotation(handDirection, objDirection);
            draggingPosition = gazeHitPosition;

            InitSnapping();
            StartedDragging.RaiseEvent();
        }

        /// <summary>
        /// Gets the pivot position for the hand, which is approximated to the base of the neck.
        /// </summary>
        /// <returns>Pivot position for the hand.</returns>
        private Vector3 GetHandPivotPosition()
        {
            Vector3 pivot = Camera.main.transform.position + new Vector3(0, -0.2f, 0) - Camera.main.transform.forward * 0.2f; // a bit lower and behind
            return pivot;
        }

        /// <summary>
        /// Enables or disables dragging.
        /// </summary>
        /// <param name="isEnabled">Indicates whether dragging shoudl be enabled or disabled.</param>
        public void SetDragging(bool isEnabled)
        {
            if (IsDraggingEnabled == isEnabled)
            {
                return;
            }

            IsDraggingEnabled = isEnabled;

            if (isDragging)
            {
                StopDragging();
            }
        }

        /// <summary>
        /// Update the position of the object being dragged.
        /// </summary>
        private void UpdateDragging()
        {
            Vector3 newHandPosition;
            currentInputSource.TryGetPosition(currentInputSourceId, out newHandPosition);

            Vector3 pivotPosition = GetHandPivotPosition();

            Vector3 newHandDirection = Vector3.Normalize(newHandPosition - pivotPosition);

            newHandDirection = mainCamera.transform.InverseTransformDirection(newHandDirection); // in camera space
            Vector3 targetDirection = Vector3.Normalize(gazeAngularOffset * newHandDirection);
            targetDirection = mainCamera.transform.TransformDirection(targetDirection); // back to world space

            float currenthandDistance = Vector3.Magnitude(newHandPosition - pivotPosition);

            float distanceRatio = currenthandDistance / handRefDistance;
            float distanceOffset = distanceRatio > 0 ? (distanceRatio - 1f) * DistanceScale : 0;
            float targetDistance = objRefDistance + distanceOffset;

            draggingPosition = pivotPosition + (targetDirection * targetDistance);

            if (RotationMode == RotationModeEnum.OrientTowardUser || RotationMode == RotationModeEnum.OrientTowardUserAndKeepUpright)
            {
                draggingRotation = Quaternion.LookRotation(HostTransform.position - pivotPosition);
            }
            else if (RotationMode == RotationModeEnum.LockObjectRotation)
            {
                draggingRotation = HostTransform.rotation;
            }
            else // RotationModeEnum.Default
            {
                Vector3 objForward = mainCamera.transform.TransformDirection(objRefForward); // in world space
                Vector3 objUp = mainCamera.transform.TransformDirection(objRefUp);   // in world space
                draggingRotation = Quaternion.LookRotation(objForward, objUp);
            }

            // Apply Final Position
            HostTransform.position = Vector3.Lerp(HostTransform.position, draggingPosition + mainCamera.transform.TransformDirection(objRefGrabPoint), PositionLerpSpeed);

            // Apply Final Rotation
            HostTransform.rotation = Quaternion.Lerp(HostTransform.rotation, draggingRotation, RotationLerpSpeed);

            if (RotationMode == RotationModeEnum.OrientTowardUserAndKeepUpright)
            {
                Quaternion upRotation = Quaternion.FromToRotation(HostTransform.up, Vector3.up);
                HostTransform.rotation = upRotation * HostTransform.rotation;
            }
            UpdateSnapping();
        }
        
        /// <summary>
        /// Stops dragging the object.
        /// </summary>
        public void StopDragging()
        {
            if (!isDragging)
            {
                return;
            }
            EndSnapping();

            // Remove self as a modal input handler
            InputManager.Instance.PopModalInputHandler();

            isDragging = false;
            currentInputSource = null;
            StoppedDragging.RaiseEvent();
        }

        #region Snapping Functions
        private void InitSnapping ()
        {
            if (m_BoundaryController != null && HologramManipulator.CurrentActiveHolograms.Contains(m_BoundaryController))
            {
                List<HologramManipulator> peers = null;
                List<SurfacePlane> surfaces = null;

                m_NormalPeers = null;
                m_SpatialPeers = null;
                switch (m_Settings.SnappingTarget)
                {
                    case SnappingTarget.Off:
                        break;
                    case SnappingTarget.SpatialOnly:
                        surfaces = GameObject.FindObjectsOfType<SurfacePlane>().ToList();
                        surfaces.RemoveAll(surface => surface.PlaneType == PlaneTypes.Unknown);
                        break;
                    case SnappingTarget.HolographicOnly:
                        peers = HologramManipulator.CurrentActiveHolograms.FindAll(obj => obj.HologramType == HologramType._3D);
                        peers.Remove(m_BoundaryController);
                        break;
                    case SnappingTarget.SpatialAndHolographic:
                        peers = HologramManipulator.CurrentActiveHolograms.FindAll(obj => obj.HologramType == HologramType._3D); ;
                        peers.Remove(m_BoundaryController);

                        surfaces = GameObject.FindObjectsOfType<HoloToolkit.Unity.SpatialMapping.SurfacePlane>().ToList();
                        surfaces.RemoveAll(surface => surface.PlaneType == PlaneTypes.Unknown);
                        break;
                }
                if (peers != null)
                {
                    m_NormalPeers = new List<Cuboid>();
                    for (int i = 0; i < peers.Count; i++)
                        m_NormalPeers.Add(new Cuboid(peers[i].GetBoundaryEdges(), peers[i], peers[i].OriginalPivot()));
                }
                if (surfaces != null)
                {
                    m_SpatialPeers = new List<Cuboid>();
                    for (int i = peers.Count; i < peers.Count + surfaces.Count; i++)
                        m_SpatialPeers.Add(new Cuboid(surfaces[i - peers.Count].GetComponent<MeshRenderer>().bounds));
                }
            }
        }
        private void UpdateSnapping(bool DraggingEnded = false)
        {
            Vector3 snappingDisplacement = Vector3.zero;
            switch (m_Settings.SnappingTarget)
            {
                case SnappingTarget.Off:
                    return;
                case SnappingTarget.HolographicOnly:
                    HolographicSnapping(out snappingDisplacement);
                    break;
                case SnappingTarget.SpatialOnly:
                    SpatialSnapping(out snappingDisplacement);
                    break;
                case SnappingTarget.SpatialAndHolographic:
                    //The order of calls determine the priorty of snapping target, calling spatial last means that
                    //spatial snapping will take precedence over holographic snapping
                    HolographicSnapping(out snappingDisplacement);
                    SpatialSnapping(out snappingDisplacement);
                    break;
            }
           

            if (!isSnapping || DraggingEnded)
                ((IHighlightable)m_BoundaryController).ChangeHighlightColor(Color.cyan);
            HostTransform.position += snappingDisplacement;
        }

        private void EndSnapping()
        {
            if (isSnapping)
            {
                thisFaceIndex = null;
                thatAdjacentFace = null;
                UpdateSnapping(true);
                isSnapping = false;
            }
        }

        private void HolographicSnapping(out Vector3 snappingDisplacement)
        {
            snappingDisplacement = Vector3.zero;
            if (m_NormalPeers != null && m_NormalPeers.Count > 0)
            {
                var thisCuboid = new Cuboid(m_BoundaryController.GetBoundaryEdges(), m_BoundaryController, m_BoundaryController.OriginalPivot());

                switch (m_Settings.SnappingMode)
                {
                    case SnappingMode.Pivot:
                        //Check if there is pivot snapping
                        TrySnapPivot(thisCuboid, m_NormalPeers, out snappingDisplacement);
                        break;
                    case SnappingMode.Face:
                        //Check if there is pivot snapping
                        TrySnapFaces(thisCuboid, m_NormalPeers, out snappingDisplacement);
                        break;
                    case SnappingMode.PivotAndFaces:
                        //The order of calls determine the priorty of snapping target, calling TrySnapPivot last means that
                        //pivot snapping will take precedence over face snapping
                        TrySnapFaces(thisCuboid, m_NormalPeers, out snappingDisplacement);
                        TrySnapPivot(thisCuboid, m_NormalPeers, out snappingDisplacement);
                        break;
                }
            }
        }
        private void SpatialSnapping(out Vector3 snappingDisplacement)
        {
            snappingDisplacement = Vector3.zero;
            if (m_SpatialPeers != null && m_SpatialPeers.Count > 0)
            {
                var thisCuboid = new Cuboid(m_BoundaryController.GetBoundaryEdges(), m_BoundaryController, m_BoundaryController.OriginalPivot());
                //If not we check if no previous faces were snapping we check if there are snapping faces and try to snap them
                TrySnapFaces(thisCuboid, m_SpatialPeers, out snappingDisplacement);
            }
        }
        private bool TrySnapPivot(Cuboid thisCuboid, List<Cuboid> otherCuboids, out Vector3 Displacement)
        {
            Displacement = Vector3.zero;
            float minDistance;
            if (m_PeerIndex == null)
                m_PeerIndex = otherCuboids.IndexOfMinBy(peer => Vector3.Distance(thisCuboid.Pivot, peer.Pivot), out minDistance);
            else
                minDistance = Vector3.Distance(thisCuboid.Pivot, otherCuboids[m_PeerIndex.Value].Pivot);

            if (minDistance <= m_Settings.SnappingDistance)
            {
                Displacement = otherCuboids[m_PeerIndex.Value].Pivot - thisCuboid.Pivot;
                isSnapping = true;
                thisCuboid.ChangeHighlightColor(new Color(1, 0.5f, 0));
                otherCuboids[m_PeerIndex.Value].ChangeHighlightColor(new Color(1, 0.5f, 0));
                return true;
            }
            if (m_PeerIndex != null)
                otherCuboids[m_PeerIndex.Value].ChangeHighlightColor(Color.cyan);

            m_PeerIndex = null;
            isSnapping = false;
            return false;
        }

        private bool TrySnapFaces(Cuboid thisCuboid, List<Cuboid> otherCuboids, out Vector3 Displacement)
        {
            Displacement = Vector3.zero;
            if (thisFaceIndex == null && thatAdjacentFace == null)
                thatAdjacentFace = GetClosestAdjacentFace(thisCuboid, otherCuboids, out thisFaceIndex);

            if (thatAdjacentFace == null)
            {
                isSnapping = false;
                return false;
            }


            var thisFace = thisCuboid.Faces[thisFaceIndex.Value];
            var thatFace = thatAdjacentFace;
            if (Face.CheckSnapping(thisFace, thatFace, m_Settings.SnappingDistance))
            {
                if (Vector3.Distance(thisFace.ProjectedPivot, thatFace.ProjectedPivot) < m_Settings.SnappingDistance)
                    Displacement = thatFace.ProjectedPivot - thisFace.ProjectedPivot;
                else
                    Displacement = thatFace.GetProjectionVector(thisFace);

                thisCuboid.HighlightFace(thisFace);
                isSnapping = true;
                return true;
            }
            else
            {
                thisFaceIndex = null;
                thatAdjacentFace = null;
                isSnapping = false;
                return false;
            }

        }

        private Face GetClosestAdjacentFace(Cuboid thisCuboid, List<Cuboid> otherCuboids, out int? thisFaceIndex)
        {
            thisFaceIndex = null;
            thatFace = null;
            float currentDistance, minDistance = float.MaxValue; int currentThisIndex, currentThatIndex;
            for (int i = 0; i < otherCuboids.Count; i++)
            {
                currentDistance = Cuboid.GetClosedFaces(thisCuboid, otherCuboids[i], m_Settings.SnappingDistance, out currentThisIndex, out currentThatIndex);
                if (currentDistance >= 0 && currentDistance < minDistance)
                {
                    minDistance = currentDistance;
                    thisFaceIndex = currentThisIndex;
                    thatFace = otherCuboids[i].Faces[currentThatIndex];
                }
            }
            return thatFace;
        }
        #endregion

        #region Event Handlers
        public void OnFocusEnter()
        {
            if (!IsDraggingEnabled)
            {
                return;
            }

            if (isGazed)
            {
                return;
            }

            isGazed = true;
        }

        public void OnFocusExit()
        {
            if (!IsDraggingEnabled)
            {
                return;
            }

            if (!isGazed)
            {
                return;
            }

            isGazed = false;
        }

        public void OnInputUp(InputEventData eventData)
        {
            if (currentInputSource != null && eventData.SourceId == currentInputSourceId)
            {
                StopDragging();
            }
        }

        public void OnInputDown(InputEventData eventData)
        {
            if (isDragging)
            {
                // We're already handling drag input, so we can't start a new drag operation.
                return;
            }

            if (!eventData.InputSource.SupportsInputInfo(eventData.SourceId, SupportedInputInfo.Position))
            {
                // The input source must provide positional data for this script to be usable
                return;
            }

            currentInputSource = eventData.InputSource;
            currentInputSourceId = eventData.SourceId;
            StartDragging();
        }

        public void OnSourceDetected(SourceStateEventData eventData)
        {
            // Nothing to do
        }

        public void OnSourceLost(SourceStateEventData eventData)
        {
            if (currentInputSource != null && eventData.SourceId == currentInputSourceId)
            {
                StopDragging();
            }
        }
        #endregion
    }
}