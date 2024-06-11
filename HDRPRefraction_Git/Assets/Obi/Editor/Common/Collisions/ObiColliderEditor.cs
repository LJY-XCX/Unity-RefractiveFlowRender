using UnityEditor;
using UnityEngine;

namespace Obi{
	
	[CustomEditor(typeof(ObiColliderBase), true), CanEditMultipleObjects] 
	public class ObiColliderEditor : Editor
	{

        ObiColliderBase collider;

        public void OnEnable()
        {
            collider = (ObiColliderBase)target;
        }

        public override void OnInspectorGUI()
        {

            serializedObject.UpdateIfRequiredOrScript();

            EditorGUI.BeginChangeCheck();
            var newCategory = EditorGUILayout.Popup("Collision category", ObiUtils.GetCategoryFromFilter(collider.Filter), ObiUtils.categoryNames);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (ObiColliderBase t in targets)
                {
                    Undo.RecordObject(t, "Set collision category");
                    t.Filter = ObiUtils.MakeFilter(ObiUtils.GetMaskFromFilter(t.Filter), newCategory);
                }
            }

            EditorGUI.BeginChangeCheck();
            var newMask = EditorGUILayout.MaskField("Collides with", ObiUtils.GetMaskFromFilter(collider.Filter), ObiUtils.categoryNames);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (ObiColliderBase t in targets)
                {
                    Undo.RecordObject(t, "Set collision mask");
                    t.Filter = ObiUtils.MakeFilter(newMask, ObiUtils.GetCategoryFromFilter(t.Filter));
                }
            }

            Editor.DrawPropertiesExcluding(serializedObject, "m_Script", "CollisionMaterial", "filter", "Thickness");

            // Apply changes to the serializedProperty
            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }

        }

    }
}


