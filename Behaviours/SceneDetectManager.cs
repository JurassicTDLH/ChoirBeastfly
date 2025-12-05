using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using TeamCherry.SharedUtils;
using System.Linq;
using HarmonyLib;
using ChoirBeastFly;
using BepInEx;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ChoirBeastFly.Patches;

namespace ChoirBeastFly.Behaviours
{
    internal class SceneDetectManager : MonoBehaviour
    {
        public static SceneDetectManager Instance { get; private set; }
        public static bool hardMode { get; set; }
        public static bool bossSummoned { get; set; }
        private GameObject heroObject;
        private AudioSource _needolinAudioSource,_needolinDeepAudioSource,_needolinAmplifiedAudioSource;
        private float _audioPlayingTimer = 0f;
        private float _deepAudioPlayingTimer = 0f;
        private const float REQUIRED_PLAYING_TIME = 3f; // 需要连续播放3秒
        private bool _isCheckingAudio = false;
        private bool _hasSwitchedThisSession = false; // 防止重复切换

        private void Awake()
        {// 设置单例实例
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            //设置boss生成状态
            hardMode = false;
            bossSummoned = false;
            // 监听场景切换
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }
        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            ChoirBeastFlyPlugin.IsInHangRoom = newScene.name == "Hang_04";
            if (ChoirBeastFlyPlugin.IsInHangRoom && MainPatches.hang04Complete == true)
            {// 重置音频检测状态
                _hasSwitchedThisSession = false;
                _isCheckingAudio = false;
                _audioPlayingTimer = 0f;
                if(bossSummoned)
                {
                    if(MainPatches.isAct3 && hardMode)
                    {
                        LoadBossDeep();
                    }
                    else
                    {
                        LoadBoss();
                    }
                }
                else
                {
                    // 启动音频检测设置
                    StartCoroutine(SetupAudioDetection());
                }
            }
        }
        private void Update()
        {
            // 检查是否在Hang_04房间且正在弹琴
            if (ChoirBeastFlyPlugin.IsInHangRoom && _isCheckingAudio && !_hasSwitchedThisSession)
            {
                CheckAudioPlayingStatus();
            }
        }
        private IEnumerator SetupAudioDetection()
        {
            yield return new WaitForSeconds(1f); // 等待场景加载完成

            try
            {
                // 获取HeroController实例
                if (HeroController.instance == null)
                {
                    yield break;
                }

                heroObject = HeroController.instance.gameObject;

                // 查找Sounds子组件
                Transform soundsTransform = heroObject.transform.Find("Sounds");
                if (soundsTransform == null)
                {
                    yield break;
                }

                // 查找Needolin Memory子组件
                Transform needolinDeepTransform = soundsTransform.Find("Needolin Memory");
                if (needolinDeepTransform == null)
                {
                    yield break;
                }
                // 查找Needolin 子组件
                Transform needolinTransform = soundsTransform.Find("Needolin");
                if (needolinTransform == null)
                {
                    yield break;
                }
                // 查找Needolin Amplified子组件
                Transform needolinAmplifiedTransform = soundsTransform.Find("Needolin Amplified");
                if (needolinAmplifiedTransform == null)
                {
                    yield break;
                }

                // 获取普通弹琴的AudioSource组件
                _needolinAudioSource = needolinTransform.GetComponent<AudioSource>();
                if (_needolinAudioSource == null)
                {
                    yield break;
                }

                // 获取深度弹琴AudioSource组件
                _needolinDeepAudioSource = needolinDeepTransform.GetComponent<AudioSource>();
                if (_needolinDeepAudioSource == null)
                {
                    yield break;
                }

                // 获取强化弹琴AudioSource组件
                _needolinAmplifiedAudioSource = needolinAmplifiedTransform.GetComponent<AudioSource>();
                if (_needolinAmplifiedAudioSource == null)
                {
                    yield break;
                }

                _isCheckingAudio = true;
                _audioPlayingTimer = 0f;
            }
            catch (Exception ex)
            {
                _isCheckingAudio = false;
            }
        }
        private void CheckAudioPlayingStatus()
        {
            if (_needolinAudioSource == null || _needolinDeepAudioSource == null || _needolinAmplifiedAudioSource == null)
            {
                StartCoroutine(SetupAudioDetection());
                return;
            }
            if (heroObject.transform.position.x > 40)
            {
                // 检查深度音频是否正在播放
                if (_needolinDeepAudioSource.isPlaying)
                {
                    _deepAudioPlayingTimer += Time.deltaTime;

                    // 如果连续播放时间达到要求
                    if (_deepAudioPlayingTimer >= REQUIRED_PLAYING_TIME)
                    {
                        _hasSwitchedThisSession = true;
                        _isCheckingAudio = false;
                        LoadBossDeep();
                    }
                }
                //强化音频
                else if (_needolinAmplifiedAudioSource.isPlaying)
                {
                    _audioPlayingTimer += Time.deltaTime;

                    // 如果连续播放时间达到要求
                    if (_audioPlayingTimer >= REQUIRED_PLAYING_TIME)
                    {
                        _hasSwitchedThisSession = true;
                        _isCheckingAudio = false;
                        LoadBoss();
                    }
                }
                //普通音频
                else if (_needolinAudioSource.isPlaying)
                {
                    _audioPlayingTimer += Time.deltaTime;

                    // 如果连续播放时间达到要求
                    if (_audioPlayingTimer >= REQUIRED_PLAYING_TIME)
                    {
                        _hasSwitchedThisSession = true;
                        _isCheckingAudio = false;
                        LoadBoss();
                    }
                }
                else
                {
                    // 音频停止播放，重置计时器
                    if (_audioPlayingTimer > 0f)
                    {
                        _audioPlayingTimer = 0f;
                    }
                    if (_deepAudioPlayingTimer > 0f)
                    {
                        _deepAudioPlayingTimer = 0f;
                    }
                }
            }
        }
        private void LoadBoss()
        {
            GameObject bossLoader = GameObject.Find("Boss Loader");
            if(bossLoader != null)
            {
                bossLoader.AddComponent<BossLoaderModifyer>();
            }
            bossSummoned = true;
        }
        private void LoadBossDeep()
        {
            GameObject bossLoader = GameObject.Find("Boss Loader");
            hardMode = true;
            if (bossLoader != null)
            {
                bossLoader.AddComponent<BossLoaderModifyer>();
            }
            bossSummoned = true;
        }
    }
}
