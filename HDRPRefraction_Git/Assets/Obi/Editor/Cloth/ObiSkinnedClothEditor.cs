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
    [CustomEditor(typeof(ObiSkinnedCloth)), CanEditMultipleObjects]
    public class ObiSkinnedClothEditor : Editor
    {

        [MenuItem("GameObject/3D Object/Obi/Obi Skinned Cloth", false, 402)]
        static void CreateObiRod(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Obi Skinned Cloth", typeof(ObiSkinnedCloth), typeof(ObiSkinnedClothRenderer));
            ObiEditorUtils.PlaceActorRoot(go, menuCommand);
        }

        ObiSkinnedCloth actor;

        SerializedProperty solver;
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

        SerializedProperty tetherConstraintsEnabled;
        SerializedProperty tetherCompliance;
        SerializedProperty tetherScale;

        public void OnEnable()
        {
            actor = (ObiSkinnedCloth)target;

            clothBlueprint = serializedObject.FindProperty("m_SkinnedClothBlueprint");

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

            tetherConstraintsEnabled = serializedObject.FindProperty("_tetherConstraintsEnabled");
            tetherCompliance = serializedObject.FindProperty("_tetherCompliance");
            tetherScale = serializedObject.FindProperty("_tetherScale");
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
                    (t as ObiSkinnedCloth).RemoveFromSolver();
                    (t as ObiSkinnedCloth).ClearState();
                }
                serializedObject.ApplyModifiedProperties();
                foreach (var t in targets)
                    (t as ObiSkinnedCloth).AddToSolver();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collisions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(collisionMaterial, new GUIContent("Collision material"));
            EditorGUILayout.PropertyField(selfCollisions, new GUIContent("Self collisions"));

            ObiEditorUtils.DoToggleablePropertyGroup(distanceConstraintsEnabled, new GUIContent("Distance Constraints", Resources.Load<Texture2D>("Icons/ObiDistanceConstraints Icon")),
            () => {
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

            ObiEditorUtils.DoToggleablePropertyGroup(tetherConstraintsEnabled, new GUIContent("Tether Constraints", Resources.Load<Texture2D>("Icons/ObiTetherConstraints Icon")),
            () => {
                EditorGUILayout.PropertyField(tetherCompliance, new GUIContent("Tether compliance"));
                EditorGUILayout.PropertyField(tetherScale, new GUIContent("Tether scale"));
            });

            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();

        }
    }

}


