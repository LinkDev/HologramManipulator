using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
namespace LinkDev.HologramManipulator.InputModule
{
    
    public class HandScaling : MonoBehaviour,
                            IManipulationHandler
#if UNITY_EDITOR
                                , IInputClickHandler
#endif
    {
        private Transform HostTransform;
        
        /// <summary>
        /// The speed of change of the object scale determined from <see cref="ManipulationManager.ScaleFactor"/> 
        /// </summary>
        private float ScaleFactor;
        
        private float m_MinScaleFactor;
        private float m_MaxScaleFactor;

        public int id;
        
        /// <summary>
        /// The ID of the opposite edge stored to facilitate scaling from this controller
        /// </summary>
        public int AdjacentPointID;

        public event Action<int> ScaleEventStarted;
        public event Action ScaleEventEnded;
        public Renderer ElementRenderer;
        public bool IsManipulating = false;

        public void Init(Action<int> ScaleEventStartedHandler, Action ScaleEventEndedHandler, Transform targetTransform, float scalingFactor, int ID, int targetID, float minScaleFactor, float maxScaleFactor)
        {
            ScaleEventStarted += ScaleEventStartedHandler;
            ScaleEventEnded += ScaleEventEndedHandler;
            HostTransform = targetTransform;
            ScaleFactor = scalingFactor;
            id = ID;
            AdjacentPointID = targetID;
            m_MinScaleFactor = minScaleFactor;
            m_MaxScaleFactor = maxScaleFactor;
        }

        public void Hide()
        {
            ElementRenderer.enabled = false;
        }
        public void Show()
        {
            ElementRenderer.enabled = true;
        }

        private float oldCumulativeDelta, cumulativeChange;
        void IManipulationHandler.OnManipulationStarted(ManipulationEventData eventData)
        {
            IsManipulating = true;
            InputManager.Instance.PushModalInputHandler(gameObject);

            if (ScaleEventStarted != null)
                ScaleEventStarted(id);

            oldCumulativeDelta = eventData.CumulativeDelta.z;
            cumulativeChange = 0;
        }

        void IManipulationHandler.OnManipulationUpdated(ManipulationEventData eventData)
        {
            cumulativeChange = (eventData.CumulativeDelta.z - oldCumulativeDelta) * ScaleFactor;
            oldCumulativeDelta = eventData.CumulativeDelta.z;
            var nextScale = HostTransform.localScale.x * (1.0f + cumulativeChange);
            if (nextScale >= m_MinScaleFactor && nextScale <= m_MaxScaleFactor)
                HostTransform.localScale = nextScale * Vector3.one;
        }

        void IManipulationHandler.OnManipulationCompleted(ManipulationEventData eventData)
        {
            ManipulationDone();
        }

        void IManipulationHandler.OnManipulationCanceled(ManipulationEventData eventData)
        {
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
            if (ScaleEventEnded != null)
            ScaleEventEnded();
        }

#if UNITY_EDITOR
        bool flag = true;
        void IInputClickHandler.OnInputClicked(InputClickedEventData eventData)
        {
            if (flag)
                ScaleEventStarted(id);
            else
                ScaleEventEnded();
            flag = !flag;
        }

#endif


    }
}
