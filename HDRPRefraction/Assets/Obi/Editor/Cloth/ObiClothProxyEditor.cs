using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{

	[CustomEditor(typeof(ObiClothProxy)), CanEditMultipleObjects] 
	public class ObiClothProxyEditor : Editor
	{
	
		ObiClothProxy proxy;
        SerializedProperty masterCloth;
		
		public void OnEnable(){
			proxy = (ObiClothProxy)target;
            masterCloth = serializedObject.FindProperty("m_Master");
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(masterCloth, new GUIContent("Source Cloth"));
			
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed)
				serializedObject.ApplyModifiedProperties();
			
		}
		
	}

}

