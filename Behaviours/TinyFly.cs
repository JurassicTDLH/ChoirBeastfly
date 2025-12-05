using System.Collections.Generic;
using System.Linq;
using GenericVariableExtension;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using static TeamCherry.DebugMenu.DebugMenu;

namespace ChoirBeastFly.Behaviours
{
    internal class TinyFly : MonoBehaviour
    {
        private BlackThreadState blackThreadState;
            private void Awake()
            {
                blackThreadState = gameObject.GetComponent<BlackThreadState>();
                if (blackThreadState != null && SceneDetectManager.hardMode == false)
                {
                    DisableVoid();
                }

            }
            private void DisableVoid()
            {
                blackThreadState.enabled = false;
            }
    }
}
