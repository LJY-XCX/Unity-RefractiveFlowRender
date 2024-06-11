using UnityEngine;
using UnityEditor;
using System;

namespace Obi
{

    [CustomPropertyDrawer(typeof(FluidRenderingUtils.FluidRendererSettings))]
    public class ObiFluidRendererSettingsDrawer : PropertyDrawer
    {
        public void DoPropertyGroupStatic(Rect position, GUIContent content, System.Action action)
        {
            EditorGUI.LabelField(position, content, EditorStyles.boldLabel);
            {
                if (action != null)
                {
                    EditorGUI.indentLevel++;
                    action();
                    EditorGUI.indentLevel--;
                }
            }
        }

        public void DoToggleablePropertyGroupStatic(Rect position, SerializedProperty enabledProperty, GUIContent content, System.Action action)
        {
            enabledProperty.boolValue = EditorGUI.Foldout(position, enabledProperty.boolValue, content, true, ObiEditorUtils.GetBoldToggleStyle());
            if (enabledProperty.boolValue)
            {
                if (action != null)
                {
                    EditorGUI.indentLevel++;
                    action();
                    EditorGUI.indentLevel--;
                }
            }
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            var blendSrc = property.FindPropertyRelative("blendSource");
            var blendDst = property.FindPropertyRelative("blendDestination");

            var particleBlendSrc = property.FindPropertyRelative("particleBlendSource");
            var particleBlendDst = property.FindPropertyRelative("particleBlendDestination");
            var particleZWrite = property.FindPropertyRelative("particleZWrite");

            var thicknessCutoff = property.FindPropertyRelative("thicknessCutoff");
            var thicknessDownsample = property.FindPropertyRelative("thicknessDownsample");

            var generateSurface = property.FindPropertyRelative("generateSurface");
            var blurRadius = property.FindPropertyRelative("blurRadius");
            var surfaceDownsample = property.FindPropertyRelative("surfaceDownsample");

            var lighting = property.FindPropertyRelative("lighting");
            var smoothness = property.FindPropertyRelative("smoothness");
            var ambientMult = property.FindPropertyRelative("ambientMultiplier");

            var generateReflection = property.FindPropertyRelative("generateReflection");
            var reflection = property.FindPropertyRelative("reflection");
            var metalness = property.FindPropertyRelative("metalness");

            var generateRefraction = property.FindPropertyRelative("generateRefraction");
            var transparency = property.FindPropertyRelative("transparency");
            var absorption = property.FindPropertyRelative("absorption");
            var refraction = property.FindPropertyRelative("refraction");
            var refractionDownsample = property.FindPropertyRelative("refractionDownsample");

            var generateFoam = property.FindPropertyRelative("generateFoam");
            var foamDownsample = property.FindPropertyRelative("foamDownsample");

            Rect pos = position;
            pos.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing ;

            DoPropertyGroupStatic(pos, new GUIContent("Basic Blending"),
            () => {
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, blendSrc, new GUIContent("Blend Source"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, blendDst, new GUIContent("Blend Destination"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, particleBlendSrc, new GUIContent("Particle Blend Source"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, particleBlendDst, new GUIContent("Particle Blend Destination"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, particleZWrite, new GUIContent("Particle Z Write"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, thicknessCutoff, new GUIContent("Thickness Cutoff"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, thicknessDownsample, new GUIContent("Thickness Downsample"));
            });

            pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            DoToggleablePropertyGroupStatic(pos, generateSurface, new GUIContent("Surface Reconstruction"),
            () => {
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos,blurRadius, new GUIContent("Blur Radius"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos,surfaceDownsample, new GUIContent("Surface Downsample"));
            });

            if (!generateSurface.boolValue)
            {
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.HelpBox(pos, "Lighting/reflection/refraction require surface reconstruction.", MessageType.Info);
            }

            GUI.enabled = generateSurface.boolValue;

            pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            DoToggleablePropertyGroupStatic(pos,lighting, new GUIContent("Lighting"),
            () => {
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, smoothness, new GUIContent("Smoothness"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, ambientMult, new GUIContent("Ambient Multiplier"));
            });

            pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            DoToggleablePropertyGroupStatic(pos,generateReflection, new GUIContent("Reflection"),
            () => {
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, reflection, new GUIContent("Reflection"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, metalness, new GUIContent("Metalness"));
            });

            pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            DoToggleablePropertyGroupStatic(pos,generateRefraction, new GUIContent("Refraction"),
            () => {
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, transparency, new GUIContent("Transparency"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, absorption, new GUIContent("Absorption"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, refraction, new GUIContent("Refraction"));
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos, refractionDownsample, new GUIContent("Refraction Downsample"));
            });

            GUI.enabled = true;

            pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            DoToggleablePropertyGroupStatic(pos,generateFoam, new GUIContent("Foam"),
            () => {
                pos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(pos,foamDownsample, new GUIContent("Foam Downsample"));
            });

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight * 13 + EditorGUIUtility.standardVerticalSpacing * 12;

            if (property.FindPropertyRelative("generateSurface").boolValue)
               height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
            else
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            if (property.FindPropertyRelative("lighting").boolValue)
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
            if (property.FindPropertyRelative("generateReflection").boolValue)
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
            if (property.FindPropertyRelative("generateRefraction").boolValue)
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4;
            if (property.FindPropertyRelative("generateFoam").boolValue)
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 1;

            return height;
        }
    }

}

