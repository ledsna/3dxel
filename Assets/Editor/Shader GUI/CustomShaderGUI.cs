using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    public class CustomShaderGUI : CustomLitShader
    {
        bool showOutlineThresholds = false;
        bool showOutlineHeader = true;
        bool showCelShadingHeader = true;
        // private MaterialEditor materialEditor;
        private MaterialProperty[] properties;
        
        private MaterialProperty _DepthThreshold;
        private MaterialProperty _NormalsThreshold;
        private MaterialProperty _ExternalScale;
        private MaterialProperty _InternalScale;
        
        private MaterialProperty _OutlineColour;
        private MaterialProperty _OutlineStrength;

        private MaterialProperty _DebugOn;
        private MaterialProperty _External;
        private MaterialProperty _Convex;
        private MaterialProperty _Concave;

        private MaterialProperty _ValueSaturationCelShader;
        private MaterialProperty _DiffuseSpecularCelShader;

        private MaterialProperty _ValueSteps;
        private MaterialProperty _SaturationSteps;
        
        private MaterialProperty _DiffuseSteps;
        private MaterialProperty _SpecularSteps;
        
        private MaterialProperty _ShadowSteps;
        private MaterialProperty _LightmapSteps;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // Store the materialEditor and properties
            this.materialEditor = materialEditor;
            this.properties = properties;

            _DepthThreshold = FindProperty("_DepthThreshold", properties);
            _NormalsThreshold = FindProperty("_NormalsThreshold", properties);
            _ExternalScale = FindProperty("_ExternalScale", properties);
            _InternalScale = FindProperty("_InternalScale", properties);
            
            _OutlineColour = FindProperty("_OutlineColour", properties);
            _OutlineStrength = FindProperty("_OutlineStrength", properties);

            _DebugOn = FindProperty("_DebugOn", properties);
            _External = FindProperty("_External", properties);
            _Convex = FindProperty("_Convex", properties);
            _Concave = FindProperty("_Concave", properties);

            _ValueSaturationCelShader = FindProperty("_ValueSaturationCelShader", properties);
            _ValueSteps = FindProperty("_ValueSteps", properties);
            _SaturationSteps = FindProperty("_SaturationSteps", properties);

            _DiffuseSpecularCelShader = FindProperty("_DiffuseSpecularCelShader", properties);
            _DiffuseSteps = FindProperty("_DiffuseSteps", properties);
            _SpecularSteps = FindProperty("_SpecularSteps", properties);
            _ShadowSteps = FindProperty("_ShadowSteps", properties);
            _LightmapSteps = FindProperty("_LightmapSteps", properties);
            
            DrawCustomProperties();
            DrawDefaultProperties();
        }

        private void DrawDefaultProperties()
        {
            base.OnGUI(materialEditor, properties);
        }

        private void DrawCustomProperties()
        {
            showOutlineThresholds = EditorGUILayout.BeginFoldoutHeaderGroup(showOutlineThresholds, "Outline Thresholds");
            if (showOutlineThresholds) {
                EditorGUILayout.Space();
                materialEditor.ShaderProperty(_DepthThreshold, "Depth Threshold");
                materialEditor.ShaderProperty(_NormalsThreshold, "Normals Threshold");
                materialEditor.ShaderProperty(_ExternalScale, "External Thickness");
                materialEditor.ShaderProperty(_InternalScale, "Internal Thickness");

                EditorGUILayout.Space();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();


            showOutlineHeader = EditorGUILayout.BeginFoldoutHeaderGroup(showOutlineHeader, "Outline Settings");
            if (showOutlineHeader) {
                // EditorGUILayout.LabelField("Outline Properties", EditorStyles.boldLabel);
                materialEditor.ShaderProperty(_OutlineColour, "Outline Colour");
                materialEditor.ShaderProperty(_OutlineStrength, "Intensity");
                materialEditor.ShaderProperty(_DebugOn, "Debug View");
                materialEditor.ShaderProperty(_External, "External");
                materialEditor.ShaderProperty(_Convex, "Internal Convex");
                materialEditor.ShaderProperty(_Concave, "Internal Concave");

                EditorGUILayout.Space();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            showCelShadingHeader = EditorGUILayout.BeginFoldoutHeaderGroup(showCelShadingHeader, "Cel Shading Settings");
            if (showCelShadingHeader) {
                // EditorGUILayout.LabelField("Colour Stepping Properties", EditorStyles.boldLabel);
                materialEditor.ShaderProperty(_ValueSaturationCelShader, "Value-Saturation Cel Shader");
                materialEditor.ShaderProperty(_ValueSteps, "Value Steps");
                materialEditor.ShaderProperty(_SaturationSteps, "Saturation Steps");
                
                materialEditor.ShaderProperty(_DiffuseSpecularCelShader, "Diffuse-Specular Cel Shader");
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
