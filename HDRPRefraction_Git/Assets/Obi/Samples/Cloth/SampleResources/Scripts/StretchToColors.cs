using UnityEngine;
using Obi;

[RequireComponent(typeof(ObiClothBase))]
public class StretchToColors : MonoBehaviour
{
	ObiClothBase cloth;

	public Gradient gradient;
    public float minForce = 0;
    public float maxForce = 10;

    float[] forces;
    int[] counts;

    void Start()
    {
		cloth = GetComponent<ObiClothBase>();
        cloth.OnEndStep += Cloth_OnEndStep;

        forces = new float[cloth.particleCount];
        counts = new int[cloth.particleCount];
    }

    private void OnDestroy()
    {
        cloth.OnEndStep -= Cloth_OnEndStep;
    }

    private void Cloth_OnEndStep(ObiActor actor, float substepTime)
    {
        if (Mathf.Approximately(maxForce, 0))
            return;

        var dc = cloth.GetConstraintsByType(Oni.ConstraintType.Distance) as ObiConstraints<ObiDistanceConstraintsBatch>;
        var sc = cloth.solver.GetConstraintsByType(Oni.ConstraintType.Distance) as ObiConstraints<ObiDistanceConstraintsBatch>;

        if (dc != null && sc != null)
        {

            float sqrTime = substepTime * substepTime;

            for (int j = 0; j < dc.batches.Count; ++j)
            {
                var batch = dc.batches[j] as ObiDistanceConstraintsBatch;
                var solverBatch = sc.batches[j] as ObiDistanceConstraintsBatch;

                for (int i = 0; i < batch.activeConstraintCount; i++)
                {
                    // divide lambda by squared delta time to get force in newtons:
                    int offset = cloth.solverBatchOffsets[(int)Oni.ConstraintType.Distance][j];
                    float force = -solverBatch.lambdas[offset + i] / sqrTime;

                    int p1 = batch.particleIndices[i * 2];
                    int p2 = batch.particleIndices[i * 2+1];

                    if (cloth.solver.invMasses[cloth.solverIndices[p1]] > 0 ||
                        cloth.solver.invMasses[cloth.solverIndices[p2]] > 0)
                    {
                        forces[p1] += force;
                        forces[p2] += force;

                        counts[p1]++;
                        counts[p2]++;
                    }
                }
            }

            // average force over each particle, map to color, and reset forces:
            for (int i = 0; i < cloth.solverIndices.Length; ++i)
            {
                if (counts[i] > 0)
                {
                    int solverIndex = cloth.solverIndices[i];
                    cloth.solver.colors[solverIndex] = gradient.Evaluate((forces[i] / counts[i] - minForce) / (maxForce - minForce));
                    forces[i] = 0;
                    counts[i] = 0;
                }
            }

        }
    }


}
