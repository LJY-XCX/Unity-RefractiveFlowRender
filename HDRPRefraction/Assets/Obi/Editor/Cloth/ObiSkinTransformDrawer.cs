using UnityEngine;
using UnityEditor;
using System;

namespace Obi
{

    [CustomPropertyDrawer(typeof(ObiTriangleSkinMap.SkinTransform))]
    public class ObiSkinTransformDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            
            var pos = property.FindPropertyRelative("position");
            var rot = property.FindPropertyRelative("rotation");
            var sc =  property.FindPropertyRelative("scale");

            EditorGUI.PropertyField(position,pos);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            var euler = EditorGUI.Vector3Field(position,"Rotation",rot.quaternionValue.eulerAngles);
            if (EditorGUI.EndChangeCheck())
            {
                Quaternion quaternion = Quaternion.Euler(euler);
                rot.quaternionValue = quaternion;
            }
            EditorGUI.EndProperty();
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(position,sc);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
        }
    }

}

