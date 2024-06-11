using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System;

namespace Obi
{
    [CustomEditor(typeof(ObiClothBlueprintBase), true)]
    public class ObiClothBlueprintEditor : ObiMeshBasedActorBlueprintEditor
    {

        public override Mesh sourceMesh
        {
            get { return clothBlueprint != null && clothBlueprint.topology != null ? clothBlueprint.topology.inputMesh : null; }
        }

        public ObiClothBlueprintBase clothBlueprint
        {
            get { return blueprint as ObiClothBlueprintBase; }
        }

        protected override bool ValidateBlueprint()
        {
            if (clothBlueprint != null && clothBlueprint.inputMesh != null)
            {
                if (!clothBlueprint.inputMesh.isReadable)
                {
                    NonReadableMeshWarning(clothBlueprint.inputMesh);
                    return false;
                }
                return true;
            }
            return false;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            properties.Add(new ObiBlueprintMass(this));
            properties.Add(new ObiBlueprintRadius(this));
            properties.Add(new ObiBlueprintFilterCategory(this));
            properties.Add(new ObiBlueprintColor(this));

            renderModes.Add(new ObiBlueprintRenderModeMesh(this));
            renderModes.Add(new ObiBlueprintRenderModeDistanceConstraints(this));
            renderModes.Add(new ObiBlueprintRenderModeBendConstraints(this));
            renderModes.Add(new ObiBlueprintRenderModeTetherConstraints(this));
            renderModes.Add(new ObiBlueprintRenderModeAerodynamicConstraints(this));

            tools.Clear();
            tools.Add(new ObiParticleSelectionEditorTool(this));
            tools.Add(new ObiPaintBrushEditorTool(this));
            tools.Add(new ObiPropertyTextureEditorTool(this));
        }

        public override int VertexToParticle(int vertexIndex)
        {
            return clothBlueprint.topology.rawToWelded[vertexIndex];
        }
    }


}
