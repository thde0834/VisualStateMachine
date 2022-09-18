using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;

public class BaseGraphWindow : EditorWindow
{
    private BaseGraphView graphView;
    private InspectorView inspectorView;

    private const string BASE_ASSET_PATH = "Assets/Editor/StateMachine/";
    private const string MENU_PATH = "Graphs/State Machine";

    [MenuItem(MENU_PATH)]
    public static void OpenWindow()
    {
        GetWindow<BaseGraphWindow>("State Machine Editor");
    }

    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        if (Selection.activeObject is not BaseGraph) return false;
        OpenWindow();
        return true;
    }

    private void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BASE_ASSET_PATH + "StateMachineEditor.uxml");
        visualTree.CloneTree(root);

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(BASE_ASSET_PATH + "StateMachineEditor.uss");
        root.styleSheets.Add(styleSheet);

        graphView = root.Q<BaseGraphView>();
        graphView.OnNodeSelected = OnNodeSelectionChanged;

        inspectorView = root.Q<InspectorView>();

        OnSelectionChange();
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnSelectionChange()
    {
        BaseGraph graph = Selection.activeObject as BaseGraph;

        if (graph == null)
        {
            if (Selection.activeGameObject != null)
            {
                StateMachine stateMachine = Selection.activeGameObject.GetComponent<StateMachine>();
                if (stateMachine != null)
                {
                    graph = stateMachine.StateMachineGraph;
                }
            }
        }
        else
        {
            if (Application.isPlaying || AssetDatabase.CanOpenAssetInEditor(graph.GetInstanceID()))
            {
                SerializedObject so = new SerializedObject(graph);
                rootVisualElement.Bind(so);
                if (graphView != null)
                {
                    graphView.InitializeView(graph);
                }

                return;
            }
        }

        rootVisualElement.Unbind();

    }

    private void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        switch (change)
        {
            // Occurs during the next update of the Editor application if it is in edit mode and was previously in play mode.
            case PlayModeStateChange.EnteredEditMode:
                OnSelectionChange();
                break;
            // Occurs when exiting edit mode, before the Editor is in play mode.
            case PlayModeStateChange.ExitingEditMode:
                break;
            // Occurs during the next update of the Editor application if it is in play mode and was previously in edit mode.
            case PlayModeStateChange.EnteredPlayMode:
                OnSelectionChange();
                break;
            // Occurs when exiting play mode, before the Editor is in edit mode.
            case PlayModeStateChange.ExitingPlayMode:
                break;
        }
    }

    private void OnNodeSelectionChanged(BaseNodeView stateView)
    {
        inspectorView.UpdateSelection(stateView);
    }

}