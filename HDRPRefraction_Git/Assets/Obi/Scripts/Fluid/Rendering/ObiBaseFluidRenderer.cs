using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Obi
{
	[ExecuteInEditMode]
	[RequireComponent (typeof(Camera))]
	public abstract class ObiBaseFluidRenderer : MonoBehaviour{

		public ObiParticleRenderer[] particleRenderers;
		protected CommandBuffer renderFluid;
		protected Camera currentCam;

		void Awake(){
			currentCam = GetComponent<Camera>();
		}

        public void OnEnable()
		{
			GetComponent<Camera>().forceIntoRenderTexture = true;
			DestroyCommandBuffer();
			Cleanup();
		}
		
		public void OnDisable()
		{
			DestroyCommandBuffer();
			Cleanup();
		}

		protected Material CreateMaterial (Shader shader)
	    {
			if (!shader || !shader.isSupported)
	            return null;
	        Material m = new Material (shader);
	        m.hideFlags = HideFlags.HideAndDontSave;
	        return m;
	    }

		protected virtual void Setup(){}
		protected virtual void Cleanup(){}

		/**
		 * Re-generates the CommandBuffer used for fluid rendering. Call it whenever a new ParticleRenderer is added, removed or modified.
		 */
		public abstract void UpdateFluidRenderingCommandBuffer();

		private void DestroyCommandBuffer()
        {
			if (renderFluid != null)
            {
				GetComponent<Camera>().RemoveCommandBuffer (CameraEvent.BeforeImageEffectsOpaque,renderFluid);
				renderFluid = null;
			}
		}

		private void OnPreRender()
        {

			bool act = gameObject.activeInHierarchy && enabled;
			if (!act || particleRenderers == null || particleRenderers.Length == 0)
			{
				DestroyCommandBuffer();
				Cleanup();
				return;
			}
	
			Setup();
	
			if (renderFluid == null)
			{
				renderFluid = new CommandBuffer();
				renderFluid.name = "Render fluid";
				currentCam.AddCommandBuffer (CameraEvent.BeforeImageEffectsOpaque, renderFluid);
			}
            else
            {
				UpdateFluidRenderingCommandBuffer();
			}
		}
	}
}

