using HarmonyLib;
using ChoirBeastFly.Behaviours;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace ChoirBeastFly.Patches;
public class MainPatches
{
    public static bool hang04Complete { get; private set; }
    public static bool isAct3 { get; private set; }
    /// <summary>
    /// Private MainPatches Harmony instance.
    /// </summary>
    private static Harmony _harmony = null!;

    /// <summary>
    /// Initialize the main patches class.
    /// </summary>
    internal static void Init()
    {
        _harmony = new Harmony(nameof(MainPatches));
    }
    /// <summary>
    /// Initialize the <see cref="AssetManager">asset manager</see> when the <see cref="GameManager">game manager</see> is ready.
    /// </summary>
    /// <param name="__instance">The game manager instance.</param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Awake))]
    private static void InitAssetManager(GameManager __instance)
    {
        __instance.StartCoroutine(AssetManager.Init());
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.SetLoadedGameData), typeof(SaveGameData), typeof(int))]
    private static void CheckPlayerData(GameManager __instance)
    {
        __instance.GetSaveStatsForSlot(PlayerData.instance.profileID, (saveStats, _) => {
            isAct3 = saveStats.IsAct3;
            hang04Complete = PlayerData.instance.hang04Battle;
        });
    }

    /// <summary>
    /// Check whether to run <see cref="BeastflyPatches">Beastfly patches</see>.
    /// </summary>
    /// <param name="__instance">The <see cref="BattleScene">battle scene</see> instance.</param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BattleScene), nameof(BattleScene.Awake))]
    private static void CheckPatchBeastfly(BattleScene __instance)
    {
        //后面这边要加一下检查高庭连战已经打过
        if (__instance.setPDBoolOnEnd != "hang04Battle" | hang04Complete == false)
        {
            return;
        }

        _harmony.PatchAll(typeof(BeastFlyPatches));
        __instance.gameObject.AddComponent<BeastflyLoader>();
    }

    /// <summary>
    /// Check whether to unpatch <see cref="BeastFlyPatches">Beastfly patches</see>.
    /// </summary>
    /// <param name="__instance">The  <see cref="BattleScene">battle scene</see> instance.</param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BattleScene), nameof(BattleScene.OnDisable))]
    private static void CheckUnpatchBeastfly(BattleScene __instance)
    {
        _harmony.UnpatchSelf();
    }
    /// <summary>
    /// Unpatch <see cref="BeastFlyPatches">boss-related patches</see> when returning to the main menu.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ReturnToMainMenu))]
    private static IEnumerator UnpatchChoirBeastfly(IEnumerator result)
    {
        while (result.MoveNext())
        {
            yield return result.Current;
        }
        SceneDetectManager.hardMode = false;
        SceneDetectManager.bossSummoned = false;
        for (int materialIndex = 0; materialIndex < ChoirBeastFlyEditor.originalCollection.materials.Length; materialIndex++)
        {

            ChoirBeastFlyEditor.originalCollection.materials[materialIndex].mainTexture = ChoirBeastFlyEditor.originalTextures[materialIndex];
        }
        _harmony.UnpatchSelf();
    }
}
