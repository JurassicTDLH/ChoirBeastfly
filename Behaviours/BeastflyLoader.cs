using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChoirBeastFly.Behaviours;

internal class BeastflyLoader : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return AssetManager.LoadBundleAssets();

        var sceneObj = AssetManager.Get<GameObject>("Boss Scene Beastfly");
        if (!sceneObj)
        {
            Debug.LogError("Failed to get Boss Scene Beastfly prefab!");
            yield break;
        }

        var sceneInst = Instantiate(sceneObj);
        var sceneTransform = sceneInst.transform;

        // Activate the object containing the Beastfly boss
        sceneTransform.Find("Beastfly States/Active").gameObject.SetActive(true);

        SetBattleScene(sceneTransform);

        AddSummonEnemies(sceneTransform);

        DeletePlatforms(sceneTransform.Find("Lava Plats"));

        sceneInst.SetActive(true);
    }

    private void OnDestroy()
    {
        AssetManager.UnloadManualBundles();
    }
    /// <summary>
    /// Set the boss's <see cref="BattleScene">battle scene</see>.
    /// </summary>
    /// <param name="sceneTransform">The <see cref="Transform">transform</see> of the boss scene.</param>
    private void SetBattleScene(Transform sceneTransform)
    {
        Debug.Log("Start set Battle Scene");
        var battleWave = sceneTransform.Find("Beastfly States/Active").gameObject.AddComponent<BattleWave>();
        var battleScene = GameObject.Find("Battle Scene").GetComponent<BattleScene>();
        battleScene.battleStartClip = null;
        battleScene.completed = false;
        battleScene.battleEndPause = 8;
        battleScene.musicCueStart = null;
        battleScene.setPDBoolOnEnd = "no";
        battleScene.openGatesOnEnd = true;
        battleScene.toggleWavesAwake = true;
        battleScene.waves = new List<BattleWave> { battleWave };
        battleWave.Init(battleScene);
        battleScene.GetComponent<BoxCollider2D>().enabled = true;
        Debug.Log("Set Battle Scene Successed!");
    }
    /// <summary>
    /// Replace the lava platforms in the boss scene with moss platforms.
    /// </summary>
    /// <param name="lavaPlats">The lava platforms parent.</param>
    private void DeletePlatforms(Transform lavaPlats)
    {
        Debug.Log("Start Cleaning Platforms");
        for (int platIndex = 1; platIndex <= 6; platIndex++)
        {
            Transform? plat = lavaPlats.transform.Find($"Song Golem Floor {platIndex}");
            if (plat)
            {
                //Debug.Log(plat.name);
                Destroy(plat.gameObject);
            }
        }
    }
    private void AddSummonEnemies(Transform sceneTransform)
    {
        //增加指挥
        Transform summonEnemyParent = sceneTransform.Find("Summon Enemies");
        Transform battleSceneParent = GameObject.Find("Battle Scene").transform;
        Transform waveParent = battleSceneParent.Find("Wave 7 - Maestro x 2");
        GameObject maestroPrefab = waveParent.Find("Song Pilgrim Maestro").gameObject;
        maestroPrefab.AddComponent<Maestro>();
        var maestro =
            Object.Instantiate(maestroPrefab, summonEnemyParent);
        maestro.SetActive(true);
        maestro.name = "Song Pilgrim Maestro";
        var maestro1 =
            Object.Instantiate(maestroPrefab, summonEnemyParent);
        maestro1.SetActive(true);
        maestro1.name = "Song Pilgrim Maestro (1)";
        //增加大臣
        waveParent = battleSceneParent.Find("Wave 5 - Song Admins");
        GameObject adminPrefab = waveParent.Find("Song Administrator").gameObject;
        adminPrefab.AddComponent<Administrator>();
        var admin =
            Object.Instantiate(adminPrefab, summonEnemyParent);
        admin.SetActive(true);
        admin.name = "Song Administrator";
        var admin1 =
            Object.Instantiate(adminPrefab, summonEnemyParent);
        admin1.SetActive(true);
        admin1.name = "Song Administrator (1)";
    }
}
