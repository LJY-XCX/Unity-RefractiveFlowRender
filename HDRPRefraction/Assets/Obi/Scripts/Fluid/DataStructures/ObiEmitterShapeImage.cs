using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;

namespace Obi
{

    [AddComponentMenu("Physics/Obi/Emitter shapes/Image", 873)]
	[ExecuteInEditMode]
	public class ObiEmitterShapeImage : ObiEmitterShape
	{
		public Texture2D image = null;
		public float pixelScale = 0.05f;
		public float maxSize = 2;

		[Range(0,1)]	
		public float maskThreshold = 0.5f;

		public override void GenerateDistribution(){

			distribution.Clear(); 

			if (image == null) return;

			float width,height;
			GetWorldSpaceEmitterSize(out width,out height);

			int numX = Mathf.FloorToInt(width/particleSize);
			int numY = Mathf.FloorToInt(height/particleSize);

			for (int x = 0; x < numX; ++x){
				for (int y = 0; y < numY; ++y){

					Color sample = image.GetPixelBilinear(x/(float)numX,y/(float)numY);
					if (sample.a > maskThreshold){

						Vector3 pos = new Vector3(x*particleSize - width*0.5f ,y*particleSize - height*0.5f,0);
						Vector3 vel = Vector3.forward;
	
						distribution.Add(new ObiEmitterShape.DistributionPoint(pos,vel,sample));
					}	
				}
			}
	
		}

		private void GetWorldSpaceEmitterSize(out float width, out float height){

			width = image.width*pixelScale;
			height = image.height*pixelScale;
			float ratio = width/height;
	
			if (width > maxSize || height > maxSize){
				if (width > height){
					width = maxSize;
					height = width / ratio;
				}else{
 					height = maxSize;
					width = ratio * height;
				}
			}

		}

	#if UNITY_EDITOR
		public void OnDrawGizmosSelected(){

			if (image == null) return;	

			Handles.matrix = transform.localToWorldMatrix;
			Handles.color  = Color.cyan;

			float width,height;
			GetWorldSpaceEmitterSize(out width,out height);

			float sx = width*0.5f;
			float sy = height*0.5f;

			Vector3[] corners = {new Vector3(-sx,-sy,0),
								 new Vector3(sx,-sy,0),
							     new Vector3(sx,sy,0),
								 new Vector3(-sx,sy,0),
								 new Vector3(-sx,-sy,0)};

			Handles.DrawPolyLine(corners);

			foreach (DistributionPoint point in distribution)
				Handles.ArrowHandleCap(0,point.position,Quaternion.LookRotation(point.velocity),0.05f,EventType.Repaint);

		}
	#endif

	}
}

