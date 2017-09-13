using System;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
using LinkDev.HologramManipulator;

public class BasicMenuButtonController : MonoBehaviour, IInputClickHandler
{
    public enum ButtonAction { Delete, Hide};

    public ButtonAction ButtonType;
    private GameObject HologramTarget;

    public void Init(GameObject target)
    {
        HologramTarget = target;    
    }
    public void OnInputClicked(InputClickedEventData eventData)
    {
        switch (ButtonType)
        {
            case ButtonAction.Delete:
                Destroy(HologramTarget);
                break;
            case ButtonAction.Hide:
                HologramTarget.GetComponent<HologramController>().SetHologramState(HologramState.Inactive);
                break;
        }
    }
}
