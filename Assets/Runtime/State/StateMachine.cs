using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    [field: SerializeField] public BaseGraph StateMachineGraph { get; private set; }

    private void Start()
    {
        // Creates and retrieves a clone of a StateMachineGraph
        // so that no two StateMachines are working with the same StateMachineGraph
        StateMachineGraph = StateMachineGraph.Clone();
    }
}
