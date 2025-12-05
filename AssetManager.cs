using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ChoirBeastfly.Behaviours;
using ChoirBeastFly.Behaviours;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using static Mono.Security.X509.X509Stores;
using Object = UnityEngine.Object;

namespace ChoirBeastFly;
internal static class AssetManager
{
    private static readonly Dictionary<string, string[]> ScenePrefabs = new()
    {
        //依次为：簧片战士，圣咏护卫，小铃铛,下落特效
        ["Hang_04_boss"] = ["Song Reed","Song Pilgrim 03", "Pilgrim 03 Song","Pt Drop Antic"],
        ["Bone_East_08_boss_beastfly"] = ["Boss Scene Beastfly"],
    };

    private static readonly Dictionary<string, string[]> BundleAssets = new()
    {
        ["audiocuesdynamic_assets_areahangarealibrary"] = ["Grand Forum Battle"],
        ["sfxstatic_assets_areahangarealibrary"] = ["H170 v2-04 Choir Battle Track"],
        ["localpoolprefabs_assets_areahang"] = ["Song Automaton Tiny"],
    };

    private static List<AssetBundle> _manuallyLoadedBundles = new();

    private static readonly Dictionary<Type, Dictionary<string, Object>> Assets = new();
    /// <summary>
    /// Manually load asset bundles.
    /// </summary>
    internal static IEnumerator Init()
    {
        yield return LoadScenePrefabs();
    }
    /// <summary>
    /// Load all prefabs located in scenes.
    /// </summary>
    private static IEnumerator LoadScenePrefabs()
    {
        AudioManager.BlockAudioChange = true;
        foreach (var (sceneName, prefabNames) in ScenePrefabs)
        {
            string loadScenePath = $"Scenes/{sceneName}";

            var loadSceneHandle = Addressables.LoadSceneAsync(loadScenePath, LoadSceneMode.Additive);
            yield return loadSceneHandle;

            if (loadSceneHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var sceneInstance = loadSceneHandle.Result;
                var scene = sceneInstance.Scene;
                foreach (var rootObj in scene.GetRootGameObjects())
                {
                    foreach (string prefabName in prefabNames)
                    {
                        GameObject? prefab = rootObj.GetComponentsInChildren<Transform>(true)
                            .FirstOrDefault(obj => obj.name == prefabName)?.gameObject;
                        if (prefab)
                        {
                            prefab.SetActive(false);
                            var prefabCopy = Object.Instantiate(prefab);
                            prefabCopy.name = prefabName;
                            Object.DontDestroyOnLoad(prefabCopy);

                            CheckSpecialBehaviour(prefabCopy);

                            TryAdd(prefabCopy);
                        }
                    }
                }
            }
            else
            {
                Debug.LogError(loadSceneHandle.OperationException);
            }

            var unloadSceneHandle =
                Addressables.UnloadSceneAsync(loadSceneHandle);
            yield return unloadSceneHandle;
        }

        AudioManager.BlockAudioChange = false;
    }
    /// <summary>
    /// Load all required assets located within loaded<see cref="AssetBundle">asset bundles</see>.
    /// </summary>
    internal static IEnumerator LoadBundleAssets()
    {
        string platformFolder = Application.platform switch
        {
            RuntimePlatform.WindowsPlayer => "StandaloneWindows64",
            RuntimePlatform.OSXPlayer => "StandaloneOSX",
            RuntimePlatform.LinuxPlayer => "StandaloneLinux64",
            _ => ""
        };
        string bundlesPath = Path.Combine(Addressables.RuntimePath, platformFolder);
        foreach (var (bundleName, assetNames) in BundleAssets)
        {
            bool bundleAlreadyLoaded = false;
            foreach (var loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                foreach (string assetPath in loadedBundle.GetAllAssetNames())
                {
                    foreach (string assetName in assetNames)
                    {
                        if (assetPath.GetAssetRoot() == assetName)
                        {
                            bundleAlreadyLoaded = true;
                            var loadAssetRequest = loadedBundle.LoadAssetAsync(assetPath);
                            yield return loadAssetRequest;

                            var loadedAsset = loadAssetRequest.asset;
                            if (loadedAsset is GameObject loadedPrefab)
                            {
                                CheckSpecialBehaviour(loadedPrefab);
                                TryAdd(loadedPrefab);
                            }
                            else if (loadedAsset)
                            {
                                TryAdd(loadedAsset);
                            }

                            break;
                        }
                    }

                    if (bundleAlreadyLoaded)
                    {
                        break;
                    }
                }

                if (bundleAlreadyLoaded)
                {
                    break;
                }
            }

            if (bundleAlreadyLoaded)
            {
                Debug.Log($"Bundle {bundleName} already loaded!");
                continue;
            }

            string bundlePath = Path.Combine(bundlesPath, $"{bundleName}.bundle");
            var bundleLoadRequest = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return bundleLoadRequest;

            AssetBundle bundle = bundleLoadRequest.assetBundle;
            _manuallyLoadedBundles.Add(bundle);
            foreach (string assetPath in bundle.GetAllAssetNames())
            {
                foreach (string assetName in assetNames)
                {
                    if (assetPath.GetAssetRoot() == assetName)
                    {
                        var assetLoadRequest = bundle.LoadAssetAsync(assetPath);
                        yield return assetLoadRequest;

                        var loadedAsset = assetLoadRequest.asset;
                        if (loadedAsset is GameObject loadedPrefab)
                        {
                            CheckSpecialBehaviour(loadedPrefab);
                        }

                        TryAdd(loadedAsset);
                    }
                }
            }
        }
    }
    /// <summary>
    /// Load textures embedded in the assembly.
    /// </summary>
    internal static void LoadTextures()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using (StreamWriter sw = new StreamWriter("resourceName.txt"))
        {
            sw.WriteLine(assembly.GetManifestResourceNames());
        }
        foreach (string resourceName in assembly.GetManifestResourceNames())
        {
            using (StreamWriter sw = new StreamWriter("resourceName.txt"))
            {
                sw.WriteLine(resourceName);
            }
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;


            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            var atlasTex = new Texture2D(2, 2);
            string texName = resourceName.Split('.')[^2];
            atlasTex.name = texName;
            atlasTex.LoadImage(buffer);
            TryAdd(atlasTex);
        }
    }
    /// <summary>
    /// Whether an asset with a specified name is already loaded.
    /// </summary>
    /// <param name="assetName">The name of the asset to check.</param>
    /// <returns></returns>
    private static bool Has(string assetName)
    {
        foreach (var (_, subDict) in Assets)
        {
            foreach (var (name, existingAsset) in subDict)
            {
                if (assetName == name && existingAsset)
                {
                    return true;
                }
            }
        }

        return false;
    }
    /// <summary>
    /// Try adding a new asset.
    /// </summary>
    /// <param name="asset">The asset to add.</param>
    private static void TryAdd<T>(T asset) where T : Object
    {
        var assetName = asset.name;
        if (Has(assetName))
        {
            Debug.Log($"Asset \"{assetName}\" has already been loaded!");
            return;
        }

        var assetType = asset.GetType();
        if (Assets.ContainsKey(assetType))
        {
            var existingAssetSubDict = Assets[assetType];
            if (existingAssetSubDict != null)
            {
                if (existingAssetSubDict.ContainsKey(assetName))
                {
                    var existingAsset = existingAssetSubDict[assetName];
                    if (existingAsset != null)
                    {
                        Debug.Log($"There is already an asset \"{assetName}\" of type \"{assetType}\"!");
                    }
                    else
                    {
                        Debug.Log(
                            $"Key \"{assetName}\" for sub-dictionary of type \"{assetType}\" exists, but its value is null; Replacing with new asset...");
                        Assets[assetType][assetName] = asset;
                    }
                }
                else
                {
                    Debug.Log($"Adding asset \"{assetName}\" of type \"{assetType}\".");
                    Assets[assetType].Add(assetName, asset);
                }
            }
            else
            {
                Debug.LogError($"Failed to get sub-dictionary of type \"{assetType}\"!");
                Assets.Add(assetType, new Dictionary<string, Object>());
            }
        }
        else
        {
            Assets.Add(assetType, new Dictionary<string, Object> { [assetName] = asset });
            Debug.Log(
                $"Added new sub-dictionary of type \"{assetType}\" with initial asset \"{assetName}\".");
        }
    }
    /// <summary>
    /// Check whether a loaded prefab should be modified in some way.
    /// </summary>
    /// <param name="prefab">The prefab to check.</param>
    private static void CheckSpecialBehaviour(GameObject prefab)
    {
        switch (prefab.name)
        {
            case "Boss Scene Beastfly":
                {
                    Transform sceneTransform = prefab.transform;

                    RepositionScene(sceneTransform);
                    ModifyTriggers(sceneTransform);

                    Transform statesTransform = sceneTransform.Find("Beastfly States");
                    ForceEnableBeastfly(statesTransform);
                    ReplaceBeastflySummonedEnemies(sceneTransform.Find("Summon Enemies"));

                    GameObject beastflyObj = statesTransform.Find("Active/Bone Flyer Giant").gameObject;
                    beastflyObj.AddComponent<ChoirBeastFlyEditor>();
                    var deathEffects = beastflyObj.GetComponent<EnemyDeathEffectsRegular>();
                    var corpsePrefab = deathEffects.CorpsePrefab;
                    corpsePrefab.AddComponent<BeastflyCorpse>();
                    // Add special behaviour to the boss's corpse
                    foreach (var corpse in beastflyObj.GetComponentsInChildren<PlayMakerFSM>(true)
                                 .Where(t => t.gameObject.layer == (int)PhysLayers.CORPSE))
                    {
                        corpse.gameObject.AddComponent<BeastflyCorpse>();
                    }

                    break;
                }
            case "Song Reed":
                {
                    prefab.AddComponent<Reed>();
                    break;
                }
            case "Song Pilgrim 03":
                {
                    prefab.AddComponent<Guard>();
                    break;
                }
            case "Pilgrim 03 Song":
                {
                    prefab.AddComponent<BellThrower>();
                    break;
                }
            case "Song Automaton Tiny":
                {
                    prefab.AddComponent<TinyFly>();
                    break;
                }
        }
    }
    /// <summary>
    /// Reposition objects in the scene.
    /// </summary>
    /// <param name="sceneTransform">The boss scene's <see cref="Transform">transform</see>.</param>
    private static void RepositionScene(Transform sceneTransform)
    {
        sceneTransform.position = new Vector2(-50, -2.5f);
        sceneTransform.Find("Beastfly States/Active/Bone Flyer Giant").position += Vector3.up * 30;
    }
    /// <summary>
    /// Adjust the trigger areas of the boss scene.
    /// </summary>
    /// <param name="sceneTransform">The <see cref="Transform">transform</see> of the scene containing the triggers.</param>
    private static void ModifyTriggers(Transform sceneTransform)
    {
        var triggerPos = new Vector2(33.23f, 15.72f);
        var triggerSize = new Vector2(85.1167f, 30.276f);
        var triggerOffset = Vector2.zero;

        Transform camLocksParent = sceneTransform.Find("CamLock Boss");
        Object.Destroy(camLocksParent.Find("CamLock Boss (1)").gameObject);
        camLocksParent.position = Vector2.zero;
        Transform camLockBoss = camLocksParent.Find("CamLock Boss");
        camLockBoss.position = triggerPos;
        var box = camLockBoss.GetComponent<BoxCollider2D>();
        box.size = triggerSize;
        box.offset = triggerOffset;
        var camLock = camLockBoss.GetComponent<CameraLockArea>();
        camLock.cameraXMin = 27f;
        camLock.cameraXMax = 36.2f;
        camLock.cameraYMin = 9.6f;
        camLock.cameraYMax = 9.6f;
    }
    /// <summary>
    /// Forcibly enable the fight.
    /// </summary>
    /// <param name="statesTransform"></param>
    private static void ForceEnableBeastfly(Transform statesTransform)
    {
        Object.Destroy(statesTransform.GetComponent<TestGameObjectActivator>());
    }
    /// <summary>
    /// Replace the scene's enemies with the Choir.
    /// </summary>
    /// <param name="summonEnemiesParent"></param>
    private static void ReplaceBeastflySummonedEnemies(Transform summonEnemiesParent)
    {
        //簧片战士预制体
        var reedPrefab = Get<GameObject>("Song Reed");
        if (!reedPrefab)
        {
            Debug.LogError("Failed to get Song Reed!");
            return;
        }
        //护卫预制体
        var pinPrefab = Get<GameObject>("Song Pilgrim 03");
        if (!pinPrefab)
        {
            Debug.LogError("Failed to get Song Pilgrim 03!");
            return;
        }
        //小铃铛预制体
        var bellPrefab = Get<GameObject>("Pilgrim 03 Song");
        if (!bellPrefab)
        {
            Debug.LogError("Failed to get Pilgrim 03 Song!");
            return;
        }
        //删除第一个吐火怪
        Transform boneSpitter = summonEnemiesParent.Find("Bone Spitter");
        Object.Destroy(boneSpitter.gameObject);
        //删除第二个吐火怪
        Transform boneSpitter1 = summonEnemiesParent.Find("Bone Spitter (1)");
        Object.Destroy(boneSpitter1.gameObject);
        //加载第一个簧片战士
        var reed =
            Object.Instantiate(reedPrefab, summonEnemiesParent);
        reed.SetActive(true);
        reed.name = "Song Reed";
        //加载第二个簧片战士
        var reed1 =
            Object.Instantiate(reedPrefab, summonEnemiesParent);
        reed1.SetActive(true);
        reed1.name = "Song Reed (1)";
        //加载第一个护卫
        var pin =
            Object.Instantiate(pinPrefab, summonEnemiesParent);
        pin.SetActive(true);
        pin.name = "Song Pilgrim 03";
        //加载第二个护卫
        var pin1 =
            Object.Instantiate(pinPrefab, summonEnemiesParent);
        pin1.SetActive(true);
        pin1.name = "Song Pilgrim 03 (1)";
        //加载第一个小铃铛
        var bell =
            Object.Instantiate(bellPrefab, summonEnemiesParent);
        bell.SetActive(true);
        bell.name = "Pilgrim 03 Song";
        //加载第二个小铃铛
        var bell1 =
            Object.Instantiate(bellPrefab, summonEnemiesParent);
        bell1.SetActive(true);
        bell1.name = "Pilgrim 03 Song (1)";
    }
    /// <summary>
    /// Supplemental method for fetching the Beastfly boss scene.
    /// </summary>
    /// <param name="beastflyScene">The resulting Beastfly scene.</param>
    /// <returns>Whether the scene was successfully fetched.</returns>
    private static bool TryGetBeastflyScene(out GameObject? beastflyScene)
    {
        var bossScenePrefab = Get<GameObject>("Boss Scene Beastfly");
        if (!bossScenePrefab)
        {
            Debug.LogError("Failed to get Boss Scene Beastfly prefab!");
            beastflyScene = null;
            return false;
        }

        beastflyScene = bossScenePrefab;
        return true;
    }
    /// <summary>
    /// Unload all saved assets.
    /// </summary>
    internal static void UnloadAll()
    {
        foreach (var assetDict in Assets.Values)
        {
            foreach (var asset in assetDict.Values)
            {
                Object.DestroyImmediate(asset);
            }
        }

        Assets.Clear();
        GC.Collect();
    }

    /// <summary>
    /// Unload bundles that were manually loaded for this mod.
    /// </summary>
    internal static void UnloadManualBundles()
    {
        foreach (var bundle in _manuallyLoadedBundles)
        {
            string bundleName = bundle.name;
            var unloadBundleHandle = bundle.UnloadAsync(true);
            unloadBundleHandle.completed += _ => { Debug.Log($"Successfully unloaded bundle \"{bundleName}\""); };
        }

        _manuallyLoadedBundles.Clear();

        foreach (var (_, obj) in Assets[typeof(GameObject)])
        {
            if (obj is GameObject gameObject && gameObject.activeSelf)
            {
                Debug.Log($"Recycling all instances of prefab \"{gameObject.name}\"");
                gameObject.RecycleAll();
            }
        }
    }
    /// <summary>
    /// Fetch an asset.
    /// </summary>
    /// <param name="assetName">The name of the asset to fetch.</param>
    /// <typeparam name="T">The type of asset to fetch.</typeparam>
    /// <returns>The fetched object if it exists, otherwise returns null.</returns>
    internal static T? Get<T>(string assetName) where T : Object
    {
        Type assetType = typeof(T);
        if (Assets.ContainsKey(assetType))
        {
            var subDict = Assets[assetType];
            if (subDict != null)
            {
                if (subDict.ContainsKey(assetName))
                {
                    var assetObj = subDict[assetName];
                    if (assetObj != null)
                    {
                        return assetObj as T;
                    }

                    Debug.LogError($"Failed to get asset \"{assetName}\"; asset is null!");
                    return null;
                }

                Debug.LogError($"Sub-dictionary for type \"{assetType}\" does not contain key \"{assetName}\"!");
                return null;
            }

            Debug.LogError($"Failed to get asset \"{assetName}\"; sub-dictionary of key \"{assetType}\" is null!");
            return null;
        }

        Debug.LogError($"Could not find a sub-dictionary of type \"{assetType}\"!");
        return null;
    }
}
