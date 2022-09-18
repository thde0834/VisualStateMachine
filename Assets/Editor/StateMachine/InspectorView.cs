using System;
using UnityEngine.UIElements;
using UnityEditor;

public class InspectorView : VisualElement
{
    public new class UxmlFactory : UxmlFactory<InspectorView, VisualElement.UxmlTraits> { }
    Editor editor;

    public InspectorView()
    {

    }

    internal void UpdateSelection(BaseNodeView graphView)
    {
        Clear();

        UnityEngine.Object.DestroyImmediate(editor);

        editor = Editor.CreateEditor(graphView.Node);
        IMGUIContainer container = new IMGUIContainer(() => { 
            if (editor.target)
            {
                editor.OnInspectorGUI();
            }
        });
        Add(container);
    }
}
