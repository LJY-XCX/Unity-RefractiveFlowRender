using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiEmitterMaterial assets. 
	 */
	
	[CustomEditor(typeof(ObiEmitterBlueprintBase),true), CanEditMultipleObjects] 
    public class ObiEmitterBlueprintEditor : ObiActorBlueprintEditor
	{
		
        ObiEmitterBlueprintBase material;	
		
		public override void OnEnable(){
            base.OnEnable();
            material = (ObiEmitterBlueprintBase)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfRequiredOrScript();		

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");

            if (GUILayout.Button("Generate", GUI.skin.FindStyle("LargeButton"), GUILayout.Height(32)))
                Generate();

			EditorGUILayout.HelpBox("Particle mass (kg):\n"+
									"2D:"+material.GetParticleMass(Oni.SolverParameters.Mode.Mode2D)+"\n"+
									"3D;"+material.GetParticleMass(Oni.SolverParameters.Mode.Mode3D)+"\n\n"+
									"Particle size:\n"+
									"2D:"+material.GetParticleSize(Oni.SolverParameters.Mode.Mode2D)+"\n"+
									"3D;"+material.GetParticleSize(Oni.SolverParameters.Mode.Mode3D),MessageType.Info);	

            EditorGUILayout.EndVertical();

			// Apply changes to the serializedProperty
			if (GUI.changed)
				serializedObject.ApplyModifiedProperties();
			
		}
		
	}
}


