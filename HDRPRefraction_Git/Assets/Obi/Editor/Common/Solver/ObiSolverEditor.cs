using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{

    /**
     * Custom inspector for ObiSolver components.
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
    [CustomEditor(typeof(ObiSolver)), CanEditMultipleObjects]
    public class ObiSolverEditor : Editor
    {

        [MenuItem("GameObject/3D Object/Obi/Obi Solver", false, 100)]
        static void CreateObiSolver(MenuCommand menuCommand)
        {
            GameObject go = ObiEditorUtils.CreateNewSolver();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Selection.activeGameObject = go;
        }

        ObiSolver solver;

        SerializedProperty backend;
        SerializedProperty simulateWhenInvisible;
        SerializedProperty parameters;
        SerializedProperty worldLinearInertiaScale;
        SerializedProperty worldAngularInertiaScale;

        SerializedProperty distanceConstraintParameters;
        SerializedProperty bendingConstraintParameters;
        SerializedProperty particleCollisionConstraintParameters;
        SerializedProperty particleFrictionConstraintParameters;
        SerializedProperty collisionConstraintParameters;
        SerializedProperty frictionConstraintParameters;
        SerializedProperty skinConstraintParameters;
        SerializedProperty volumeConstraintParameters;
        SerializedProperty shapeMatchingConstraintParameters;
        SerializedProperty tetherConstraintParameters;
        SerializedProperty pinConstraintParameters;
        SerializedProperty stitchConstraintParameters;
        SerializedProperty densityConstraintParameters;
        SerializedProperty stretchShearConstraintParameters;
        SerializedProperty bendTwistConstraintParameters;
        SerializedProperty chainConstraintParameters;

        bool constraintsFoldout = false;

        GUIContent constraintLabelContent;

        public void OnEnable()
        {
            solver = (ObiSolver)target;
            constraintLabelContent = new GUIContent();

            backend = serializedObject.FindProperty("m_Backend");
            simulateWhenInvisible = serializedObject.FindProperty("simulateWhenInvisible");
            parameters = serializedObject.FindProperty("parameters");
            worldLinearInertiaScale = serializedObject.FindProperty("worldLinearInertiaScale");
            worldAngularInertiaScale = serializedObject.FindProperty("worldAngularInertiaScale");

            distanceConstraintParameters = serializedObject.FindProperty("distanceConstraintParameters");
            bendingConstraintParameters = serializedObject.FindProperty("bendingConstraintParameters");
            particleCollisionConstraintParameters = serializedObject.FindProperty("particleCollisionConstraintParameters");
            particleFrictionConstraintParameters = serializedObject.FindProperty("particleFrictionConstraintParameters");
            collisionConstraintParameters = serializedObject.FindProperty("collisionConstraintParameters");
            frictionConstraintParameters = serializedObject.FindProperty("frictionConstraintParameters");
            skinConstraintParameters = serializedObject.FindProperty("skinConstraintParameters");
            volumeConstraintParameters = serializedObject.FindProperty("volumeConstraintParameters");
            shapeMatchingConstraintParameters = serializedObject.FindProperty("shapeMatchingConstraintParameters");
            tetherConstraintParameters = serializedObject.FindProperty("tetherConstraintParameters");
            pinConstraintParameters = serializedObject.FindProperty("pinConstraintParameters");
            stitchConstraintParameters = serializedObject.FindProperty("stitchConstraintParameters");
            densityConstraintParameters = serializedObject.FindProperty("densityConstraintParameters");
            stretchShearConstraintParameters = serializedObject.FindProperty("stretchShearConstraintParameters");
            bendTwistConstraintParameters = serializedObject.FindProperty("bendTwistConstraintParameters");
            chainConstraintParameters = serializedObject.FindProperty("chainConstraintParameters");
        }

        public override void OnInspectorGUI()
        {

            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.HelpBox("Used particles:" + solver.allocParticleCount +"\n"+
                                    "Points:" + solver.pointCount + "\n" +
                                    "Edges:" + solver.edgeCount + "\n" +
                                    "Triangles:" + solver.triCount + "\n" +
                                    "Contacts:" + solver.contactCount + "\n" +
                                    "Particle contacts:" + solver.particleContactCount, MessageType.None);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(backend);

#if !(OBI_BURST && OBI_MATHEMATICS && OBI_COLLECTIONS)
            if (backend.enumValueIndex == (int)ObiSolver.BackendType.Burst)
                EditorGUILayout.HelpBox("The Burst backend depends on the following packages: Mathematics, Collections, Jobs and Burst. The default backend (Oni) will be used instead, if possible.", MessageType.Warning);
#endif
#if !(OBI_ONI_SUPPORTED)
            if (backend.enumValueIndex == (int)ObiSolver.BackendType.Oni)
                EditorGUILayout.HelpBox("The Oni backend is not compatible with the target platform. Please switch to a compatible platform, or use the Burst backend instead.", MessageType.Warning);
#endif

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                foreach (var t in targets)
                    (t as ObiSolver).UpdateBackend();
            }

            EditorGUILayout.PropertyField(parameters);
            EditorGUILayout.PropertyField(worldLinearInertiaScale);
            EditorGUILayout.PropertyField(worldAngularInertiaScale);
            EditorGUILayout.PropertyField(simulateWhenInvisible);

            constraintsFoldout = EditorGUILayout.Foldout(constraintsFoldout, "Constraints");
            if (constraintsFoldout)
            {
                constraintLabelContent.text = "Distance";
                EditorGUILayout.PropertyField(distanceConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Bending";
                EditorGUILayout.PropertyField(bendingConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Particle collision / Queries";
                EditorGUILayout.PropertyField(particleCollisionConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Particle friction";
                EditorGUILayout.PropertyField(particleFrictionConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Collision";
                EditorGUILayout.PropertyField(collisionConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Friction";
                EditorGUILayout.PropertyField(frictionConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Skin";
                EditorGUILayout.PropertyField(skinConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Volume";
                EditorGUILayout.PropertyField(volumeConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Shape matching";
                EditorGUILayout.PropertyField(shapeMatchingConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Tether";
                EditorGUILayout.PropertyField(tetherConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Pin";
                EditorGUILayout.PropertyField(pinConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Stitch";
                EditorGUILayout.PropertyField(stitchConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Density";
                EditorGUILayout.PropertyField(densityConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Stretch & Shear";
                EditorGUILayout.PropertyField(stretchShearConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Bend & Twist";
                EditorGUILayout.PropertyField(bendTwistConstraintParameters, constraintLabelContent);

                constraintLabelContent.text = "Chain";
                EditorGUILayout.PropertyField(chainConstraintParameters, constraintLabelContent);
            }

            // Apply changes to the serializedProperty
            if (GUI.changed)
            {

                serializedObject.ApplyModifiedProperties();
                solver.PushSolverParameters();

            }

        }

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Selected)]
        static void DrawGizmoForSolver(ObiSolver solver, GizmoType gizmoType)
        {

            if ((gizmoType & GizmoType.InSelectionHierarchy) != 0)
            {

                Gizmos.color = new Color(1, 1, 1, 0.5f);
                Bounds bounds = solver.Bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

        }

    }
}


