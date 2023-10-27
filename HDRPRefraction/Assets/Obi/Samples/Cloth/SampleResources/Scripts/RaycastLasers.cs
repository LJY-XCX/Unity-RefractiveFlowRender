using System.Collections.Generic;
using UnityEngine;
using Obi;

public class RaycastLasers : MonoBehaviour
{
    public ObiSolver solver;
    public LineRenderer[] lasers;

    List<Ray> rays = new List<Ray>();

    void Update()
    {
        rays.Clear();

        for (int i = 0; i < lasers.Length; ++i)
        {
            lasers[i].useWorldSpace = true;
            lasers[i].positionCount = 2;
            lasers[i].SetPosition(0, lasers[i].transform.position);
            rays.Add(new Ray(lasers[i].transform.position, lasers[i].transform.up));
        }

        int filter = ObiUtils.MakeFilter(ObiUtils.CollideWithEverything, 0);

        var results = solver.Raycast(rays, filter);

        if (results != null)
        {
            for (int i = 0; i < results.Length; ++i)
            {
                lasers[i].SetPosition(1, rays[i].GetPoint(results[i].distance));

                if (results[i].simplexIndex >= 0)
                {
                    int simplexStartA = solver.simplexCounts.GetSimplexStartAndSize(results[i].simplexIndex, out int simplexSizeA);

                    // Debug draw the simplex we hit (assuming it's a triangle):
                    if (simplexSizeA == 3)
                    {
                        Vector3 pos1 = solver.positions[solver.simplices[simplexStartA]];
                        Vector3 pos2 = solver.positions[solver.simplices[simplexStartA + 1]];
                        Vector3 pos3 = solver.positions[solver.simplices[simplexStartA + 2]];
                        Debug.DrawLine(pos1, pos2, Color.yellow);
                        Debug.DrawLine(pos2, pos3, Color.yellow);
                        Debug.DrawLine(pos3, pos1, Color.yellow);
                    }

                    lasers[i].startColor = Color.red;
                    lasers[i].endColor = Color.red;
                }
                else
                {
                    lasers[i].startColor = Color.blue;
                    lasers[i].endColor = Color.blue;
                }
            }
        }
    }
}
