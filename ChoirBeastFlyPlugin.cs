using BepInEx;
using ChoirBeastFly.Behaviours;
using ChoirBeastFly.Patches;
using HarmonyLib;
using UnityEngine;

namespace ChoirBeastFly;

// TODO - adjust the plugin guid as needed
[BepInAutoPlugin(id: "io.github.jurassictdlh.choirbeastfly")]
public partial class ChoirBeastFlyPlugin : BaseUnityPlugin
{
    private static Harmony _harmony = null!;
    public static bool IsInHangRoom { get; set; } = false;
    private static GameObject _sceneDetectManager;
    private void Awake()
    {
        AssetManager.LoadTextures();
        _harmony = new Harmony("io.github.silksong_choirbeastfly");
        MainPatches.Init();
        _harmony.PatchAll(typeof(MainPatches));
        CreateDetectManager();
    }
    private void CreateDetectManager()
    {
        // 查找是否已存在管理器
        _sceneDetectManager = GameObject.Find("ChoirBeastFlySceneDetectManager");
        if (_sceneDetectManager == null)
        {
            _sceneDetectManager = new GameObject("ChoirBeastFlySceneDetectManager");
            UnityEngine.Object.DontDestroyOnLoad(_sceneDetectManager);

            // 添加监听器组件
            _sceneDetectManager.AddComponent<SceneDetectManager>();
        }
    }
    private void OnDestroy()
    {
        _harmony.UnpatchSelf();
        AssetManager.UnloadAll();
    }
}
