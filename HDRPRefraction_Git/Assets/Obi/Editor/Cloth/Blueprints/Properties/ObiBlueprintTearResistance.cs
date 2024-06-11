using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Obi
{
    public class ObiBlueprintTearResistance : ObiBlueprintFloatProperty
    {

        public ObiBlueprintTearResistance(ObiTearableClothBlueprintEditor editor) : base(editor, 0)
        {
            brushModes.Add(new ObiFloatPaintBrushMode(this));
            brushModes.Add(new ObiFloatAddBrushMode(this));
            brushModes.Add(new ObiFloatSmoothBrushMode(this));
        }

        public override string name
        {
            get { return "Tear resistance"; }
        }

        public override float Get(int index)
        {
            return ((ObiTearableClothBlueprint)((ObiTearableClothBlueprintEditor)editor).clothBlueprint).tearResistance[index];
        }
        public override void Set(int index, float value)
        {
            ((ObiTearableClothBlueprint)((ObiTearableClothBlueprintEditor)editor).clothBlueprint).tearResistance[index] = value;
        }
        public override bool Masked(int index)
        {
            return !editor.Editable(index);
        }
    }
}
