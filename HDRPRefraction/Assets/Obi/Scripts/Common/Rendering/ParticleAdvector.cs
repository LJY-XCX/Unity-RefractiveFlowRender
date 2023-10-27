using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace Obi{

    [AddComponentMenu("Physics/Obi/Obi Particle Advector", 1002)]
[ExecuteInEditMode]
[RequireComponent(typeof(ParticleSystem))]
public class ParticleAdvector : MonoBehaviour {

	public ObiSolver solver;
	public uint minNeighbors = 4;

	private ParticleSystem ps;
	private ParticleSystem.Particle[] particles;

	ObiNativeVector4List positions;
	ObiNativeVector4List velocities;
	ObiNativeIntList neighbourCount;

	int alive;
	int solverOffset;

	public ParticleSystem Particles{
		get{return ps;}
	}

	void OnEnable(){

		if (solver != null){
            solver.OnEndStep += Solver_OnStepEnd;
		}
	}

	void OnDisable(){
		if (solver != null){
            solver.OnEndStep -= Solver_OnStepEnd;
		}
	}

	void ReallocateParticles(){

		if (ps == null){
			ps = GetComponent<ParticleSystem>();
			ParticleSystem.MainModule main = ps.main;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
		}

		// Array to get/set particles:
		if (particles == null || particles.Length != ps.main.maxParticles){
			particles = new ParticleSystem.Particle[ps.main.maxParticles];
			positions = new ObiNativeVector4List(ps.main.maxParticles);
			velocities = new ObiNativeVector4List(ps.main.maxParticles);
			neighbourCount = new ObiNativeIntList(ps.main.maxParticles);
            positions.count = ps.main.maxParticles;
            velocities.count = ps.main.maxParticles;
            neighbourCount.count = ps.main.maxParticles;
        }

		alive = ps.GetParticles(particles);

	}


    void Solver_OnStepEnd (ObiSolver s)
	{
		if (solver == null) return;

		ReallocateParticles();

		for (int i = 0; i < alive; ++i)
			positions[i] = particles[i].position;

        solver.implementation.InterpolateDiffuseProperties(solver.velocities, positions, velocities, neighbourCount, alive);
        Matrix4x4 s2World = solver.transform.localToWorldMatrix;

        for (int i = 0; i < alive; ++i)
        {
			// kill the particle if it has very few neighbors:
			if (neighbourCount[i] < minNeighbors)
				particles[i].remainingLifetime = 0;

            particles[i].velocity = s2World.MultiplyVector(velocities[i]);
		}

		ps.SetParticles(particles, alive);
	}
}
}