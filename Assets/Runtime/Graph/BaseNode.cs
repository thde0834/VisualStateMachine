using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class BaseNode : ScriptableObject
{
    [HideInInspector] public string GUID;
    [HideInInspector] public Vector2 Position;

    [field: SerializeField] public List<BaseNode> Parents { get; private set; } = new List<BaseNode>();
    [field: SerializeField] public List<BaseNode> Children { get; private set; } = new List<BaseNode>();

    public virtual BaseNode Clone() => Instantiate(this);

    public void SetPosition(Vector2 position) => this.Position = position;
    public void SetPosition(float x, float y) => this.Position.Set(x, y);

    public bool HasParent(BaseNode parent) => this.Parents.Contains(parent);
    public bool HasChild(BaseNode child) => this.Children.Contains(child);

    public int GetIndexOfParent(BaseNode parent) => this.Parents.IndexOf(parent);
    public int GetIndexOfChild(BaseNode child) => this.Children.IndexOf(child);

    public int GetCountOfParents() => this.Parents.Count;
    public int GetCountOfChildren() => this.Children.Count;

    public void AddParent(BaseNode parent)
    {
        // Add parent to this: parent----x[this]
        if (this.Parents.Contains(parent) == false)
        {
            this.Parents.Add(parent);
        }
    }
    public void RemoveParent(BaseNode parent)
    {
        // Remove parent from this: parent----x[this]
        this.Parents.Remove(parent);
    }

    public void AddChild(BaseNode child)
    {
        // Add parent to this: [this]x----child
        if (this.Children.Contains(child) == false)
        {
            this.Children.Add(child);
        }
    }
    public void RemoveChild(BaseNode child)
    {
        // Remove child from this: [this]x----child
        this.Children.Remove(child);
    }

    public void SortParents() => this.Parents.Sort(SortByVerticalPosition);
    public void SortChildren() => this.Children.Sort(SortByVerticalPosition);
    private int SortByVerticalPosition(BaseNode node1, BaseNode node2)
    {
        return node1.Position.y < node2.Position.y ? -1 : 1;
    }
}