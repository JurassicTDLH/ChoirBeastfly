using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace ChoirBeastFly.Behaviours;

internal class Guard : MonoBehaviour
{
    private BlackThreadState blackThreadState;
    private void Awake()
    {
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
        var controlFSM = gameObject.LocateMyFSM("Attack");
        Fsm fsm = controlFSM.Fsm;

        var fsmVars = controlFSM.FsmVariables;
        var vec2Vars = fsmVars.Vector2Variables.ToList();
        var gameObjVars = fsmVars.GameObjectVariables.ToList();
        var stringVars = fsmVars.StringVariables.ToList();

        // Adjust the summon positions based on enemy name 
        var summonPos = new Vector2(ArenaBounds.XCenter, ArenaBounds.YCenter);
        if (name.Contains("(1)"))
        {
            summonPos = new Vector2(22, 8);
        }
        else
        {
            summonPos = new Vector2(38, 8);
        }
        var summonPosVec2Var = new FsmVector2("Summon Position")
        {
            Value = summonPos,
        };
        vec2Vars.Add(summonPosVec2Var);

        var initState = fsm.GetState("Init");
        var initActions = initState.Actions.ToList();
        var sleepState = fsm.GetState("Sleep");
        var enterInState = fsm.GetState("Enter Idle");

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
        initActions.RemoveAt(6);
        initState.Actions = initActions.ToArray();
        clipVariable.Value = "Sleep 1";
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
                    ToFsmState = fsm.GetState("Enter Antic 2"),
                    ToState = "Enter Antic 2",
                }
            ]
        };
        states.Add(setSummonedState);
        sleepState.Transitions = sleepState.Transitions.Append(new FsmTransition
        {
            FsmEvent = summonEvent,
            ToFsmState = setSummonedState,
            ToState = setSummonedState.name,
        }).ToArray();
        var enterState = fsm.GetState("Enter Leap 2");
        var enterActions = enterState.Actions.ToList();
        if (enterActions[9] is SetPosition setPosition)
        {
            setPosition.x = summonPos.x;
            setPosition.y = summonPos.y;
        }
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
                    ToFsmState = sleepState,
                    ToState = sleepState.Name,
                }
            ],
        };
        states.Add(resetState);

        // Simulate a "fake death" that doesn't destroy the enemy object
        var deathState = new FsmState(fsm)
        {
            Name = "Death",
            Actions = [
                new EnemyDeathEffectsRegular.SimulateDeath {
                    target = new FsmOwnerDefault(),
                },
            ],
            Transitions = [
                new FsmTransition {
                    FsmEvent = FsmEvent.Finished,
                    ToFsmState = resetState,
                    ToState = resetState.Name,
                }
            ],
        };
        states.Add(deathState);
        // Global transition for when the enemy is "killed"
        var zeroHPEvent = FsmEvent.GetFsmEvent("ZERO HP");
        var zeroHPTransition = new FsmTransition
        {
            FsmEvent = zeroHPEvent,
            ToFsmState = deathState,
            ToState = deathState.Name,
        };

        fsmVars.GameObjectVariables = gameObjVars.ToArray();
        fsmVars.Vector2Variables = vec2Vars.ToArray();
        fsm.States = states.ToArray();
        fsm.Events = fsm.Events.Append(summonEvent).ToArray();
        fsm.GlobalTransitions = fsm.GlobalTransitions.Append(zeroHPTransition).ToArray();
        // Force set the enemy's state to when it is waiting to be summoned
        //controlFSM.SetState(sleepState.Name);
    }
}
