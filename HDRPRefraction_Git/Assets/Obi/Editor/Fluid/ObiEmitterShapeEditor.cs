using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for all ObiEmitterShape components. Just updates their point distribution when something changes. 
	 */

	[CustomEditor(typeof(ObiEmitterShape), true), CanEditMultipleObjects] 
	public class ObiEmitterShapeEditor : Editor
	{
        [MenuItem("GameObject/3D Object/Obi/Emitter Shapes/Disk", false, 201)]
        static void CreateObiEmitterShapeDisk(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Disk", typeof(ObiEmitterShapeDisk));
            ObiEditorUtils.PlaceActorRoot(go, menuCommand);
        }

        [MenuItem("GameObject/3D Object/Obi/Emitter Shapes/Square", false, 202)]
        static void CreateObiEmitterShapeSquare(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Square", typeof(ObiEmitterShapeSquare));
            ObiEditorUtils.PlaceActorRoot(go, menuCommand);
        }

        [MenuItem("GameObject/3D Object/Obi/Emitter Shapes/Edge", false, 203)]
        static void CreateObiEmitterShapeEdge(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Edge", typeof(ObiEmitterShapeEdge));
            ObiEditorUtils.PlaceActorRoot(go, menuCommand);
        }

        [MenuItem("GameObject/3D Object/Obi/Emitter Shapes/Cube", false, 220)]
        static void CreateObiEmitterShapeCube(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Cube", typeof(ObiEmitterShapeCube));
            ObiEditorUtils.PlaceActorRoot(go, menuCommand);
        }

        [MenuItem("GameObject/3D Object/Obi/Emitter Shapes/Sphere", false, 221)]
        static void CreateObiEmitterShapeSphere(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Sphere", typeof(ObiEmitterShapeSphere));
            ObiEditorUtils.PlaceActorRoot(go, menuCommand);
        }

        [MenuItem("GameObject/3D Object/Obi/Emitter Shapes/Image", false, 222)]
        static void CreateObiEmitterShapeImage(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Image", typeof(ObiEmitterShapeImage));
            ObiEditorUtils.PlaceActorRoot(go, menuCommand);
        }

        [MenuItem("GameObject/3D Object/Obi/Emitter Shapes/Mesh", false, 222)]
        static void CreateObiEmitterShapeMesh(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Mesh", typeof(ObiEmitterShapeMesh));
            ObiEditorUtils.PlaceActorRoot(go, menuCommand);
        }

        ObiEmitterShape shape;
		
		public void OnEnable(){
			shape = (ObiEmitterShape)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfRequiredOrScript();

			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
				shape.GenerateDistribution();
			}
			
		}
		
	}

}

