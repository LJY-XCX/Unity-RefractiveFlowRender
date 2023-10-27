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
    [CustomEditor(typeof(ObiCloth)), CanEditMultipleObjects]
    public class ObiClothEditor : Editor
    {
        [MenuItem("GameObject/3D Object/Obi/Obi Cloth", false, 400)]
        static void CreateObiRod(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Obi Cloth", typeof(ObiCloth), typeof(ObiClothRenderer));
            ObiEditorUtils.PlaceActorRoot(go, menuCommand);
        }

        SerializedProperty clothBlueprint;

        SerializedProperty collisionMaterial;
        SerializedProperty oneSided;
        SerializedProperty selfCollisions;
        SerializedProperty surfaceCollisions;

        SerializedProperty distanceConstraintsEnabled;
        SerializedProperty stretchingScale;
        SerializedProperty stretchCompliance;
        SerializedProperty maxCompression;

        SerializedProperty bendConstraintsEnabled;
        SerializedProperty bendCompliance;
        SerializedProperty maxBending;
        SerializedProperty plasticYield;
        SerializedProperty plasticCreep;

        SerializedProperty volumeConstraintsEnabled;
        SerializedProperty compressionCompliance;
        SerializedProperty pressure;

        SerializedProperty aerodynamicsEnabled;
        SerializedProperty drag;
        SerializedProperty lift;

        SerializedProperty tetherConstraintsEnabled;
        SerializedProperty tetherCompliance;
        SerializedProperty tetherScale;

        public void OnEnable()
        {
            clothBlueprint = serializedObject.FindProperty("m_ClothBlueprint");

            collisionMaterial = serializedObject.FindProperty("m_CollisionMaterial");
            oneSided = serializedObject.FindProperty("m_OneSided");
            selfCollisions = serializedObject.FindProperty("m_SelfCollisions");
            surfaceCollisions = serializedObject.FindProperty("m_SurfaceCollisions");

            distanceConstraintsEnabled = serializedObject.FindProperty("_distanceConstraintsEnabled");
            stretchingScale = serializedObject.FindProperty("_stretchingScale");
            stretchCompliance = serializedObject.FindProperty("_stretchCompliance");
            maxCompression = serializedObject.FindProperty("_maxCompression");

            bendConstraintsEnabled = serializedObject.FindProperty("_bendConstraintsEnabled");
            bendCompliance = serializedObject.FindProperty("_bendCompliance");
            maxBending = serializedObject.FindProperty("_maxBending");
            plasticYield = serializedObject.FindProperty("_plasticYield");
            plasticCreep = serializedObject.FindProperty("_plasticCreep");

            volumeConstraintsEnabled = serializedObject.FindProperty("_volumeConstraintsEnabled");
            compressionCompliance = serializedObject.FindProperty("_compressionCompliance");
            pressure = serializedObject.FindProperty("_pressure");

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
                    (t as ObiCloth).RemoveFromSolver();
                    (t as ObiCloth).ClearState();
                }
                serializedObject.ApplyModifiedProperties();
                foreach (var t in targets)
                    (t as ObiCloth).AddToSolver();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collisions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(collisionMaterial, new GUIContent("Collision material"));
            EditorGUILayout.PropertyField(oneSided, new GUIContent("One sided collisions"));
            EditorGUILayout.PropertyField(selfCollisions, new GUIContent("Self collisions"));
            EditorGUILayout.PropertyField(surfaceCollisions, new GUIContent("Surface collisions"));

            EditorGUILayout.Space();
            ObiEditorUtils.DoToggleablePropertyGroup(distanceConstraintsEnabled, new GUIContent("Distance Constraints", Resources.Load<Texture2D>("Icons/ObiDistanceConstraints Icon")),
            () => {
                EditorGUILayout.PropertyField(stretchingScale, new GUIContent("Stretching scale"));
                EditorGUILayout.PropertyField(stretchCompliance, new GUIContent("Stretch compliance"));
                EditorGUILayout.PropertyField(maxCompression, new GUIContent("Max compression"));
            });

            ObiEditorUtils.DoToggleablePropertyGroup(bendConstraintsEnabled, new GUIContent("Bend Constraints", Resources.Load<Texture2D>("Icons/ObiBendConstraints Icon")),
            () => {
                EditorGUILayout.PropertyField(bendCompliance, new GUIContent("Bend compliance"));
                EditorGUILayout.PropertyField(maxBending, new GUIContent("Max bending"));
                EditorGUILayout.PropertyField(plasticYield, new GUIContent("Plastic yield"));
                EditorGUILayout.PropertyField(plasticCreep, new GUIContent("Plastic creep"));
            });

            ObiEditorUtils.DoToggleablePropertyGroup(volumeConstraintsEnabled, new GUIContent("Volume Constraints", Resources.Load<Texture2D>("Icons/ObiVolumeConstraints Icon")),
            () => {
                EditorGUILayout.PropertyField(compressionCompliance, new GUIContent("Compression compliance"));
                EditorGUILayout.PropertyField(pressure, new GUIContent("Pressure"));
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


