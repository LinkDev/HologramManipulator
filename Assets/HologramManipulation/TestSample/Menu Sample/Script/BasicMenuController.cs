using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LinkDev.HologramManipulator
{
    public class BasicMenuController : MonoBehaviour, IMenuController
    {
        private GameObject TargetHologram;

        void IMenuController.Init(GameObject target)
        {
            TargetHologram = target;
            foreach (var button in GetComponentsInChildren<BasicMenuButtonController>())
            {
                button.Init(TargetHologram);
            }
        }

        void IMenuController.SetState(bool state)
        {
            gameObject.SetActive(state);

        }
    }
}