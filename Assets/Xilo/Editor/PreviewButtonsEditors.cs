using UnityEditor;
using UnityEngine;
using Xilo.Objects;
using Xilo.Steps;
using Xilo.Tools;

namespace Xilo.EditorTools
{
    // Big Inspector buttons for the motion previews (they also work outside Play mode).
    static class PreviewButtons
    {
        public static bool Draw(string label)
        {
            return GUILayout.Button(label, GUILayout.Height(28f));
        }
    }

    [CustomEditor(typeof(PaperSheet))]
    sealed class PaperSheetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            var sheet = (PaperSheet)target;
            if (PreviewButtons.Draw("▶ Preview reveal (and back)"))
                sheet.PreviewReveal();
            if (PreviewButtons.Draw("■ Stop preview and restore"))
                sheet.Tween.StopPreview();
        }
    }

    [CustomEditor(typeof(PoseTween))]
    sealed class PoseTweenEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            var tween = (PoseTween)target;
            if (PreviewButtons.Draw("▶ Preview → target"))
                tween.PreviewToTarget();
            if (PreviewButtons.Draw("▶ Preview → target and back"))
                tween.PreviewToTargetAndBack();
            if (PreviewButtons.Draw("■ Stop preview and restore"))
                tween.StopPreview();
        }
    }

    [CustomEditor(typeof(PaperPlacementHandler))]
    sealed class PaperPlacementHandlerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (PreviewButtons.Draw("▶ Preview baren hop (make room)"))
                ((PaperPlacementHandler)target).PreviewMakeRoom();
        }
    }

    [CustomEditor(typeof(ToolTipProbe))]
    sealed class ToolTipProbeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Tip contact distance: how far from the surface (meters) the tip still paints. " +
                "Select the tool to see it as the yellow gizmo line (green while touching in Play).", MessageType.Info);
            DrawDefaultInspector();
        }
    }
}
