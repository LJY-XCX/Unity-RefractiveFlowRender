using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Obi
{
    public class ObiBlueprintSkinStiffness : ObiBlueprintFloatProperty
    {

        public ObiBlueprintSkinStiffness(ObiActorBlueprintEditor editor) : base(editor,0)
        {
            brushModes.Add(new ObiFloatPaintBrushMode(this));
            brushModes.Add(new ObiFloatAddBrushMode(this));
            brushModes.Add(new ObiFloatSmoothBrushMode(this));
        }

        public override string name
        {
            get { return "Skin compliance"; }
        }

        public override float Get(int index)
        {
            var constraints = editor.blueprint.GetConstraintsByType(Oni.ConstraintType.Skin) as ObiConstraints<ObiSkinConstraintsBatch>;
            return constraints.batches[0].skinCompliance[index];
        }
        public override void Set(int index, float value)
        {
            var constraints = editor.blueprint.GetConstraintsByType(Oni.ConstraintType.Skin) as ObiConstraints<ObiSkinConstraintsBatch>;
            constraints.batches[0].skinCompliance[index] = value;
        }
        public override bool Masked(int index)
        {
            return !editor.Editable(index);
        }
    }
}
