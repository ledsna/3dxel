using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    public class MyCustomShaderGUI : CustomLitShader
    {

        bool showOutlineHeader = true;
        bool showCelShadingHeader = true;
        private MaterialEditor materialEditor;
        private MaterialProperty[] properties;
        private MaterialProperty _DebugOn;
        private MaterialProperty _External;
        private MaterialProperty _Convex;
        private MaterialProperty _Concave;
        private MaterialProperty _OutlineStrength;
        private MaterialProperty _DiffuseSteps;
        private MaterialProperty _SpecularSteps;
        private MaterialProperty _ShadowSteps;
        private MaterialProperty _LightmapSteps;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // Store the materialEditor and properties
            this.materialEditor = materialEditor;
            this.properties = properties;

            // Find the custom property by its name (ensure the name matches what's in your shader)
            _DebugOn = FindProperty("_DebugOn", properties);
            _External = FindProperty("_External", properties);
            _Convex = FindProperty("_Convex", properties);
            _Concave = FindProperty("_Concave", properties);
            _OutlineStrength = FindProperty("_OutlineStrength", properties);
            _DiffuseSteps = FindProperty("_DiffuseSteps", properties);
            _SpecularSteps = FindProperty("_SpecularSteps", properties);
            _ShadowSteps = FindProperty("_ShadowSteps", properties);
            _LightmapSteps = FindProperty("_LightmapSteps", properties);

            // Draw custom properties
            DrawCustomProperties();
            
            // Draw default properties
            DrawDefaultProperties();
        }

        private void DrawDefaultProperties()
        {
            base.OnGUI(materialEditor, properties); // Draw all default properties
        }

        private void DrawCustomProperties()
        {
            showOutlineHeader = EditorGUILayout.BeginFoldoutHeaderGroup(showOutlineHeader, "Outline Settings");

            if (showOutlineHeader)
            {
                EditorGUILayout.Space();
                // EditorGUILayout.LabelField("Outline Properties", EditorStyles.boldLabel);
                materialEditor.ShaderProperty(_OutlineStrength, "Intensity");
                materialEditor.ShaderProperty(_DebugOn, "Debug View");
                materialEditor.ShaderProperty(_External, "External");
                materialEditor.ShaderProperty(_Convex, "Internal Convex");
                materialEditor.ShaderProperty(_Concave, "Internal Concave");

                // Colour Stepping Properties Header
                EditorGUILayout.Space();


            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            showCelShadingHeader = EditorGUILayout.BeginFoldoutHeaderGroup(showCelShadingHeader, "Cel Shading Settings");
            if (showCelShadingHeader) {
                // EditorGUILayout.LabelField("Colour Stepping Properties", EditorStyles.boldLabel);

                materialEditor.ShaderProperty(_DiffuseSteps, "Diffuse Lighting Steps");
                materialEditor.ShaderProperty(_SpecularSteps, "Specular Lighting Steps");
                materialEditor.ShaderProperty(_ShadowSteps, "Received Shadows Steps");
                materialEditor.ShaderProperty(_LightmapSteps, "Baked GI Steps");
                
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();

        }
    }
}
