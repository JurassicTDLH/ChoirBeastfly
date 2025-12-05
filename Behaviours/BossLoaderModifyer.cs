using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChoirBeastFly.Behaviours;
internal class BossLoaderModifyer : MonoBehaviour
{
    bool complete = false;
    private void Awake()
    {
        var bossLoader = GameObject.Find("Boss Loader").GetComponent<SceneAdditiveLoadConditional>();
        if (bossLoader != null)
        {
            if(complete == false)
            {
                if (bossLoader.sceneLoaded)
                {
                    bossLoader.Unload();
                }
                bossLoader.loadAlt = false;
                bossLoader.StartCoroutine(bossLoader.LoadRoutine(true, bossLoader));
                complete = true;
            }

        }
        else
        {
            Debug.LogError("Boss Loader No Found");
        }
    }
}
