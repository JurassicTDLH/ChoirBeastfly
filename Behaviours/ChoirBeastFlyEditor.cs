using System.Linq;
using GenericVariableExtension;
using GlobalSettings;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace ChoirBeastFly.Behaviours;
internal class ChoirBeastFlyEditor : MonoBehaviour
{
    public static tk2dSpriteCollectionData originalCollection { get; set;}
    public static Texture[] originalTextures { get; private set;}
    private BlackThreadState blackThreadState;
    private void Awake()
    {
        blackThreadState = gameObject.GetComponent<BlackThreadState>();
        if (blackThreadState != null && SceneDetectManager.hardMode == false)
        {
            DisableVoid();
        }
        ModifyComponents();
        ChangeTextures();
        ModifyDeathFsm();
        ModifyControlFSM();
    }
    private void DisableVoid()
    {
        blackThreadState.enabled = false;
    }
    /// <summary>
    /// Modify non-FSM components on the boss.
    /// </summary>
    private void ModifyComponents()
    {
        ModifyPositionConstraints();
        ModifyHealth();
    }
    /// <summary>
    /// Adjust position bounds in the <see cref="ConstrainPosition"/> component.
    /// </summary>
    private void ModifyPositionConstraints()
    {
        var constrainPos = GetComponent<ConstrainPosition>();
        constrainPos.xMin = ArenaBounds.XMin;
        constrainPos.xMax = ArenaBounds.XMax;
        constrainPos.yMin = ArenaBounds.YMin;
    }
    /// <summary>
    /// Modify the health of the boss.
    /// </summary>
    private void ModifyHealth()
    {
        var health = GetComponent<HealthManager>();
        health.hp = 1100;
        health.hasSpecialDeath = false;
        health.immuneToWater = true;
        health.OnDeath += () => { PlayMakerFSM.BroadcastEvent("ZERO HP");SceneDetectManager.bossSummoned = false; };
    }
    private void ModifyDeathFsm()
    {
        PlayMakerFSM deadFSM = gameObject.LocateMyFSM("FSM");
        Fsm fsm = deadFSM.Fsm;
        fsm.GetFsmFloat("X Min").value = ArenaBounds.XMin;
        fsm.GetFsmFloat("X Max").value = ArenaBounds.XMax;
    }
    /// <summary>
    /// Modify the boss's Control <see cref="PlayMakerFSM">FSM</see>.
    /// </summary>
    private void ModifyControlFSM()
    {
        PlayMakerFSM controlFSM = gameObject.LocateMyFSM("Control");
        Fsm fsm = controlFSM.Fsm;
        // Adjust positions marked in FSM
        foreach (var fsmFloat in controlFSM.FsmVariables.FloatVariables)
        {
            switch (fsmFloat.Name)
            {
                case "Lava Y":
                    fsmFloat.Value = -10f;
                    break;
                case "Low Y Point":
                    fsmFloat.Value = ArenaBounds.YCenter - 2;
                    break;
                case "Rematch Arena X Max":
                    fsmFloat.Value = ArenaBounds.XMax;
                    break;
                case "Rematch Arena X Min":
                    fsmFloat.Value = ArenaBounds.XMin;
                    break;
                case "High Y Pos":
                    fsmFloat.Value = ArenaBounds.YCenter + 2;
                    break;
            }
        }
        // Modify the particle effects when the boss rises from the water
        FsmState initState = fsm.GetState("Init");
        var initActions = initState.Actions.ToList();
        initActions.RemoveAt(7);
        initState.Actions = initActions.ToArray();
        var ptDropAntic = AssetManager.Get<GameObject>("Pt Drop Antic");
        if (!ptDropAntic)
        {
            Debug.LogError("Failed to get Incisive Drop Antic!");
            return;
        }
        var dropAntic = Instantiate(ptDropAntic,gameObject.transform);
        dropAntic.SetActive(true);
        dropAntic.transform.SetParent(gameObject.transform);
        dropAntic.transform.localPosition = new Vector3(0,-5f,0);
        fsm.GetFsmGameObject("Pt LavaAntic").value = dropAntic;
        FsmState entryBurstState = fsm.GetState("Entry Burst");
        var entryBurstActions = entryBurstState.Actions;
        if (entryBurstActions[0] is SetRotation setRotation)
        {
            setRotation.zAngle = 90f;
        }
        if (entryBurstActions[12] is SetVelocity2d setVelocity2d)
        {
            setVelocity2d.vector = new Vector2(0f,-30f);
        }
        if (entryBurstActions[13] is CheckYPosition checkYPosition)
        {
            checkYPosition.compareTo = 15f;
            checkYPosition.lessThan = FsmEvent.Finished;
            checkYPosition.greaterThan = null;
        }
        // Load and assign the boss theme
        var choirBattleMusicCue = AssetManager.Get<MusicCue>("Grand Forum Battle");
        if (!choirBattleMusicCue)
        {
            Debug.LogError("Failed to get Incisive Battle music cue!");
            return;
        }

        var choirBattleClip = AssetManager.Get<AudioClip>("H170 v2-04 Choir Battle Track");
        choirBattleMusicCue.channelInfos[0].clip = choirBattleClip;

        FsmState rematchStartState = fsm.GetState("Rematch Start");
        var rematchStartActions = rematchStartState.Actions;
        if (rematchStartActions[0] is ApplyMusicCue rematchStartMusic)
        {
            rematchStartMusic.musicCue = choirBattleMusicCue;
        }

        FsmState rematchReadyState = fsm.GetState("Rematch Ready");
        var rematchReadyActions = rematchReadyState.Actions.ToList();
        if (rematchReadyActions[16] is SetIntValue setIntValue)
        {
            setIntValue.intValue = 8;//更改对象池中允许召唤时的至少剩余怪物数量，目前有10个怪物，那么想要保证场上的怪物是2-3这个数字就是8
        }
        rematchReadyActions.RemoveAt(12);
        rematchReadyActions.RemoveAt(14);
        rematchReadyActions.RemoveAt(15);//禁用数值自动修改
        rematchReadyState.Actions = rematchReadyActions.ToArray();

        FsmState introRecoverState = fsm.GetState("Intro Recover");
        var introRecoverActions = introRecoverState.Actions;
        if (introRecoverActions[4] is ApplyMusicCue introRecoverMusic)
        {
            introRecoverMusic.musicCue = choirBattleMusicCue;
        }

        // Adjust the Charge state's limits
        FsmState chargeState = fsm.GetState("Charge");
        var chargeActions = chargeState.Actions;
        if (chargeActions[20] is CheckXPosition checkXMinPos)
        {
            checkXMinPos.compareTo = ArenaBounds.XMin - 6;
        }

        if (chargeActions[21] is CheckXPosition checkXMaxPos)
        {
            checkXMaxPos.compareTo = ArenaBounds.XMax + 6;
        }

        // Remove the boss's ability to stop chasing the player
        FsmState choiceState = fsm.GetState("Choice");
        choiceState.Transitions = choiceState.Transitions
            .Where(t => t.EventName != "GO HOME" && t.EventName != "DOOR SLAM").ToArray();
        var choiceActions = choiceState.Actions.ToList();
        choiceActions.RemoveAt(2);
        choiceActions.RemoveAt(4);
        choiceState.Actions = choiceActions.ToArray();

        FsmState stompPosState = fsm.GetState("Stomp Pos");
        stompPosState.Transitions = stompPosState.Transitions.Where(t => t.EventName != "GO HOME").ToArray();
        var stompPosActions = stompPosState.Actions.ToList();
        stompPosActions.RemoveAt(2);
        stompPosState.Actions = stompPosActions.ToArray();
        //edit stomp behaviour
        FsmState stompAnticState = fsm.GetState("Stomp Antic");
        FsmState extraAnticState = fsm.GetState("Extra Antic");
        var stompAnticActions = stompAnticState.Actions.ToList();
        stompAnticActions.RemoveAt(0);
        stompAnticState.Actions = stompAnticActions.ToArray();
        var extraAnticActions = extraAnticState.Actions.ToList();
        extraAnticActions.RemoveAt(0);
        extraAnticActions.RemoveAt(1);
        extraAnticState.Actions = extraAnticActions.ToArray();
        //edit floor slam and wall slam behaviour
        FsmState floorSlamState = fsm.GetState("Floor Slam");
        var heavySentry = GameObject.Find("Battle Scene").transform.Find("Wave 8 - Heavy Sentry").transform.Find("Song Heavy Sentry").gameObject;
        PlayMakerFSM bombCastFSM = gameObject.AddComponent<PlayMakerFSM>();
        bombCastFSM.Fsm = heavySentry.LocateMyFSM("Bomb Cast").Fsm;
        FsmState firstBombState = bombCastFSM.Fsm.GetState("First Bomb");
        var firstBombStateActions = firstBombState.Actions.ToList();
        if (firstBombStateActions[3] is FloatAdd floatAdd)
        {
            floatAdd.add = 1f;
        }
        firstBombState.Actions = firstBombStateActions.ToArray();
        var floorSlamActions = floorSlamState.Actions.ToList();
        FsmEventTarget fsmEventTarget = new FsmEventTarget();
        fsmEventTarget.target = FsmEventTarget.EventTarget.GameObject;
        floorSlamActions.Insert(5, new SendEventByName { eventTarget = fsmEventTarget, sendEvent = "BOMB CAST", delay = 0,everyFrame = false });
        floorSlamState.Actions = floorSlamActions.ToArray();
        
        PlayMakerFSM heavySentryFSM = heavySentry.LocateMyFSM("Control");
        FsmState chargedState = heavySentryFSM.Fsm.GetState("Charged?");
        var chargedActions = chargedState.Actions.ToList();
        GameObject runeSlamObject = new GameObject();
        if (chargedActions[4] is SpawnObjectFromGlobalPool spawnObjectFromGlobalPool)
        {
            runeSlamObject = spawnObjectFromGlobalPool.gameObject.Value;
        }
        FsmState wallSlamState = fsm.GetState("Wall Slam");
        var wallSlamActions = wallSlamState.Actions.ToList();
        Debug.Log(fsm.GetFsmGameObject("Pt SlamWall").Value.name);
        wallSlamActions.Insert(4, new SpawnObjectFromGlobalPool
        {
            gameObject = runeSlamObject,
            spawnPoint = fsm.GetFsmGameObject("Pt SlamWall").Value,
            position = Vector3.up*0.7f,
            rotation = Vector3.zero,
            storeObject = null,
        });
        wallSlamState.Actions = wallSlamActions.ToArray();
    }
    /// <summary>
    /// Change the <see cref="Texture2D">texture</see> atlases of the boss.
    /// </summary>
    private void ChangeTextures()
    {
        var sprite = GetComponent<tk2dSprite>();
        var cln = sprite.Collection;
        originalCollection = cln;
        originalTextures = new Texture[cln.materials.Length];
        for (int materialIndex = 0; materialIndex < cln.materials.Length; materialIndex++)
        {
            originalTextures[materialIndex] = cln.materials[materialIndex].mainTexture;
            cln.materials[materialIndex].mainTexture = AssetManager.Get<Texture2D>($"atlas{materialIndex}");
        }
    }
}
