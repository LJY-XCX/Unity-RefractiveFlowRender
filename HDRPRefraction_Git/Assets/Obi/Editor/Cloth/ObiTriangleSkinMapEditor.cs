using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{

    [CustomEditor(typeof(ObiTriangleSkinMap))]
    public class ObiTriangleSkinMapEditor : Editor
    {
        public enum SubjectBeingEdited
        {
            Master,
            Slave,
        }

        public ObiTriangleSkinMap skinMap;

        SceneSetup[] oldSetup;
        Object oldSelection;

        GameObject masterObject;
        GameObject slaveObject;

        Material paintMaterial;
        Material standardMaterial;

        // skin channel painting stuff:
        static bool editMode = false;
        static bool paintMode = false;
        static int targetSkinChannel = 0;

        ObiRaycastBrush paintBrush;
        ObiSkinMapChannel currentProperty = null;

        Mesh masterMesh = null;
        public SubjectBeingEdited subject = SubjectBeingEdited.Master;

        protected IEnumerator routine;

        Vector3[] wsPositions = new Vector3[0];
        public bool[] facingCamera = new bool[0];

        SerializedProperty barycentricWeight;
        SerializedProperty normalAlignmentWeight;
        SerializedProperty elevationWeight;

        public void OnEnable()
        {

            skinMap = (ObiTriangleSkinMap)target;
            barycentricWeight = serializedObject.FindProperty("barycentricWeight");
            normalAlignmentWeight = serializedObject.FindProperty("normalAlignmentWeight");
            elevationWeight = serializedObject.FindProperty("elevationWeight");

            paintBrush = new ObiRaycastBrush(skinMap.slave,
                                             () =>
                                             {
                                                 // As RecordObject diffs with the end of the current frame,
                                                 // and this is a multi-frame operation, we need to use RegisterCompleteObjectUndo instead.
                                                 Undo.RegisterCompleteObjectUndo(skinMap, "Paint skin channel");
                                             },
                                             () => { SceneView.RepaintAll(); },
                                             () =>
                                             {
                                                 EditorUtility.SetDirty(skinMap);
                                             });


            currentProperty = new ObiSkinMapChannel(this);
            paintBrush.brushMode = new ObiMasterSlavePaintBrushMode(currentProperty);

            Selection.selectionChanged += OnSelectionChange;
        }

        public void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChange;
            DestroyImmediate(masterMesh);
            ExitSkinEditMode();
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        void OnSelectionChange()
        {
            if (editMode)
            {
                if (masterObject != null && Selection.activeGameObject == masterObject)
                    EditMaster();
                if (slaveObject != null && Selection.activeGameObject == slaveObject)
                    EditSlave();
            }
        }

        private void EditMaster()
        {
            subject = SubjectBeingEdited.Master;
            paintBrush.raycastTarget = masterMesh;
            SceneView.RepaintAll();
        }

        private void EditSlave()
        {
            subject = SubjectBeingEdited.Slave;
            paintBrush.raycastTarget = skinMap.slave;
            SceneView.RepaintAll();
        }

        public void GetMaterials()
        {
            if (paintMaterial == null)
                paintMaterial = Resources.Load<Material>("PropertyGradientMaterial");
            if (standardMaterial == null)
                standardMaterial = new Material(Shader.Find("Standard"));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            GetMaterials();

            if (!editMode)
            {
                UpdateNormalMode();
            }
            else
            {
                UpdateEditMode();
            }

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();

                // Apply transform changes back to the slave:
                if (slaveObject != null)
                    skinMap.m_SlaveTransform.Apply(slaveObject.transform);
            }

        }

        private void MeshFromMasterBlueprint()
        {
            DestroyImmediate(masterMesh);
            masterMesh = new Mesh();

            Vector3[] vertices = new Vector3[skinMap.master.activeParticleCount];
            Vector3[] normals = new Vector3[skinMap.master.activeParticleCount];
            List<int> triangles = new List<int>();

            for (int i = 0; i < vertices.Length; ++i)
                vertices[i] = skinMap.master.positions[i];

            for (int i = 0; i < normals.Length; ++i)
                normals[i] = skinMap.master.restNormals[i];

            for (int i = 0; i < skinMap.master.deformableTriangles.Length; i += 3)
            {
                if (skinMap.master.deformableTriangles[i] >= skinMap.master.activeParticleCount ||
                    skinMap.master.deformableTriangles[i + 1] >= skinMap.master.activeParticleCount ||
                    skinMap.master.deformableTriangles[i + 2] >= skinMap.master.activeParticleCount)
                    continue;

                triangles.Add(skinMap.master.deformableTriangles[i]);
                triangles.Add(skinMap.master.deformableTriangles[i+1]);
                triangles.Add(skinMap.master.deformableTriangles[i+2]);
            }

            masterMesh.vertices = vertices;
            masterMesh.normals = normals;
            masterMesh.SetTriangles(triangles, 0);
        }

        void EnterSkinEditMode()
        {
            if (!editMode)
            {
#if (UNITY_2019_1_OR_NEWER)
                SceneView.duringSceneGui += this.OnSceneGUI;
#else
                SceneView.onSceneGUIDelegate += this.OnSceneGUI;
#endif

                oldSelection = Selection.activeObject;
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    ActiveEditorTracker.sharedTracker.isLocked = true;

                    oldSetup = EditorSceneManager.GetSceneManagerSetup();
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

                    if (skinMap.master != null)
                    {
                        MeshFromMasterBlueprint();
                        masterObject = new GameObject("Master mesh", typeof(MeshRenderer), typeof(MeshFilter));
                        masterObject.GetComponent<MeshRenderer>().material = standardMaterial;
                        masterObject.GetComponent<MeshFilter>().sharedMesh = Instantiate(masterMesh);
                        Selection.activeGameObject = masterObject;
                    }

                    if (skinMap.slave != null)
                    {
                        slaveObject = new GameObject("Slave mesh", typeof(MeshRenderer), typeof(MeshFilter));
                        slaveObject.GetComponent<MeshRenderer>().material = standardMaterial;
                        slaveObject.GetComponent<MeshFilter>().sharedMesh = Instantiate(skinMap.slave);
                        skinMap.m_SlaveTransform.Apply(slaveObject.transform);
                    }

                    EditMaster();

                    SceneView.FrameLastActiveSceneView();

                    editMode = true;
                }
            }
        }

        void ExitSkinEditMode()
        {
            if (editMode)
            {

                editMode = false;

                ActiveEditorTracker.sharedTracker.isLocked = false;

                if (SceneManager.GetActiveScene().path.Length <= 0)
                {
                    if (this.oldSetup != null && this.oldSetup.Length > 0)
                    {
                        EditorSceneManager.RestoreSceneManagerSetup(this.oldSetup);
                        this.oldSetup = null;
                    }
                    else
                    {
                        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
                    }
                }

                Selection.activeObject = oldSelection;
                Tools.hidden = false;

#if (UNITY_2019_1_OR_NEWER)
                SceneView.duringSceneGui -= this.OnSceneGUI;
#else
                SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
#endif

            }
        }

        void UpdateNormalMode()
        {
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            EditorGUI.BeginChangeCheck();
            ObiClothBlueprintBase master = EditorGUILayout.ObjectField("Master blueprint", skinMap.master, typeof(ObiClothBlueprintBase), false) as ObiClothBlueprintBase;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(skinMap, "Set skin master");
                skinMap.master = master;
            }

            EditorGUI.BeginChangeCheck();
            Mesh slaveMesh = EditorGUILayout.ObjectField("Slave mesh", skinMap.slave, typeof(Mesh), false) as Mesh;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(skinMap, "Set skin slave");
                skinMap.slave = slaveMesh;
            }

            // Print skin info:
            if (skinMap.bound)
            {
                EditorGUILayout.HelpBox("Skin info generated." + skinMap.skinnedVertices.Count, MessageType.Info);
            }

            // Error / warning messages
            bool errors = false;
            if (skinMap.master == null)
            {
                EditorGUILayout.HelpBox("Please provide a master blueprint.", MessageType.Info);
                errors = true;
            }
            if (skinMap.slave == null)
            {
                EditorGUILayout.HelpBox("Please provide a slave mesh.", MessageType.Info);
                errors = true;
            }

            // Edit mode buttons:
            GUI.enabled = !errors && !Application.isPlaying;
            if (GUILayout.Button("Edit skin map"))
                EditorApplication.delayCall += EnterSkinEditMode;

            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        void UpdateEditMode()
        {
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            EditorGUILayout.PropertyField(barycentricWeight);
            EditorGUILayout.PropertyField(normalAlignmentWeight);
            EditorGUILayout.PropertyField(elevationWeight);

            EditorGUI.BeginChangeCheck();
            paintMode = GUILayout.Toggle(paintMode, new GUIContent("Paint skin", Resources.Load<Texture2D>("PaintButton")), "LargeButton");
            Tools.hidden = paintMode || subject == SubjectBeingEdited.Master;
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();

            // Buttons:
            GUILayout.BeginHorizontal();

                if (GUILayout.Button("Bind") && masterObject != null && slaveObject != null)
                {
                    EditorUtility.SetDirty(target);
                    CoroutineJob job = new CoroutineJob();
                    routine = job.Start(skinMap.Bind());
                    EditorCoroutine.ShowCoroutineProgressBar("Generating skinmap...", ref routine);
                    EditorGUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Done"))
                    EditorApplication.delayCall += ExitSkinEditMode;

            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // skin channel selector:
            if (paintMode)
            {
                EditorGUILayout.Space();
                GUILayout.Box(GUIContent.none, ObiEditorUtils.GetSeparatorLineStyle());

                EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

                // Brush parameters:
                paintBrush.radius = EditorGUILayout.Slider("Brush size", paintBrush.radius, 0.0001f, 0.5f);
                paintBrush.innerRadius = 1;
                paintBrush.opacity = 1;

                EditorGUI.BeginChangeCheck();
                if (paintBrush.brushMode.needsInputValue)
                    currentProperty.PropertyField();
                if (EditorGUI.EndChangeCheck())
                    SceneView.RepaintAll();

                GUILayout.BeginHorizontal();

                if (GUILayout.Button(new GUIContent("Fill"), EditorStyles.miniButtonLeft))
                {
                    if (subject == SubjectBeingEdited.Master)
                        skinMap.FillChannel(skinMap.m_MasterChannels,currentProperty.GetDefault());
                    else
                        skinMap.FillChannel(skinMap.m_SlaveChannels, currentProperty.GetDefault());
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button(new GUIContent("Clear"), EditorStyles.miniButtonMid))
                {
                    if (subject == SubjectBeingEdited.Master)
                        skinMap.ClearChannel(skinMap.m_MasterChannels, currentProperty.GetDefault());
                    else
                        skinMap.ClearChannel(skinMap.m_SlaveChannels, currentProperty.GetDefault());
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button(new GUIContent("Copy"), EditorStyles.miniButtonMid))
                    targetSkinChannel = currentProperty.GetDefault();

                if (GUILayout.Button(new GUIContent("Paste"), EditorStyles.miniButtonRight))
                {
                    if (subject == SubjectBeingEdited.Master)
                        skinMap.CopyChannel(skinMap.m_MasterChannels, targetSkinChannel, currentProperty.GetDefault());
                    else
                        skinMap.CopyChannel(skinMap.m_SlaveChannels, targetSkinChannel, currentProperty.GetDefault());
                    SceneView.RepaintAll();
                }

                GUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
           
            EditorGUILayout.Space();
            GUILayout.Box(GUIContent.none, ObiEditorUtils.GetSeparatorLineStyle());

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Slave transform", EditorStyles.boldLabel);
            if (GUILayout.Button("Reset", EditorStyles.miniButton))
                skinMap.m_SlaveTransform.Reset();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SlaveTransform"));

            EditorGUILayout.EndVertical();
        }

        // OnSceneGUI doesnt seem to be called for ScriptableObjects, so we need to tap onto the (undocumented) SceneView class.
        public void OnSceneGUI(SceneView sceneView)
        {
            if (slaveObject != null)
                skinMap.m_SlaveTransform = new ObiTriangleSkinMap.SkinTransform(slaveObject.transform);

            // Change materials when painting weights:
            if (Event.current.type == EventType.Repaint)
            {
                UpdateSourceObject();
                UpdateTargetObject();
            }

            if (!paintMode || sceneView.camera == null)
                return;

            if (subject == SubjectBeingEdited.Master)
            {
                paintBrush.raycastTransform = Matrix4x4.identity;
                UpdateBrushPositions(masterMesh, sceneView.camera);
            }
            else
            {
                paintBrush.raycastTransform = skinMap.m_SlaveTransform.GetMatrix4X4();
                UpdateBrushPositions(skinMap.slave, sceneView.camera);
            }

            // Update paintbrush:
            if (Camera.current != null && wsPositions != null)
                paintBrush.DoBrush(wsPositions);

        }

        private void UpdateBrushPositions(Mesh mesh, Camera cam)
        {
            System.Array.Resize(ref wsPositions, mesh.vertexCount);
            System.Array.Resize(ref facingCamera, mesh.vertexCount);

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            for (int i = 0; i < vertices.Length; ++i)
            {
                wsPositions[i] = paintBrush.raycastTransform.MultiplyPoint3x4(vertices[i]);
                facingCamera[i] = Vector3.Dot(masterObject.transform.TransformVector(normals[i]), cam.transform.position - wsPositions[i]) > 0;
            }
        }

        void UpdateSourceObject()
        {

            if (masterObject == null)
                return;

            masterObject.GetComponent<MeshRenderer>().material = standardMaterial;

            if (Selection.activeGameObject != masterObject)
                return;

            Selection.objects = new Object[] { masterObject };

            if (paintMode)
            {

                masterObject.GetComponent<MeshRenderer>().material = paintMaterial;
                if (slaveObject != null)
                    slaveObject.GetComponent<MeshRenderer>().material = standardMaterial;

                Mesh mesh = masterObject.GetComponent<MeshFilter>().sharedMesh;
                Color[] colors = new Color[mesh.vertexCount];

                Color active = ObiEditorSettings.GetOrCreateSettings().propertyGradient.Evaluate(1);
                Color inactive = ObiEditorSettings.GetOrCreateSettings().propertyGradient.Evaluate(0);

                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    if ((skinMap.m_MasterChannels[i] & (1 << currentProperty.GetDefault())) != 0)
                        colors[i] = active;
                    else
                        colors[i] = inactive;
                }

                mesh.colors = colors;

                if (paintMaterial.SetPass(1))
                {   
                    Color wireColor = ObiEditorSettings.GetOrCreateSettings().brushWireframeColor;
                    Mesh wireMesh = Instantiate(mesh);
                    for (int i = 0; i < paintBrush.weights.Length; i++)
                    {
                        colors[i] = wireColor * paintBrush.weights[i];
                    }

                    wireMesh.colors = colors;
                    GL.wireframe = true;
                    Graphics.DrawMeshNow(wireMesh, masterObject.transform.localToWorldMatrix);
                    GL.wireframe = false;
                    DestroyImmediate(wireMesh);
                }
            }

        }

        void UpdateTargetObject()
        {

            if (slaveObject == null)
                return;

            slaveObject.GetComponent<MeshRenderer>().material = standardMaterial;

            if (Selection.activeGameObject != slaveObject)
                return;

            Selection.objects = new Object[] { slaveObject };

            if (paintMode)
            {

                slaveObject.GetComponent<MeshRenderer>().material = paintMaterial;
                if (masterObject != null)
                    masterObject.GetComponent<MeshRenderer>().material = standardMaterial;

                Mesh mesh = slaveObject.GetComponent<MeshFilter>().sharedMesh;
                Color[] colors = new Color[mesh.vertexCount];

                Color active = ObiEditorSettings.GetOrCreateSettings().propertyGradient.Evaluate(1);
                Color inactive = ObiEditorSettings.GetOrCreateSettings().propertyGradient.Evaluate(0);

                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    if ((skinMap.m_SlaveChannels[i] & (1 << currentProperty.GetDefault())) != 0)
                        colors[i] = active;
                    else
                        colors[i] = inactive;
                }

                mesh.colors = colors;

                if (paintMaterial.SetPass(1))
                {
                    Color wireColor = ObiEditorSettings.GetOrCreateSettings().brushWireframeColor;
                    Mesh wireMesh = Instantiate(mesh);
                    for (int i = 0; i < paintBrush.weights.Length; i++)
                    {
                        colors[i] = wireColor * paintBrush.weights[i];
                    }

                    wireMesh.colors = colors;
                    GL.wireframe = true;
                    Graphics.DrawMeshNow(wireMesh, slaveObject.transform.localToWorldMatrix);
                    GL.wireframe = false;
                    DestroyImmediate(wireMesh);
                }
            }

        }
    }
}

