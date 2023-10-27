using UnityEngine;
using Obi;

[RequireComponent(typeof(ObiSolver))]
public class Melt : MonoBehaviour {

	public float heat = 0.1f;
	public float cooling = 0.1f;

 	ObiSolver solver;
	public ObiCollider hotCollider = null;
	public ObiCollider coldCollider = null;

	void Awake(){
		solver = GetComponent<ObiSolver>();
	}

	void OnEnable () {
		solver.OnCollision += Solver_OnCollision;
	}

	void OnDisable(){
		solver.OnCollision -= Solver_OnCollision;
	}
	
	void Solver_OnCollision (object sender, ObiSolver.ObiCollisionEventArgs e)
	{
        var colliderWorld = ObiColliderWorld.GetInstance();

        for (int i = 0;  i < e.contacts.Count; ++i)
		{
			if (e.contacts.Data[i].distance < 0.001f)
			{
                var col = colliderWorld.colliderHandles[e.contacts.Data[i].bodyB].owner;
                if (col != null)
                {
                    int k = e.contacts.Data[i].bodyA;

					Vector4 userData = solver.userData[k];
					if (col == hotCollider){
						userData[0] = Mathf.Max(0.05f,userData[0] - heat * Time.fixedDeltaTime);
						userData[1] = Mathf.Max(0.5f,userData[1] - heat * Time.fixedDeltaTime);
					}else if (col == coldCollider){
						userData[0] = Mathf.Min(10,userData[0] + cooling * Time.fixedDeltaTime);
						userData[1] = Mathf.Min(2,userData[1] + cooling * Time.fixedDeltaTime);
					}
					solver.userData[k] = userData;
				}
			}
		}

	}

}
