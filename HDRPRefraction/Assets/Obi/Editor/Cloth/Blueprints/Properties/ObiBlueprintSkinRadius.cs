using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Obi
{
    public class ObiBlueprintSkinRadius : ObiBlueprintFloatProperty
    {

        public ObiBlueprintSkinRadius(ObiActorBlueprintEditor editor) : base(editor,0)
        {
            brushModes.Add(new ObiFloatPaintBrushMode(this));
            brushModes.Add(new ObiFloatAddBrushMode(this));
            brushModes.Add(new ObiFloatSmoothBrushMode(this));
        }

        public override string name
        {
            get { return "Skin radius"; }
        }

        public override float Get(int index)
        {
            var constraints = editor.blueprint.GetConstraintsByType(Oni.ConstraintType.Skin) as ObiConstraints<ObiSkinConstraintsBatch>;
            return constraints.batches[0].skinRadiiBackstop[index * 3];
        }
        public override void Set(int index, float value)
        {
            var constraints = editor.blueprint.GetConstraintsByType(Oni.ConstraintType.Skin) as ObiConstraints<ObiSkinConstraintsBatch>;
            constraints.batches[0].skinRadiiBackstop[index * 3] = value;
        }
        public override bool Masked(int index)
        {
            return !editor.Editable(index);
        }

		public override void OnSceneRepaint()
		{
            var meshEditor = editor as ObiMeshBasedActorBlueprintEditor;
            if (meshEditor != null)
            {
                
                // Get per-particle normals:
                Vector3[] normals = meshEditor.sourceMesh.normals;
                Vector3[] particleNormals = new Vector3[meshEditor.blueprint.particleCount];
                for (int i = 0; i < normals.Length; ++i)
                {
                    int welded = meshEditor.VertexToParticle(i);
                    particleNormals[welded] = normals[i];
                }

                using (new Handles.DrawingScope(Color.blue, Matrix4x4.identity))
                {
                    var constraints = meshEditor.blueprint.GetConstraintsByType(Oni.ConstraintType.Skin) as ObiConstraints<ObiSkinConstraintsBatch>;
                    if (constraints != null)
                    {
                        var batches = constraints.batches;
                        foreach (ObiSkinConstraintsBatch batch in batches)
                        {
                            for (int i = 0; i < batch.activeConstraintCount; ++i)
                            {
                                int particleIndex = batch.particleIndices[i];
                                if (meshEditor.visible[particleIndex])
                                {
                                    Vector3 position = meshEditor.blueprint.GetParticlePosition(particleIndex);
                                    Handles.DrawLine(position, position + particleNormals[particleIndex] * batch.skinRadiiBackstop[i * 3]);
                                }
                            }
                        }
                    }
                }
            }
		}
	}
}
