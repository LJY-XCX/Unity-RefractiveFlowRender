using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
	
	/**
	 * Custom inspector for ObiEmitter components.
	 * Allows particle emission and constraint edition. 
	 * 
	 * Selection:
	 * 
	 * - To select a particle, left-click on it. 
	 * - You can select multiple particles by holding shift while clicking.
	 * - To deselect all particles, click anywhere on the object except a particle.
	 * 
	 * Constraints:
	 * 
	 * - To edit particle constraints, select the particles you wish to edit.
	 * - Constraints affecting any of the selected particles will appear in the inspector.
	 * - To add a new pin constraint to the selected particle(s), click on "Add Pin Constraint".
	 * 
	 */
	[CustomEditor(typeof(ObiEmitter)), CanEditMultipleObjects] 
	public class ObiEmitterEditor : Editor
	{

        SerializedProperty emitterBlueprint;

        SerializedProperty collisionMaterial;

        SerializedProperty fluidPhase;
        SerializedProperty emissionMethod;
        SerializedProperty minPoolSize;
        SerializedProperty speed;
        SerializedProperty lifespan;
        SerializedProperty randomVelocity;
        SerializedProperty useShapeColor;

		[MenuItem("GameObject/3D Object/Obi/Obi Emitter",false,200)]
        static void CreateObiCloth(MenuCommand menuCommand)
		{
            GameObject go = new GameObject("Obi Emitter");
            ObiEmitter emitter = go.AddComponent<ObiEmitter>();
            ObiEmitterShapeDisk shape = go.AddComponent<ObiEmitterShapeDisk>();
            ObiParticleRenderer renderer = go.AddComponent<ObiParticleRenderer>();
            shape.Emitter = emitter;
            ObiEditorUtils.PlaceActorRoot(go, menuCommand);
		}
		
		ObiEmitter emitter;
		
		public void OnEnable()
        {
		
			emitter = (ObiEmitter)target;
            emitter.UpdateEmitterDistribution();

            emitterBlueprint = serializedObject.FindProperty("emitterBlueprint");

            collisionMaterial = serializedObject.FindProperty("m_CollisionMaterial");

            fluidPhase = serializedObject.FindProperty("fluidPhase");
            emissionMethod = serializedObject.FindProperty("emissionMethod");
            minPoolSize = serializedObject.FindProperty("minPoolSize");
            speed = serializedObject.FindProperty("speed");
            lifespan = serializedObject.FindProperty("lifespan");
            randomVelocity = serializedObject.FindProperty("randomVelocity");
            useShapeColor = serializedObject.FindProperty("useShapeColor");
		}

		public override void OnInspectorGUI() 
        {
			
			serializedObject.Update();

            EditorGUILayout.HelpBox((emitter.isEmitting?"Emitting...":"Idle") + "\nActive particles:"+ emitter.activeParticleCount,MessageType.Info);

            EditorGUILayout.PropertyField(emitterBlueprint, new GUIContent("Blueprint"));
            EditorGUILayout.PropertyField(collisionMaterial, new GUIContent("Collision material"));

            EditorGUI.BeginChangeCheck();
            var newCategory = EditorGUILayout.Popup("Collision category", ObiUtils.GetCategoryFromFilter(emitter.Filter), ObiUtils.categoryNames);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(emitter, "Set collision category");
                emitter.Filter = ObiUtils.MakeFilter(ObiUtils.GetMaskFromFilter(emitter.Filter), newCategory);
            }

            EditorGUI.BeginChangeCheck();
            var newMask = EditorGUILayout.MaskField("Collides with", ObiUtils.GetMaskFromFilter(emitter.Filter), ObiUtils.categoryNames);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(emitter, "Set collision mask");
                emitter.Filter = ObiUtils.MakeFilter(newMask, ObiUtils.GetCategoryFromFilter(emitter.Filter));
            }

            EditorGUILayout.PropertyField(emissionMethod, new GUIContent("Emission method"));
            EditorGUILayout.PropertyField(minPoolSize, new GUIContent("Min pool size"));
            EditorGUILayout.PropertyField(speed, new GUIContent("Speed"));
            EditorGUILayout.PropertyField(lifespan, new GUIContent("Lifespan"));
            EditorGUILayout.PropertyField(randomVelocity, new GUIContent("Random velocity"));
            EditorGUILayout.PropertyField(useShapeColor, new GUIContent("Use shape color"));
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
			}
			
		}
		
	}
}




