using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Obi
{
    [AddComponentMenu("Physics/Obi/Emitter shapes/Voxel Mesh", 870)]
	[ExecuteInEditMode]
	public class ObiEmitterShapeMesh : ObiEmitterShape
	{
        public Mesh mesh;
        public Vector3 scale = Vector3.one;

		public override void GenerateDistribution(){

			distribution.Clear();

            if (particleSize <= 0 || mesh == null) return;

            // Calculate voxel size so that no more than 32^3 points are created:
            Vector3 boundsSize = Vector3.Scale(mesh.bounds.size, Vector3.one);
            float voxelSize = Mathf.Max(boundsSize.x / 32.0f, boundsSize.y / 32.0f, boundsSize.z / 32.0f, particleSize);

            // Voxelize mesh:
            MeshVoxelizer voxelizer = new MeshVoxelizer(mesh, voxelSize);
            voxelizer.Voxelize(scale);

            MeshVoxelizer.Voxel[,,] voxels = voxelizer.voxels;

            // Create one distribution point at the center of each volume/surface voxel:
            for (int x = 0; x < voxels.GetLength(0); ++x)
                for (int y = 0; y < voxels.GetLength(1); ++y)
                    for (int z = 0; z < voxels.GetLength(2); ++z)
                        if (voxels[x, y, z] != MeshVoxelizer.Voxel.Outside)
                        {
                            Vector3 pos = new Vector3(voxelizer.Origin.x + x + 0.5f, voxelizer.Origin.y + y + 0.5f, voxelizer.Origin.z + z + 0.5f) * voxelSize;
                            distribution.Add(new ObiEmitterShape.DistributionPoint(pos, Vector3.forward));
                        }
            
        }

	#if UNITY_EDITOR
		public void OnDrawGizmosSelected(){

			Handles.matrix = transform.localToWorldMatrix;
			Handles.color  = Color.cyan;
            Vector3 size = Vector3.one * particleSize;
			foreach (DistributionPoint point in distribution)
            {
                Handles.DrawWireCube(point.position, size);
                Handles.ArrowHandleCap(0, point.position, Quaternion.LookRotation(point.velocity), 0.05f, EventType.Repaint);
            }
				

		}
	#endif

	}
}

