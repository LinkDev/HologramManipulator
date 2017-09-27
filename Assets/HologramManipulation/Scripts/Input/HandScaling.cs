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
        public int ID;
        
        /// <summary>
        /// The ID of the opposite edge stored to facilitate scaling from this controller
        /// </summary>
        public int AdjacentPointID;

        public event Action<int> ScaleEventStarted;
        public event Action ScaleEventEnded;
        public Renderer ElementRenderer;
        public bool IsManipulating = false; public Color RenderingColor
        {
            set
            {
                m_Renderer.material.color = value;
            }
        }

        private Transform HostTransform;
        private float m_MinScaleFactor;
        private float m_MaxScaleFactor;
        private Renderer m_Renderer;
        private ManipulatorSettings m_Settings;

        public void Init(Transform targetTransform, ManipulatorSettings manipulationSettings, float minScaleFactor, float maxScaleFactor)
        {
            HostTransform = targetTransform;
            manipulationSettings = m_Settings;
            m_MinScaleFactor = minScaleFactor;
            m_MaxScaleFactor = maxScaleFactor;
            m_Renderer = GetComponent<Renderer>();
        }

        /// <summary>
        /// Used to define the point of scale, for example when using the bottom left controller
        /// we are scaling around the opposite edge of the cube
        /// </summary>
        /// <param name="_ID"></param>
        /// <param name="adjacentID"></param>
        public void SetIDAndAdjacentID(int _ID, int adjacentID)
        {
            ID = _ID;
            AdjacentPointID = adjacentID;
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

            RenderingColor = m_Settings.ActiveControllerColor;
            if (ScaleEventStarted != null)
                ScaleEventStarted(ID);

            oldCumulativeDelta = eventData.CumulativeDelta.z;
            cumulativeChange = 0;
        }

        void IManipulationHandler.OnManipulationUpdated(ManipulationEventData eventData)
        {
            cumulativeChange = (eventData.CumulativeDelta.z - oldCumulativeDelta) * m_Settings.ScaleFactor;
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
            RenderingColor = m_Settings.BoxDefaultColor;
            if (ScaleEventEnded != null)
                ScaleEventEnded();
        }
        
#if UNITY_EDITOR
        bool flag = true;
        void IInputClickHandler.OnInputClicked(InputClickedEventData eventData)
        {
            if (flag)
                ScaleEventStarted(ID);
            else
                ScaleEventEnded();
            flag = !flag;
        }

#endif


    }
}
