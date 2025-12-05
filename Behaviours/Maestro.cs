using System.Collections.Generic;
using System.Linq;
using GenericVariableExtension;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace ChoirBeastFly.Behaviours;

internal class Maestro : MonoBehaviour
{
    private BlackThreadState blackThreadState;
    private void Awake()
    {
        blackThreadState = gameObject.GetComponent<BlackThreadState>();
        if (blackThreadState != null && SceneDetectManager.hardMode == false)
        {
            DisableVoid();
        }
        ModifyHealth();
        ModifyControlFSM();
    }
    private void DisableVoid()
    {
        blackThreadState.enabled = false;
    }
    private void ModifyHealth()
    {
        var health = GetComponent<HealthManager>();
        health.initHp = 55;
        health.hp = 55;
    }
    /// <summary>
    /// Modify the enemy's Control <see cref="PlayMakerFSM">FSM</see>.
    /// </summary>
    private void ModifyControlFSM()
    {
        GameObject tinyFly = AssetManager.Get<GameObject>("Song Automaton Tiny");
        var controlFSM = gameObject.LocateMyFSM("Control");
        Fsm fsm = controlFSM.Fsm;

        var fsmVars = controlFSM.FsmVariables;
        var vec2Vars = fsmVars.Vector2Variables.ToList();
        var gameObjVars = fsmVars.GameObjectVariables.ToList();
        var stringVars = fsmVars.StringVariables.ToList();

        // Adjust the summon positions based on enemy name 
        var summonPos = new Vector2(ArenaBounds.XCenter, ArenaBounds.YCenter);
        if (name.Contains("(1)"))
        {
            summonPos = new Vector2(20,10);
        }
        else
        {
            summonPos = new Vector2(40, 10);
        }
        var summonPosVec2Var = new FsmVector2("Summon Position")
        {
            Value = summonPos,
        };
        vec2Vars.Add(summonPosVec2Var);

        var initState = fsm.GetState("Init");
        var initActions = initState.Actions.ToList();
        var flyInReadyState = fsm.GetState("Fly In Ready");
        var flyInState = fsm.GetState("Fly In");

        var heroObjVar = fsm.GetFsmGameObject("Hero");
        gameObjVars.Add(heroObjVar);
        var summonEvent = FsmEvent.GetFsmEvent("SUMMON");
        var summonEnemiesParent = GameObject.Find("Summon Enemies");
        List<FsmState> states = fsm.States.ToList();
        var noneFloatVar = new FsmFloat("")
        {
            UseVariable = true,
        };
        var startY = fsm.GetFsmFloat("Start Y");
        var defendY = fsm.GetFsmFloat("Defend Y");
        FsmString clipVariable = stringVars[0];
        if (initActions[11] is CreateObject createObject1)
        {
            createObject1.gameObject = tinyFly;
        }
        if (initActions[20] is CreateObject createObject2)
        {
            createObject2.gameObject = tinyFly;
        }
        if (initActions[29] is CreateObject createObject3)
        {
            createObject3.gameObject = tinyFly;
        }
        initActions.RemoveAt(1);
        initState.Actions = initActions.ToArray();
        clipVariable.Value = "Ambush";
        // State that sets some parameters when the enemy is summoned
        var setSummonedState = new FsmState(fsm)
        {
            Name = "Set Summoned",
            Actions = [
                new SetParent {
                    gameObject = new FsmOwnerDefault(),
                    parent = null,
                    resetLocalPosition = false,
                    resetLocalRotation = false,
                },
                new WaitRandom {
                    timeMin = 0.4f,
                    timeMax = 0.8f,
                    finishEvent = FsmEvent.Finished,
                    realTime = false,
                },
            ],
            Transitions = [
                new FsmTransition {
                    FsmEvent = FsmEvent.Finished,
                    ToFsmState = fsm.GetState("Fly In"),
                    ToState = "Fly In",
                }
            ]
        };
        states.Add(setSummonedState);
        flyInReadyState.Transitions = flyInReadyState.Transitions.Append(new FsmTransition
        {
            FsmEvent = summonEvent,
            ToFsmState = setSummonedState,
            ToState = setSummonedState.name,
        }).ToArray();
        flyInState.Actions = [
                            new Tk2dPlayAnimation {
                    gameObject = new FsmOwnerDefault(),
                    animLibName = "",
                    clipName = "Shift Fly",
                },
                new SetPosition2d {
                    gameObject = new FsmOwnerDefault(),
                    vector = summonPosVec2Var,
                    x = noneFloatVar,
                    y = noneFloatVar,
                    space = Space.World,
                    everyFrame = false,
                    lateUpdate = false,
                },
                new GetPosition {
                    gameObject = new FsmOwnerDefault(),
                    vector = Vector3.zero,
                    x = noneFloatVar,
                    y = startY,
                    z = noneFloatVar,
                    space = 0,
                    everyFrame = false,
                },
                new FloatOperator {
                    float1 = startY,
                    float2 = 1f,
                    operation = 0,
                    storeResult = defendY,
                    everyFrame = false,
                },
                new Translate {
                    gameObject = new FsmOwnerDefault(),
                    vector = new FsmVector3("") {
                        UseVariable = true,
                    },
                    x = 0,
                    y = 10,
                    z = 20,
                    space = Space.World,
                    perSecond = false,
                    everyFrame = false,
                    lateUpdate = false,
                    fixedUpdate = false,
                },
                new GetHero {
                    storeResult = heroObjVar,
                },
                new FaceObjectV2 {
                    objectA = new FsmOwnerDefault(),
                    objectB = heroObjVar,
                    spriteFacesRight = false,
                    playNewAnimation = false,
                    newAnimationClip = "",
                    resetFrame = false,
                    everyFrame = false,
                    pauseBetweenTurns = 0.5f,
                },
                new SetMeshRenderer {
                    gameObject = new FsmOwnerDefault(),
                    active = true,
                },
                new AnimatePositionBy {
                    gameObject = new FsmOwnerDefault(),
                    shiftBy = new Vector3(0, -10, -20),
                    time = 0.75f,
                    speed = noneFloatVar,
                    delay = noneFloatVar,
                    easeType = EaseFsmAction.EaseType.easeOutSine,
                    reverse = false,
                    finishEvent = FsmEvent.Finished,
                    realTime = false,
                },
                new Tk2dSpriteTweenColor {
                    gameObject = new FsmOwnerDefault(),
                    color = Color.white,
                    duration = 0.75f,
                },
            ];
        var health = GetComponent<HealthManager>();
        // Required for the enemy object to not get destroyed on death
        health.deathReset = true;
        // Do not destroy the enemy object on death
        health.hasSpecialDeath = true;
        // Do not drop shell shards on "death"
        health.shellShardDrops = 0;
        health.largeGeoDrops = 0;
        health.smallGeoDrops = 0;
        health.mediumGeoDrops = 0;
        health.largeSmoothGeoDrops = 0;

        // Reset some parameters after the enemy dies
        var resetState = new FsmState(fsm)
        {
            Name = "Reset",
            Actions = [
                new SetHP {
                    target = new FsmOwnerDefault(),
                    hp = health.initHp,
                },
                new SetParent {
                    gameObject = new FsmOwnerDefault(),
                    parent = summonEnemiesParent,
                    resetLocalPosition = true,
                    resetLocalRotation = false,
                },
                new SetVelocity2d {
                    gameObject = new FsmOwnerDefault(),
                    vector = Vector2.zero,
                    x = 0,
                    y = 0,
                    everyFrame = false,
                },
                new AudioStopV2 {
                    gameObject = new FsmOwnerDefault(),
                    fadeTime = 0,
                    cancelOnEarlyExit = false,
                }
            ],
            Transitions = [
                new FsmTransition {
                    FsmEvent = FsmEvent.Finished,
                    ToFsmState = initState,
                    ToState = initState.Name,
                }
            ],
        };
        states.Add(resetState);

        var deathState = fsm.GetState("Death");
        deathState.Actions = deathState.actions.Append(new EnemyDeathEffectsRegular.SimulateDeath { target = new FsmOwnerDefault(), }).ToArray();
        deathState.Transitions = deathState.Transitions.Append(new FsmTransition { FsmEvent = FsmEvent.Finished, ToFsmState = resetState, ToState = resetState.Name, }).ToArray();

        fsmVars.GameObjectVariables = gameObjVars.ToArray();
        fsmVars.Vector2Variables = vec2Vars.ToArray();
        fsm.States = states.ToArray();
        fsm.Events = fsm.Events.Append(summonEvent).ToArray();
        // Force set the enemy's state to when it is waiting to be summoned
        controlFSM.SetState(initState.Name);
    }
}
