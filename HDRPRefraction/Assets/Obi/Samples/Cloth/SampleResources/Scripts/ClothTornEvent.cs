using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

//[RequireComponent(typeof(ObiTearableCloth))]
public class ClothTornEvent : MonoBehaviour {

	/*ObiTearableCloth cloth;
	public GameObject thingToSpawn;

	void OnEnable () {
		cloth = GetComponent<ObiTearableCloth>();
		cloth.OnConstraintTorn += Cloth_OnConstraintTorn;
	}
	
	void OnDisable(){
		cloth.OnConstraintTorn -= Cloth_OnConstraintTorn;
	}
	
	void Cloth_OnConstraintTorn (object sender, ObiTearableCloth.ObiConstraintTornEventArgs e)
	{
		if (thingToSpawn != null)
			GameObject.Instantiate(thingToSpawn,cloth.Solver.positions[cloth.particleIndices[e.particleIndex]],Quaternion.identity);
	}*/
}
