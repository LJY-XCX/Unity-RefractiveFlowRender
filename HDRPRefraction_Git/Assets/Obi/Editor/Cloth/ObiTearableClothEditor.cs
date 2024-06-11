using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;


namespace Obi
{

    /**
 * Custom inspector for ObiCloth components.
 * Allows particle selection and constraint edition. 
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
    [CustomEditor(typeof(ObiTearableCloth)), CanEditMultipleObjects]
    public class ObiTearableClothEditor : Editor
    {

        [MenuItem("GameObject/3D Object/Obi/Obi Tearable Cloth", false, 401)]
        static void CreateObiRod(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Obi Tearable Cloth", typeof(ObiTearableCloth), typeof(ObiTearableClothRenderer));
            ObiEditorUtils.PlaceActorRoot(go, menuCommand);
        }

        SerializedProperty clothBlueprint;

        SerializedProperty collisionMaterial;
        SerializedProperty selfCollisions;

        SerializedProperty distanceConstraintsEnabled;
        SerializedProperty stretchCompliance;
        SerializedProperty maxCompression;

        SerializedProperty bendConstraintsEnabled;
        SerializedProperty bendCompliance;
        SerializedProperty maxBending;

        SerializedProperty aerodynamicsEnabled;
        SerializedProperty drag;
        SerializedProperty lift;

        SerializedProperty tearingEnabled;
        SerializedProperty tearResistanceMultiplier;
        SerializedProperty tearRate;
        SerializedProperty tearDebilitation;

        public void OnEnable()
        {
            clothBlueprint = serializedObject.FindProperty("m_TearableClothBlueprint");

            collisionMaterial = serializedObject.FindProperty("m_CollisionMaterial");
            selfCollisions = serializedObject.FindProperty("m_SelfCollisions");

            distanceConstraintsEnabled = serializedObject.FindProperty("_distanceConstraintsEnabled");
            stretchCompliance = serializedObject.FindProperty("_stretchCompliance");
            maxCompression = serializedObject.FindProperty("_maxCompression");

            bendConstraintsEnabled = serializedObject.FindProperty("_bendConstraintsEnabled");
            bendCompliance = serializedObject.FindProperty("_bendCompliance");
            maxBending = serializedObject.FindProperty("_maxBending");

            aerodynamicsEnabled = serializedObject.FindProperty("_aerodynamicsEnabled");
            drag = serializedObject.FindProperty("_drag");
            lift = serializedObject.FindProperty("_lift");

            tearingEnabled = serializedObject.FindProperty("tearingEnabled");
            tearResistanceMultiplier = serializedObject.FindProperty("tearResistanceMultiplier");
            tearRate = serializedObject.FindProperty("tearRate");
            tearDebilitation = serializedObject.FindProperty("tearDebilitation");
        }

        public override void OnInspectorGUI()
        {

            serializedObject.UpdateIfRequiredOrScript();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(clothBlueprint, new GUIContent("Blueprint"));
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var t in targets)
                {
                    (t as ObiTearableCloth).RemoveFromSolver();
                    (t as ObiTearableCloth).ClearState();
                }
                serializedObject.ApplyModifiedProperties();
                foreach (var t in targets)
                    (t as ObiTearableCloth).AddToSolver();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collisions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(collisionMaterial, new GUIContent("Collision material"));
            EditorGUILayout.PropertyField(selfCollisions, new GUIContent("Self collisions"));

            EditorGUILayout.Space();
            ObiEditorUtils.DoToggleablePropertyGroup(tearingEnabled, new GUIContent("Tearing"),
            () => {
                EditorGUILayout.PropertyField(tearResistanceMultiplier, new GUIContent("Tear compliance"));
                EditorGUILayout.PropertyField(tearRate, new GUIContent("Tear rate"));
                EditorGUILayout.PropertyField(tearDebilitation, new GUIContent("Tear debilitation"));
            });

            ObiEditorUtils.DoToggleablePropertyGroup(distanceConstraintsEnabled,new GUIContent("Distance Constraints", Resources.Load<Texture2D>("Icons/ObiDistanceConstraints Icon")),
            ()=>{
                EditorGUILayout.PropertyField(stretchCompliance, new GUIContent("Stretch compliance"));
                EditorGUILayout.PropertyField(maxCompression, new GUIContent("Max compression"));
            });

            ObiEditorUtils.DoToggleablePropertyGroup(bendConstraintsEnabled, new GUIContent("Bend Constraints", Resources.Load<Texture2D>("Icons/ObiBendConstraints Icon")),
            () => {
                EditorGUILayout.PropertyField(bendCompliance, new GUIContent("Bend compliance"));
                EditorGUILayout.PropertyField(maxBending, new GUIContent("Max bending"));
            });

            ObiEditorUtils.DoToggleablePropertyGroup(aerodynamicsEnabled, new GUIContent("Aerodynamics", Resources.Load<Texture2D>("Icons/ObiAerodynamicConstraints Icon")),
            () => {
                EditorGUILayout.PropertyField(drag, new GUIContent("Drag"));
                EditorGUILayout.PropertyField(lift, new GUIContent("Lift"));
            });

            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();

        }
    }

}


