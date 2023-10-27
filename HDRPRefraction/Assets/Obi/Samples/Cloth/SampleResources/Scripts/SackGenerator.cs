using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

/**
 * Generates a fully procedural cloth sack at runtime. This is done by generating a mesh,
 * creating a cloth blueprint and making two cloth pieces out of it. Then they are sewn together using
 * the ObiStitcher component.
 */
public class SackGenerator : MonoBehaviour {

	public Material outsideMaterial;			    /**< material used for rendering the sack.*/
	public Material insideMaterial;			    /**< material used for rendering the sack.*/
	public float sackSize = 1; 				/**< size of the sack in world units.*/
	public int resolution = 10;   		/**< resolution of sack mesh in quads per side.*/

	/**
     * Generates and returns procedural tesselated square mesh.
     */
	private Mesh GenerateSheetMesh(){

		// create a new mesh:
		Mesh mesh = new Mesh();
		mesh.name = "sack_sheet";

		float quadSize = sackSize/resolution;
		
		int vertexCount = (resolution + 1) * (resolution + 1);
		int triangleCount = resolution * resolution * 2;
		Vector3[] vertices = new Vector3[vertexCount];
		Vector3[] normals = new Vector3[vertexCount];
		Vector4[] tangents = new Vector4[vertexCount];
		Vector2[] uvs = new Vector2[vertexCount];
		int[] triangles = new int[triangleCount * 3];
		
		// generate vertices:
		// for each row:
		for (int y = 0; y < resolution+1; ++y){
			// for each column:
			for (int x = 0; x < resolution+1; ++x){
				int v = y*(resolution+1)+x;
				vertices[v] = new Vector3(quadSize*x,quadSize*y,0);
				normals[v] = Vector3.forward;
				tangents[v] = new Vector4(1,0,0,1);
				uvs[v] = new Vector3(x/(float)resolution,y/(float)resolution);
			}
		}

		// generate triangle faces:
		for (int y = 0; y < resolution; ++y){
			// for each column:
			for (int x = 0; x < resolution; ++x){

				int face = (y*(resolution+1)+x);
				int t = (y*resolution+x)*6;

				triangles[t] = face;
				triangles[t+1] = face+1;
				triangles[t+2] = face+resolution+1;
		
				triangles[t+3] = face+resolution+1;
				triangles[t+4] = face+1;
				triangles[t+5] = face+resolution+2;
			}
		}
	
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.tangents = tangents;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		mesh.RecalculateBounds();
		return mesh;
	}

	private IEnumerator GenerateSack(){

		Mesh mesh = GenerateSheetMesh();

		// create and give a material to both sides of the sack:
		GameObject sheet1 = new GameObject("sack_side1",typeof(MeshFilter),typeof(MeshRenderer), typeof (ObiCloth), typeof (ObiClothRenderer)); 
		sheet1.GetComponent<MeshRenderer>().materials = new Material[]{outsideMaterial,insideMaterial};

		GameObject sheet2 = new GameObject("sack_side2",typeof(MeshFilter),typeof(MeshRenderer), typeof(ObiCloth), typeof(ObiClothRenderer)); 
		sheet2.GetComponent<MeshRenderer>().materials = new Material[]{outsideMaterial,insideMaterial};

		// position both sheets around the center of this object:
		sheet1.transform.parent = transform;
		sheet2.transform.parent = transform;
		sheet1.transform.localPosition = Vector3.forward;
		sheet2.transform.localPosition = -Vector3.forward;

        // generate blueprint:
        var blueprint = ScriptableObject.CreateInstance<ObiClothBlueprint>();
        blueprint.inputMesh = GenerateSheetMesh();
        yield return StartCoroutine(blueprint.Generate());

        // generate cloth for both of them:
        ObiCloth cloth1 = sheet1.GetComponent<ObiCloth>();
        cloth1.clothBlueprint = blueprint;

        ObiCloth cloth2 = sheet2.GetComponent<ObiCloth>();
        cloth2.clothBlueprint = blueprint;

        // sew both sides together:
        ObiStitcher stitcher = gameObject.AddComponent<ObiStitcher>();
        stitcher.Actor1 = cloth1;
        stitcher.Actor2 = cloth2;

		// add stitches: top and bottom edges:
		for (int i = 1; i < resolution; ++i){
			stitcher.AddStitch(i,i);
			stitcher.AddStitch((resolution+1)*resolution + i,(resolution+1)*resolution + i);
		} 

		// sides:
		for (int i = 0; i <= (resolution+1)*resolution; i+=resolution+1){
			stitcher.AddStitch(i,i);
			stitcher.AddStitch(i + resolution ,i + resolution );
		}

        stitcher.PushDataToSolver();

        // adjust bending constraints to obtain a little less rigid fabric:
        cloth1.maxBending = 0.04f;
        cloth2.maxBending = 0.04f; 

        Destroy(this);
    }

	public void Update()
    {
		if (Input.GetKeyDown(KeyCode.Space))
        {
			StartCoroutine(GenerateSack());
		}
	}

}
