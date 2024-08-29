using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GrassCreator : MonoBehaviour {
	public GrassHolder GrassHolder;

	// Constant For Creating Low Discrepancy Sequence 
	private const float g = 1.32471795572f;

	public bool TryGeneratePoints(GameObject obj, int countGrass) {
		if (obj.TryGetComponent(out MeshFilter sourceMesh)) {
			GrassHolder ??= GetComponent<GrassHolder>();


			if (GrassHolder is null || countGrass < 0)
				return false;

			// Pass root surface's shader variables to grass instances
			//Material meshMaterial = obj.GetComponent<MeshRenderer>().sharedMaterial;
			//GrassHolder._rootMeshMaterial = meshMaterial;

			// Get Data from Mesh
			var triangles = sourceMesh.sharedMesh.triangles;
			var vertices = sourceMesh.sharedMesh.vertices;
			var normals = sourceMesh.sharedMesh.normals;

			// cache
			var localToWorldMatrix = obj.transform.localToWorldMatrix;
			var localToWorldMatrixInverseTranspose = obj.transform.localToWorldMatrix.inverse.transpose;
			var objPosition = obj.transform.position;

			// Transform to world for right calculations
			for (int i = 0; i < normals.Length; i++) {
				vertices[i] = localToWorldMatrix * vertices[i];
				normals[i] = localToWorldMatrixInverseTranspose * normals[i];
				normals[i] /= normals[i].magnitude;
			}

			float surfaceAreas = CalculateSurfaceArea(triangles, vertices, out var areas);

			// Generation Algorithm
			GrassData grassData = new GrassData();
			grassData.color = new Vector3(0, 1, 0);
			Vector3 a, b, c, v1, v2, offset;
			for (int i = 0; i < areas.Length; i++) {
				grassData.normal = normals[triangles[i * 3]];
				// Define Two Main Vectors for Creating Points On Triangle
				a = vertices[triangles[i * 3 + 1]] - vertices[triangles[i * 3]];
				b = vertices[triangles[i * 3 + 2]] - vertices[triangles[i * 3 + 1]];
				c = vertices[triangles[i * 3]] - vertices[triangles[i * 3 + 2]];
				if (a.magnitude > b.magnitude && a.magnitude > c.magnitude) {
					v1 = -b;
					v2 = c;
					offset = vertices[triangles[i * 3 + 2]];
				}
				else if (b.magnitude > a.magnitude && b.magnitude > c.magnitude) {
					v1 = a;
					v2 = -c;
					offset = vertices[triangles[i * 3]];
				}
				else {
					v1 = -a;
					v2 = b;
					offset = vertices[triangles[i * 3 + 1]];
				}

				// Generating Points
				float r1, r2;
				var countGrassOnTriangle = (int)(countGrass * areas[i] / surfaceAreas);
				for (int j = 0; j < countGrassOnTriangle; j++) {
					r1 = (j / g) % 1;
					r2 = (j / g / g) % 1;
					if (r1 + r2 > 1) {
						r1 = 1 - r1;
						r2 = 1 - r2;
					}

					grassData.position = objPosition + offset + r1 * v1 + r2 * v2;
					GrassHolder.grassData.Add(grassData);
				}
			}

			GrassHolder.OnEnable();
			return true;
		}
		else if (obj.TryGetComponent(out Terrain terrain)) {
			GrassData grassData = new() {
				color = new Vector3(0, 1, 0)
			};
			// Computing v1, v2 and offset
			var v1 = new Vector3(1, 0, 0) * terrain.terrainData.size.x;//terrain.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(1, 0, 0));
			var v2 = new Vector3(0, 0, 1) * terrain.terrainData.size.z;//terrain.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(0, 0, 1));
			         //terrain.terrainData.size.z;
			var offset = terrain.GetPosition();

			int countCreatedGrass = 0, i = 0;
			while (countCreatedGrass < countGrass) {
				i++;
				var r1 = (i / g) % 1;
				var r2 = (i / g / g) % 1;
				grassData.position = offset + r1 * v1 + r2 * v2;
				grassData.position.y += terrain.SampleHeight(grassData.position) + 20;
				grassData.normal =terrain.terrainData.GetInterpolatedNormal(r1, r2);
				// Collider[] cols = Physics.OverlapBox(newData.position, Vector3.one * 0.2f, Quaternion.identity, toolSettings.paintBlockMask);
				// if (cols.Length > 0) {
				// 	continue;
				// }

				countCreatedGrass++;
				GrassHolder.grassData.Add(grassData);
			}
			GrassHolder.OnEnable();
			return true;
		}

		return false;
	}

	private float CalculateSurfaceArea(int[] tris, Vector3[] verts, out float[] sizes) {
		float res = 0f;
		int triangleCount = tris.Length / 3;
		sizes = new float[triangleCount];
		for (int i = 0; i < triangleCount; i++) {
			res += sizes[i] = .5f * Vector3.Cross(
				verts[tris[i * 3 + 1]] - verts[tris[i * 3]],
				verts[tris[i * 3 + 2]] - verts[tris[i * 3]]).magnitude;
		}

		return res;
	}

	private void OnEnable() {
		GrassHolder = GetComponent<GrassHolder>();
	}
}