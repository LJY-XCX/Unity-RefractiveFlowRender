using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{

	[RequireComponent(typeof(ObiEmitter))]
	public class ViscositySurfTensionToUserData : MonoBehaviour
	{
		void Awake()
        {
            GetComponent<ObiEmitter>().OnEmitParticle += Emitter_OnEmitParticle;
		}

		void Emitter_OnEmitParticle (ObiEmitter emitter, int particleIndex)
		{
			if (emitter.solver != null)
            {
                int k = emitter.solverIndices[particleIndex];
				
				Vector4 userData = emitter.solver.userData[k];
				userData[0] = emitter.solver.viscosities[k];
				userData[1] = emitter.solver.surfaceTension[k];
				emitter.solver.userData[k] = userData;
			}		
		}
	
	}
}

