// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using System;
using System.Collections.Generic;
using System.Collections;
namespace LinkDev.HologramManipulator.InputModule
{
    /// <summary>
    /// Component that allows dragging an object with your hand on HoloLens.
    /// Dragging is done by calculating the angular delta and z-delta between the current and previous hand positions,
    /// and then repositioning the object based on that.
    /// </summary>
    /// 
    

    #region WithManipulation
    public class HandRotating : MonoBehaviour, IManipulationHandler
#if UNITY_EDITOR
                                , IInputClickHandler
#endif
    {
        private Transform HostTransform;

        /// <summary>
        /// The speed of change of the object rotation determined from <see cref="ManipulatorSettings.RotateFactor"/> 
        /// </summary>
        private float RotateFactor;

        public event Action RotateEventStarted;
        public event Action RotateEventEnded;

        public Renderer ElementRenderer;
        public Axis RotationAxis = Axis.Y;
        public Axis ManipulationAxis;
        
        /// <summary>
        /// Rotating is done in increments to make rotation less cumbersome you control the threshold of rotation here 
        /// </summary>
        private float AngleIncrement = 10;
        private float cumulativeChange = 0, oldCumulativeDelta = 0;
        public bool IsManipulating = false;
        public int ID;

        public void Init(Action rotateEventStartedHandler, Action rotateEventEndedHandler, Transform targetTransform, float rotateFactor)
        {
            RotateEventStarted += rotateEventStartedHandler;
            RotateEventEnded += rotateEventEndedHandler;
            HostTransform = targetTransform;
            RotateFactor = rotateFactor;
        }

        void IManipulationHandler.OnManipulationStarted(ManipulationEventData eventData)
        {
            InputManager.Instance.PushModalInputHandler(gameObject);
            IsManipulating = true;
            cumulativeChange = 0;
            RotateEventStarted();
            if (ManipulationAxis == Axis.X)
                oldCumulativeDelta = eventData.CumulativeDelta.x;
            else
                oldCumulativeDelta = eventData.CumulativeDelta.y;

            ManipulationAxis = GetManipulationAxis();
        }

        void IManipulationHandler.OnManipulationUpdated(ManipulationEventData eventData)
        {
            if (ManipulationAxis == Axis.X)
            {
                cumulativeChange += (eventData.CumulativeDelta.x - oldCumulativeDelta) * RotateFactor;
                oldCumulativeDelta = eventData.CumulativeDelta.x;
            }
            else
            {
                cumulativeChange += (eventData.CumulativeDelta.y - oldCumulativeDelta) * RotateFactor;
                oldCumulativeDelta = eventData.CumulativeDelta.y;
            }

            if (Mathf.Abs(cumulativeChange / AngleIncrement) > 1)
            {
                Vector3 d = HostTransform.position - Camera.main.transform.position;
                float sign = Vector3.Dot(Vector3.forward, d);
                sign = (sign < 0) ? 1 : -1;

                HostTransform.RotateAround(HostTransform.position, GetRotationAxis(), AngleIncrement * Mathf.Sign(cumulativeChange) * sign);
                cumulativeChange = 0;
            }
        }

        private Axis GetManipulationAxis()
        {
            var rotationAroundAxis = GetRotationAxis();
            Vector3 projectVector = Vector2.zero;

            var projectionPlaneNormal = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);
            projectVector = Vector3.ProjectOnPlane(rotationAroundAxis, projectionPlaneNormal);

            if (Mathf.Abs(projectVector.x) > Mathf.Abs(projectVector.y))
                return Axis.Y;
            else
                return Axis.X;
        }

        private Vector3 GetRotationAxis()
        {
            switch (RotationAxis)
            {
                case Axis.X:
                    return -HostTransform.right;
                case Axis.Y:
                    return HostTransform.up;
                case Axis.Z:
                    return HostTransform.forward;
                default:
                    return Vector3.zero;
            }
        }
        /// <summary>
        /// Calculation to ensure the direction of hand movement is consistent with the actual rotation of the object
        /// </summary>
        /// <returns></returns>
        private int GetSign()
        {
            var A = Camera.main.transform.position;
            var B = HostTransform.position;
            var C = transform.position;
            float sign = 0;
            switch (RotationAxis)
            {
                case Axis.X:
                    sign = Vector3.Dot(B - A, C - B);
                    if (sign < 0)
                        return 1;
                    return -1;
                case Axis.Y:
                    sign = Vector3.Dot(A - B, C - (A + B) / 2);
                    if (sign < 0)
                        return 1;
                    return -1;
                case Axis.Z:
                    sign = Vector3.Dot(Vector3.Cross(A - B, B - C), Vector3.up);
                    if (sign < 0)
                        return 1;
                    return -1;
            }
            return 1;
        }

        void IManipulationHandler.OnManipulationCompleted(ManipulationEventData eventData)
        {
            GetComponentInChildren<MeshRenderer>().material.color = Color.cyan;
            ManipulationDone();
        }

        void IManipulationHandler.OnManipulationCanceled(ManipulationEventData eventData)
        {
            GetComponentInChildren<MeshRenderer>().material.color = Color.cyan;
            ManipulationDone();
        }

        private void OnDisable()
        {
            if (IsManipulating)
                ManipulationDone();
        }

        private void ManipulationDone()
        {
            IsManipulating = false;
            InputManager.Instance.PopModalInputHandler();
            RotateEventEnded();
        }

        public void Hide()
        {
            ElementRenderer.enabled = false;
        }

        public void Show()
        {
            ElementRenderer.enabled = true;
        }

#if UNITY_EDITOR
        bool flag = true;
        void IInputClickHandler.OnInputClicked(InputClickedEventData eventData)
        {
            if (flag)
                RotateEventStarted();
            else
                RotateEventEnded();
            flag = !flag;
        }
#endif
    }
    #endregion
}