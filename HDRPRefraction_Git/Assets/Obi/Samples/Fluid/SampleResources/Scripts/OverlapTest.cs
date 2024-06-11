using UnityEngine;
using Obi;

public class OverlapTest : MonoBehaviour
{
    public ObiSolver solver;
    public Transform[] cubes;

    ObiNativeQueryShapeList queries;
    ObiNativeAffineTransformList transforms;
    ObiNativeQueryResultList results;

    private void Start()
    {
        queries = new ObiNativeQueryShapeList();
        transforms = new ObiNativeAffineTransformList();
        results = new ObiNativeQueryResultList();
    }

    private void OnDestroy()
    {
        queries.Dispose();
        transforms.Dispose();
        results.Dispose();
    }

    void Update()
    {
        int filter = ObiUtils.MakeFilter(ObiUtils.CollideWithEverything, 0);

        queries.Clear();
        transforms.Clear();

        for (int i = 0; i < cubes.Length; ++i)
        {
            queries.Add(new QueryShape()
            {
                type = QueryShape.QueryType.Box,
                center = Vector3.zero,
                size = new Vector3(1,1,1),
                contactOffset = 0,
                maxDistance = 0,
                filter = filter
            });

            transforms.Add(new AffineTransform(cubes[i].position,cubes[i].rotation,cubes[i].localScale));
        }

        solver.SpatialQuery(queries,transforms,results);

        for (int i = 0; i < solver.colors.Length; ++i)
            solver.colors[i] = Color.cyan;

        // Iterate over results and draw their distance to the center of the cube.
        // We're assuming the solver only contains 0-simplices (particles).
        for (int i = 0; i < results.count; ++i)
        {
            if (results[i].distance < 0)
            {
                int particleIndex = solver.simplices[results[i].simplexIndex];

                if (results[i].queryIndex == 0)
                    solver.colors[particleIndex] = Color.red;
                else if (results[i].queryIndex == 1)
                    solver.colors[particleIndex] = Color.yellow;
            }
        }
    }
}