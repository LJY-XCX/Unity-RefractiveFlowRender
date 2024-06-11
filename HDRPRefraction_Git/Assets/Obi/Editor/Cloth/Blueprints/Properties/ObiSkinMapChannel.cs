using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Obi
{
    public class ObiSkinMapChannel : ObiBlueprintIntProperty
    {
        public ObiTriangleSkinMapEditor obiTriangleSkinMapEditor;

        public ObiSkinMapChannel(ObiTriangleSkinMapEditor obiTriangleSkinMapEditor) : base(0, 32)
        {
            this.obiTriangleSkinMapEditor = obiTriangleSkinMapEditor;
            brushModes.Add(new ObiMasterSlavePaintBrushMode(this));
        }

        public override string name
        {
            get { return "Skin channel"; }
        }

        public override int Get(int index)
        {
            if (obiTriangleSkinMapEditor.subject == ObiTriangleSkinMapEditor.SubjectBeingEdited.Master)
                return (int)obiTriangleSkinMapEditor.skinMap.m_MasterChannels[index];
            else
                return (int)obiTriangleSkinMapEditor.skinMap.m_SlaveChannels[index];
        }
        public override void Set(int index, int value)
        {
            if (obiTriangleSkinMapEditor.subject == ObiTriangleSkinMapEditor.SubjectBeingEdited.Master)
                obiTriangleSkinMapEditor.skinMap.m_MasterChannels[index] = (uint) value;
            else
                obiTriangleSkinMapEditor.skinMap.m_SlaveChannels[index] = (uint)value;
        }
        public override bool Masked(int index)
        {
            return !obiTriangleSkinMapEditor.facingCamera[index];
        }
    }
}
