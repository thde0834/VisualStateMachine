using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BaseGraph : ScriptableObject
{
    public List<BaseNode> Nodes = new List<BaseNode>();

    public BaseGraph Clone()
    {
        BaseGraph machine = Instantiate(this);
        machine.Nodes.ForEach(node => node.Clone());
        return machine;
    }

    public BaseNode CreateGraphNode(BaseNode node, Vector2 position)
    {
        node.name = node.GetType().Name;
        node.SetPosition(position);

        Nodes.Add(node);

        return node;
    }

    public void DeleteGraphNode(BaseNode node)
    {
        Nodes.Remove(node);
    }
    
    public void CreateEdge(BaseNode parent, BaseNode child)
    {
        child.AddParent(parent);
        parent.AddChild(child);
    }

    public void RemoveEdge(BaseNode parent, BaseNode child)
    {
        child.RemoveParent(parent);
        parent.RemoveChild(child);
    }
}
