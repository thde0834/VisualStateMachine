using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class BaseGraphView : UnityEditor.Experimental.GraphView.GraphView
{
    public new class UxmlFactory : UxmlFactory<BaseGraphView, UnityEditor.Experimental.GraphView.GraphView.UxmlTraits> { }

    public BaseGraph graph;
    public Action<BaseNodeView> OnNodeSelected;

    private bool initialized = false;
    private List<BaseNodeView> graphNodeViews = new List<BaseNodeView>();

    private bool hasGraph => graph != null;

    public BaseGraphView() : base()
    {
        Insert(0, new GridBackground());

        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/StateMachine/StateMachineEditor.uss");
        styleSheets.Add(styleSheet);

        Undo.undoRedoPerformed += OnUndoRedoPerformed;
    }
    
    private void OnUndoRedoPerformed()
    {
        InitializeView(graph);
        AssetDatabase.SaveAssets();
    }

    /*
     * Deletes ALL existing views and recreates all existing views.
     * Should only be called once.
     */
    public void InitializeView(BaseGraph graph)
    {
        //if (initialized) return;

        this.graph = graph;
        if (hasGraph == false) return;

        /*
         *  Deletes ALL elements.
         *  Creates ONLY what elements OnGraphViewChanged() returns.
         */
        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements.ToList());
        graphViewChanged += OnGraphViewChanged;

        /*
         * Creates a blank GraphNodeView for every GraphNode with NO edges.
         */
        graph.Nodes.ForEach(node =>
        {
            CreateGraphNodeView(node);
        });

        /*
         *  Edges VISUALLY are created here.
         */
        graph.Nodes.ForEach(node => {

            BaseNodeView nodeView = GetGraphNodeView(node);

            // Sorts nodeView.Node's Parents and Childrens based on their relative horizontal position
            nodeView.SortInputPorts();
            nodeView.SortOutputPorts();

            foreach (var child in node.Children)
            {
                BaseNodeView childView = GetNodeByGuid(child.GUID) as BaseNodeView;
                Edge edge = nodeView.CreateOutputEdge(childView);
                AddElement(edge);
            }

        });

        //initialized = true;
    }

    public void UpdateView(BaseGraph graph)
    {
        this.graph = graph;
        if (hasGraph == false) return;


        this.graphNodeViews.ForEach(nodeView =>
        {
            nodeView.SortInputPorts();
            nodeView.SortOutputPorts();


        });

        /*
         *  Edges VISUALLY are created here.
         */
        graph.Nodes.ForEach(node => {

            BaseNodeView nodeView = GetGraphNodeView(node);

            // Sorts nodeView.Node's Parents and Childrens based on their relative horizontal position
            nodeView.SortInputPorts();
            nodeView.SortOutputPorts();

            foreach (var child in node.Children)
            {
                BaseNodeView childView = GetNodeByGuid(child.GUID) as BaseNodeView;
                Edge edge = nodeView.CreateOutputEdge(childView);
                AddElement(edge);
            }

        });
    }

    public BaseNodeView GetGraphNodeView(BaseNode node) => GetNodeByGuid(node.GUID) as BaseNodeView;

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(endPort =>
        endPort.direction != startPort.direction &&
        endPort.node != startPort.node).ToList();
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        if (graphViewChange.elementsToRemove != null)
        {
            foreach(GraphElement element in graphViewChange.elementsToRemove)
            {
                switch (element)
                {
                    case BaseNodeView nodeView:
                        {
                            DeleteGraphNode(nodeView.Node);
                            break;
                        }
                    case Edge edge:
                        {
                            BaseNodeView parentView = edge.output.node as BaseNodeView;
                            BaseNodeView childView = edge.input.node as BaseNodeView;
                            RemoveEdge(parentView.Node, childView.Node);
                            break;
                        }
                }
            }
        }

        if (graphViewChange.edgesToCreate != null && hasGraph)
        {
            Debug.Log("here");
            for (int i = graphViewChange.edgesToCreate.Count - 1; i >= 0; i--)
            {
                Edge edge = graphViewChange.edgesToCreate[i];

                BaseNodeView parentView = edge.output.node as BaseNodeView;
                BaseNodeView childView = edge.input.node as BaseNodeView;

                // Do NOT create an Edge between GraphNodes where an Edge already exists.
                if (parentView.Node.HasChild(childView.Node))
                {
                    Debug.LogError($"[{this.GetType().Name}]: You are trying to create an Edge that already exists!");
                    graphViewChange.edgesToCreate.Remove(edge);
                    continue;
                }

                CreateEdge(parentView.Node, childView.Node);

                parentView.SortOutputPorts();
                childView.SortInputPorts();
            }
        }

        //if (graphViewChange.movedElements != null)
        //{
        //    foreach (GraphNodeView parentNodeView
        //             in from movedElement in graphViewChange.movedElements
        //                let movedNode = movedElement as GraphNodeView
        //                where movedNode is { Input: { connections: { } } }
        //                from edge in movedNode.Input.connections
        //                where edge.output is { node: GraphNodeView }
        //                select edge.output?.node as GraphNodeView)
        //    {
        //        parentNodeView?.SortChildren();
        //    }
        //}

        return graphViewChange;
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        // get every state and populate menu
        var types = TypeCache.GetTypesDerivedFrom<State>();

        // gets mouse position in view
        var mousePosition = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);
        
        foreach (var type in types)
        {
            if (type.IsAbstract) continue;
            string path = (type.BaseType != typeof(State)) ? $"{type.BaseType.Name}/{type.Name}" : $"{type.Name}";
            evt.menu.AppendAction(path, a =>
            {
                CreateGraphNode(type, new Vector2(mousePosition.x, mousePosition.y));
            });
        }
    }

    #region GraphNode Management

    /*
     *  GraphNodes are NOT VISUALLY created here.
     *  These functions are ONLY manipulating the data of GraphNodes.
     *  -------------------------------------------------
     *  PopulateView() handles BOTH visual creation and deletion of Nodes and Edges.
     *  -------------------------------------------------
     *  UNDO and REDO functionality implemented HERE
     */

    private void CreateGraphNode(System.Type type, Vector2 position)
    {
        if (hasGraph == false) return;

        BaseNode graphNode = ScriptableObject.CreateInstance(type) as BaseNode;
        graphNode.GUID = GUID.Generate().ToString();

        Undo.RecordObject(graph, $"Create Graph Node ({this.GetType().Name})");

        graphNode = graph.CreateGraphNode(graphNode, position);
        CreateGraphNodeView(graphNode);

        if (Application.isPlaying) return;

        AssetDatabase.AddObjectToAsset(graphNode, graph);
        AssetDatabase.SaveAssets();

        Undo.RegisterCreatedObjectUndo(graphNode, $"Create Graph Node ({this.GetType().Name})");
        Undo.RegisterFullObjectHierarchyUndo(graph, $"Create Graph Node ({this.GetType().Name})");

        EditorUtility.SetDirty(graph);
        EditorUtility.SetDirty(graphNode);

    }

    private void DeleteGraphNode(BaseNode graphNode)
    {
        if (hasGraph == false) return;
        
        Undo.RecordObject(graph, $"Delete Graph Node ({this.GetType().Name})");

        graph.DeleteGraphNode(graphNode);

        Undo.DestroyObjectImmediate(graphNode);
        AssetDatabase.SaveAssets();

        EditorUtility.SetDirty(graph);
    }

    #endregion

    public void CreateGraphNodeView(BaseNode graphNode)
    {
        if (graphNode == null) return;

        BaseNodeView nodeView = new BaseNodeView(graphNode, this);
        nodeView.OnNodeSelected = OnNodeSelected;

        AddElement(nodeView);
    }

    #region Edge Management

    /*
     *  Edges are NOT VISUALLY created here.
     *  These functions are ONLY manipulating the data of GraphNodes.
     *  -------------------------------------------------
     *  PopulateView() handles BOTH visual creation and deletion of Nodes and Edges.
     *  -------------------------------------------------
     *  UNDO and REDO functionality implemented HERE
     */

    private void CreateEdge(BaseNode parentNode, BaseNode childNode)
    {
        UnityEngine.Object[] objectsToUndo = { parentNode, childNode };

        Undo.RecordObjects(objectsToUndo, $"Create Edge ({this.GetType().Name})");

        graph.CreateEdge(parentNode, childNode);

        EditorUtility.SetDirty(parentNode);
        EditorUtility.SetDirty(childNode);
    }

    private void RemoveEdge(BaseNode parentNode, BaseNode childNode)
    {
        UnityEngine.Object[] objectsToUndo = { parentNode, childNode };
        Undo.RecordObjects(objectsToUndo, $"Remove Edge ({this.GetType().Name})");

        graph.RemoveEdge(parentNode, childNode);

        EditorUtility.SetDirty(parentNode);
        EditorUtility.SetDirty(childNode);
    }

    #endregion
}
