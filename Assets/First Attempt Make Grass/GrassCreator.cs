using System.Collections.Generic;
using UnityEngine;

public class GrassCreator : MonoBehaviour {
	public GrassHolder GrassHolder;

	// Constant For Creating Low Discrepancy Sequence 
	private const float g = 1.32471795572f;

	public bool TryGeneratePoints(GameObject obj, int countGrass) {
		if (obj.TryGetComponent(out MeshFilter sourceMesh)) {
			GrassHolder ??= GetComponent<GrassHolder>();


			if (GrassHolder is null || countGrass < 0)
				return false;

			// Get Data from Mesh
			// ------------------
			var triangles = sourceMesh.sharedMesh.triangles;
			var vertices = sourceMesh.sharedMesh.vertices;
			var normals = sourceMesh.sharedMesh.normals;

			// Transform to world for right calculations
			for (int i = 0; i < normals.Length; i++) {
				vertices[i] = obj.transform.localToWorldMatrix * vertices[i];
				normals[i] = obj.transform.localToWorldMatrix.inverse.transpose * normals[i];
			}

			float surfaceAreas = CalculateSurfaceArea(triangles, vertices, out var areas);
			// ------------------

			// Generation Algorithm
			// --------------------
			GrassData grassData = new GrassData();
			grassData.color = new Vector3(0, 1, 0);
			Vector3 a, b, c, v1, v2, offset;
			for (int i = 0; i < areas.Length; i++) {
				grassData.normal = normals[triangles[i * 3]];
				// Define Two Main Vectors for Creating Points On Triangle
				// -------------------------------------------------------
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
				// -------------------------------------------------------

				// Generating Points
				// -----------------
				float r1, r2;
				var countGrassOnTriangle = (int)(countGrass * areas[i] / surfaceAreas);
				for (int j = 1; j < countGrassOnTriangle+1; j++) {
					r1 = (j / g) % 1;
					r2 = (j / g / g) % 1;
					if (r1 + r2 > 1) {
						r1 = 1 - r1;
						r2 = 1 - r2;
					}

					grassData.position = obj.transform.position + offset + r1 * v1 + r2 * v2;
					GrassHolder.grassData.Add(grassData);
				}
				// -----------------
			}
			// --------------------

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