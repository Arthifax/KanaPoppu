using UnityEngine;
using UnityEditor;

namespace VolumetricLights
{

    [CustomEditor(typeof(VolumetricLightsRenderFeature))]
    public class RenderFeatureEditor : Editor
    {

        SerializedProperty renderPassEvent;
        SerializedProperty blendMode, brightness;
        SerializedProperty downscaling, blurPasses, blurDownscaling, blurSpread, ditherStrength;

        private void OnEnable()
        {
            renderPassEvent = serializedObject.FindProperty("renderPassEvent");
            blendMode = serializedObject.FindProperty("blendMode");
            brightness = serializedObject.FindProperty("brightness");
            downscaling = serializedObject.FindProperty("downscaling");
            blurPasses = serializedObject.FindProperty("blurPasses");
            blurDownscaling = serializedObject.FindProperty("blurDownscaling");
            blurSpread = serializedObject.FindProperty("blurSpread");
            ditherStrength = serializedObject.FindProperty("ditherStrength");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(renderPassEvent);
            EditorGUILayout.PropertyField(downscaling);
            EditorGUILayout.PropertyField(blurPasses);
            if (blurPasses.intValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(blurDownscaling);
                EditorGUILayout.PropertyField(blurSpread);
                EditorGUI.indentLevel--;
            }
            if (blurPasses.intValue == 0 && downscaling.floatValue <= 1f)
            {
                EditorGUILayout.HelpBox("No composition in effect (no downscaling and no blur applied).", MessageType.Info);
                GUI.enabled = false;
            }
            EditorGUILayout.PropertyField(blendMode);
            EditorGUILayout.PropertyField(brightness);
            EditorGUILayout.PropertyField(ditherStrength);
            GUI.enabled = true;
            serializedObject.ApplyModifiedProperties();

        }

    }
}