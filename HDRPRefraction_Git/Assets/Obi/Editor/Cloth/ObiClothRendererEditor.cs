using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{

    [CustomEditor(typeof(ObiClothRendererBase),true)]
    public class ObiClothRendererEditor : Editor
    {

        private void BakeMesh()
        {
            ObiClothRendererBase clothRenderer = (ObiClothRendererBase)target;
            if (clothRenderer.clothMesh != null)
            {
                ObiEditorUtils.SaveMesh(clothRenderer.clothMesh, "Save cloth mesh", "cloth mesh");
            }
        }

        public override void OnInspectorGUI()
        {

            serializedObject.UpdateIfRequiredOrScript();

            if (GUILayout.Button("Bake Mesh"))
            {
                BakeMesh();
            }

            Editor.DrawPropertiesExcluding(serializedObject, "m_Script");

            // Apply changes to the serializedProperty
            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();

        }

    }

}

