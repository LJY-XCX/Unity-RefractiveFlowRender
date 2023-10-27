using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
	/**
	 * Sample script that colors fluid particles based on their vorticity (2D only)
	 */
	[RequireComponent(typeof(ObiEmitter))]
	public class ColorFromViscosity : MonoBehaviour
	{
		ObiEmitter emitter;

		public float min = 0;
		public float max = 1;
		public Gradient grad;

		void Awake()
        {
			emitter = GetComponent<ObiEmitter>();
		}

		void LateUpdate()
		{
			if (!isActiveAndEnabled)
				return;

			for (int i = 0; i < emitter.solverIndices.Length; ++i){

				int k = emitter.solverIndices[i];

                emitter.solver.colors[k] = grad.Evaluate((emitter.solver.viscosities[k] - min) / (max - min));

				emitter.solver.viscosities[k] = emitter.solver.userData[k][0];
				emitter.solver.surfaceTension[k] = emitter.solver.userData[k][1];
			}
		}
	
	}
}

