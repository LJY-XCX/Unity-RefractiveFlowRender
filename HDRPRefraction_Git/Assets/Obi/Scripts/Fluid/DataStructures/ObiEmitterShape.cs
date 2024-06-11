using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Obi{

	[ExecuteInEditMode]
	public abstract class ObiEmitterShape : MonoBehaviour
	{
		[Serializable]
		public struct DistributionPoint{
			public Vector3 position;
			public Vector3 velocity;
			public Color color;

			public DistributionPoint(Vector3 position, Vector3 velocity){
				this.position = position;
				this.velocity = velocity;
				this.color = Color.white;
			}

			public DistributionPoint(Vector3 position, Vector3 velocity, Color color){
				this.position = position;
				this.velocity = velocity;
				this.color = color;
			}

			public DistributionPoint GetTransformed(Matrix4x4 transform, Color multiplyColor){
				return new DistributionPoint(transform.MultiplyPoint3x4(position),
											 transform.MultiplyVector(velocity),
										     color*multiplyColor);
			}
		}

		[SerializeProperty("Emitter")]
		[SerializeField] protected ObiEmitter emitter;

		public Color color = Color.white;

		[HideInInspector] public float particleSize = 0;
		[HideInInspector] public List<DistributionPoint> distribution = new List<DistributionPoint>();

		protected Matrix4x4 l2sTransform;

		public ObiEmitter Emitter{
			set{
				if (emitter != value){

					if (emitter != null){
						emitter.RemoveShape(this);
					}

					emitter = value;
					
					if (emitter != null){
						emitter.AddShape(this);
					}
				}
			}
			get{return emitter;}
		}

		public Matrix4x4 ShapeLocalToSolverMatrix{
			get{return l2sTransform;}
		}

		public void OnEnable(){
			if (emitter != null)
				emitter.AddShape(this);
		}

		public void OnDisable(){
			if (emitter != null)
				emitter.RemoveShape(this);
		}

		public void UpdateLocalToSolverMatrix(){
            if (emitter != null && emitter.solver != null){
				l2sTransform = emitter.solver.transform.worldToLocalMatrix * transform.localToWorldMatrix;
			}else{
				l2sTransform = transform.localToWorldMatrix;
			}
		}

		public abstract void GenerateDistribution();
		
	}
}

