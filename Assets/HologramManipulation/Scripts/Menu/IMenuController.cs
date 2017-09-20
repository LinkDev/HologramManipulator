using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LinkDev.HologramManipulator
{    
    /// <summary>
     /// Interface  to provide the basis of any script that attempts to provide
     /// a menu controller for the hologram, done this way to mainly refrain from enabling
     /// or disabling the menu object using SetActive which can complicate opening or closing
     /// using animation for the menu
     /// </summary>

    public interface IMenuController
    {
        void Init(GameObject target);

        /// <summary>
        /// Set the state of the menu as enabled/disabled can be changed to enum
        /// if you use more than two states
        /// </summary>
        /// <param name="state"></param>
        void SetState(bool state);

    }
}
