using System.Collections.Generic;
using System.Linq;
using GenericVariableExtension;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace ChoirBeastFly.Behaviours;

internal class Reed : MonoBehaviour
{
    private BlackThreadState blackThreadState;
    private void Awake()
    {
        var alertTrigger = transform.Find("Alert Range").GetComponent<CircleCollider2D>();
        alertTrigger.radius = 64;
        blackThreadState = gameObject.GetComponent<BlackThreadState>();
        if (blackThreadState != null && SceneDetectManager.hardMode == false)
        {
            DisableVoid();
        }
        ModifyControlFSM();
    }
    private void DisableVoid()
    {
        blackThreadState.enabled = false;
    }
    /// <summary>
    /// Modify the enemy's Control <see cref="PlayMakerFSM">FSM</see>.
    /// </summary>
    private void ModifyControlFSM()
    {
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
            summonPos = new Vector2(18, 12);
        }
        else
        {
            summonPos = new Vector2(42, 12);
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

        var heroObjVar = new FsmGameObject("Hero");
        gameObjVars.Add(heroObjVar);
        var summonEvent = FsmEvent.GetFsmEvent("SUMMON");
        var summonEnemiesParent = GameObject.Find("Summon Enemies");
        List<FsmState> states = fsm.States.ToList();
        var noneFloatVar = new FsmFloat("")
        {
            UseVariable = true,
        };
        FsmString clipVariable = stringVars[0];
        //if (initActions[1] is GetDefaultTk2dClipName getDefaultTk2DClipName)
        //{
        //    //initActions[1] = new SetStringValue { stringVariable = "Clip", stringValue = "Fly In" , everyFrame = false };
        //}
        initActions.RemoveAt(1);
        initState.Actions = initActions.ToArray();
        clipVariable.Value = "Fly In";
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
        // Do not drop shell shards and geo on "death"
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
                    ToFsmState = flyInReadyState,
                    ToState = flyInReadyState.Name,
                }
            ],
        };
        states.Add(resetState);

        // Simulate a "fake death" that doesn't destroy the enemy object
        //var deathState = new FsmState(fsm)
        //{
        //    Name = "Dead",
        //    Actions = [
        //        new EnemyDeathEffectsRegular.SimulateDeath {
        //            target = new FsmOwnerDefault(),
        //        },
        //    ],
        //    Transitions = [
        //        new FsmTransition {
        //            FsmEvent = FsmEvent.Finished,
        //            ToFsmState = resetState,
        //            ToState = resetState.Name,
        //        }
        //    ],
        //};
        //states.Add(deathState);
        var deathState = fsm.GetState("Death");
        deathState.Actions = deathState.actions.Append(new EnemyDeathEffectsRegular.SimulateDeath {target = new FsmOwnerDefault(),}).ToArray();
        deathState.Transitions = deathState.Transitions.Append(new FsmTransition {FsmEvent = FsmEvent.Finished,ToFsmState = resetState,ToState = resetState.Name,}).ToArray();
        // Global transition for when the enemy is "killed"
        //var zeroHPEvent = FsmEvent.GetFsmEvent("ZERO HP");
        //var zeroHPTransition = new FsmTransition
        //{
        //    FsmEvent = zeroHPEvent,
        //    ToFsmState = deathState,
        //    ToState = deathState.Name,
        //};

        fsmVars.GameObjectVariables = gameObjVars.ToArray();
        fsmVars.Vector2Variables = vec2Vars.ToArray();
        fsm.States = states.ToArray();
        fsm.Events = fsm.Events.Append(summonEvent).ToArray();
        //fsm.GlobalTransitions = fsm.GlobalTransitions.Append(zeroHPTransition).ToArray();
        // Force set the enemy's state to when it is waiting to be summoned
        //controlFSM.SetState(flyInReadyState.Name);
    }
}
