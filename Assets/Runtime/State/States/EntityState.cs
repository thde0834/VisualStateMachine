using System.Collections.Generic;
using UnityEngine;

//public abstract class EntityState : ScriptableObject
//{
//    private EntityStateMachine parentStateMachine;
    
//    [SerializeField] 
//    private List<BaseGameEventListener> transitionListeners;

//    [SerializeField]
//    private List<GameEventListenerDictionary> entityActions;

//    public void Initialize(EntityStateMachine stateMachine) => parentStateMachine = stateMachine; 

//    public void ActivateState() => parentStateMachine.SetCurrentState(this);

//    public void OnEnter() => entityActions.ForEach((action) => action.RegisterAllListeners());

//    public void OnExit() => entityActions.ForEach((action) => action.UnregisterAllListeners());

//}

public abstract class EntityState : State
{
    public override void OnEnter()
    {
        //isActive = true;
        Debug.Log($"{this} on enter");
    }
    public override void OnExit()
    {
        //isActive = false;
        Debug.Log($"{this} on exit");
    }
}