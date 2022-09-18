using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

public class BaseNodeView : UnityEditor.Experimental.GraphView.Node
{
    public Action<BaseNodeView> OnNodeSelected;

    private GraphView graphView;

    public BaseNode Node;

    public List<Port> Inputs { get; private set; } = new List<Port>();
    public List<Port> Outputs { get; private set; } = new List<Port>();

    // called dynamically, this class's data is reset on redraw of graph
    public BaseNodeView(BaseNode node, GraphView graphView)
    {
        // style code
        this.Node = node; 
        this.Node.name = node.GetType().Name;
        this.title = node.name.Replace("(Clone)", "").Replace("Node", "");

        this.viewDataKey = node.GUID;

        style.left = node.Position.x;
        style.top = node.Position.y;

        Button addInputButton = new Button(CreateInputPort);
        addInputButton.text = "Add Input";
        mainContainer.Add(addInputButton);

        Button addOutputButton = new Button(CreateOutputPort);
        addOutputButton.text = "Add Output";
        mainContainer.Add(addOutputButton);

        // other
        this.graphView = graphView;

        this.InitializePorts();

        
    }

    private void InitializePorts()
    {
        int parentCount = this.Node.GetCountOfParents() == 0 ? 1 : this.Node.GetCountOfParents();
        int childCount = this.Node.GetCountOfChildren() == 0 ? 1 : this.Node.GetCountOfChildren();

        for (int i = 0; i < parentCount; i++)
        {
            CreateInputPort();
        }
        for (int i = 0; i < childCount; i++)
        {
            CreateOutputPort();
        }
    }

    #region Port Creation/Deletion

    private void CreateInputPort()
    {
        Port port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(BaseNode));
        
        port.portName = "";
        port.name = "";
        port.userData = GUID.Generate();

        Button removeButton = new Button(() => RemovePort(port));
        removeButton.text = "-";
        port.contentContainer.Add(removeButton);

        Inputs.Add(port);
        inputContainer.Add(port);
    }
    private void CreateOutputPort()
    {
        Port port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(BaseNode));

        port.portName = "";
        port.name = "";

        Button removeButton = new Button(() => RemovePort(port));
        removeButton.text = "-";
        port.contentContainer.Add(removeButton);

        

        Outputs.Add(port);
        outputContainer.Add(port);
    }

    private void RemovePort(Port port)
    {
        if (port.connected == false)
        {
            if (port.parent.childCount > 1)
            {
                port.parent.Remove(port);
            }
            return;
        }

        Edge edge = port.connections.First();

        BaseNodeView childView = edge.input.node as BaseNodeView;
        BaseNodeView parentView = edge.output.node as BaseNodeView;

        parentView.Node.RemoveChild(childView.Node);
        childView.Node.RemoveParent(parentView.Node);

        edge.input.Disconnect(edge);
        edge.output.Disconnect(edge);

        //if (edge.input.parent.childCount > 1) 
        //{
        //    edge.input.parent.Remove(edge.input);
        //}

        if (childView.Node.GetCountOfParents() > 0)
        {
            edge.input.parent.Remove(edge.input);
        }

        if (parentView.Node.GetCountOfChildren() > 0)
        {
            edge.output.parent.Remove(edge.output);
        }

        graphView.RemoveElement(edge);

        // not sure if necessary
        RefreshPorts();
        RefreshExpandedState();
    }

    #endregion

    #region Port Helper Functions

    public Port GetInputPortByEdge(Edge targetEdge)
    {
        foreach (Port input in Inputs)
        {
            if (input.connected == false) continue;

            if (input.connections.First() == targetEdge)
            {
                return input;
            }
        }

        Debug.LogError("Target Edge not found!");

        return null;
    }
    public Port GetOutputPortByEdge(Edge targetEdge)
    {
        foreach (Port output in Outputs)
        {
            if (output.connected == false) continue;

            if (output.connections.First() == targetEdge)
            {
                return output;
            }
        }

        Debug.LogError("Target Edge not found!");

        return null;
    }

    public void SortInputPorts()
    {
        if (this.Node.GetCountOfParents() <= 1) return;

        Node.SortParents();
    }

    public void SortOutputPorts()
    {
        if (this.Node.GetCountOfChildren() <= 1) return;

        Node.SortChildren();
    }

    #endregion

    #region Edge Management

    public Edge CreateOutputEdge(BaseNodeView childView)
    {
        int outputIndex = this.Node.GetIndexOfChild(childView.Node);
        int inputIndex = childView.Node.GetIndexOfParent(this.Node);

        return this.Outputs[outputIndex].ConnectTo(childView.Inputs[inputIndex]);
    }

    public void DisconnectInputEdge(Edge targetEdge) => this.GetInputPortByEdge(targetEdge)?.Disconnect(targetEdge);
    private void DisconnectOutputEdge(Edge targetEdge) => this.GetOutputPortByEdge(targetEdge)?.Disconnect(targetEdge);

    #endregion

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        Undo.RecordObject(Node, "State Machine Graph (Set Position)");
        Node.SetPosition(newPos.xMin, newPos.yMin);
        EditorUtility.SetDirty(Node);
    }

    public override void OnSelected()
    {
        base.OnSelected();
        OnNodeSelected?.Invoke(this);
    }

}
