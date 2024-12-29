using System.Collections.Generic;
using UnityEngine;

public class GrassCullingTree {
	public Bounds bounds;
	public List<GrassCullingTree> children = new List<GrassCullingTree>();
	public List<int> grassIDHeld = new List<int>();
	public bool isDrawn = true;

	public GrassCullingTree(Bounds bounds, int depth) {
		children.Clear();
		this.bounds = bounds;

		if (depth > 0) {
			var size = bounds.size;
			size /= 4.0f;
			var childSize = bounds.size / 2.0f;
			var center = bounds.center;

			childSize.y = bounds.size.y;
			Bounds topLeftSingle = new Bounds(new Vector3(center.x - size.x, center.y, center.z - size.z), childSize);
			Bounds bottomRightSingle =
				new Bounds(new Vector3(center.x + size.x, center.y, center.z + size.z), childSize);
			Bounds topRightSingle = new Bounds(new Vector3(center.x - size.x, center.y, center.z + size.z), childSize);
			Bounds bottomLeftSingle =
				new Bounds(new Vector3(center.x + size.x, center.y, center.z - size.z), childSize);

			children.Add(new GrassCullingTree(topLeftSingle, depth - 1));
			children.Add(new GrassCullingTree(bottomRightSingle, depth - 1));
			children.Add(new GrassCullingTree(topRightSingle, depth - 1));
			children.Add(new GrassCullingTree(bottomLeftSingle, depth - 1));
		}
	}

	public bool FindLeaf(Vector3 point, int index) {
		bool FoundSpot = false;
		if (bounds.Contains(point)) {
			if (children.Count != 0) {
				foreach (var child in children)
					if (child.FindLeaf(point, index))
						return true;
			}
			else {
				grassIDHeld.Add(index);
				return true;
			}
		}

		return FoundSpot;
	}

	public void RecalculateBoundsHeight(List<GrassData> grassData) {
		if (children.Count > 0) {
			foreach (var child in children) {
				child.RecalculateBoundsHeight(grassData);
			}
			float highestY = float.NegativeInfinity, lowestY = float.PositiveInfinity;
			foreach (var child in children) {
				highestY = Mathf.Max(highestY, child.bounds.size.y/2 + child.bounds.center.y);
				lowestY = Mathf.Min(lowestY, child.bounds.center.y - child.bounds.size.y/2);
			}
			bounds.center = new Vector3(bounds.center.x, (highestY + lowestY) / 2, bounds.center.z);
			bounds.size = new Vector3(bounds.size.x, highestY - lowestY,bounds.size.z);
		}
		else {
			float highestY = float.NegativeInfinity, lowestY = float.PositiveInfinity;
			foreach (var grassID in grassIDHeld) {
				var y = grassData[grassID].position.y;
				highestY = Mathf.Max(highestY, y);
				lowestY = Mathf.Min(lowestY, y);
			}
			bounds.center = new Vector3(bounds.center.x, (highestY + lowestY) / 2, bounds.center.z);
			bounds.size = new Vector3(bounds.size.x, highestY - lowestY,bounds.size.z);
		}
	}

	public void RetrieveLeaves(Plane[] frustum, List<int> visibleIDList) {
		isDrawn = false;
		if (GeometryUtility.TestPlanesAABB(frustum, bounds)) {
			isDrawn = true;
			if (children.Count == 0) {
				if (grassIDHeld.Count > 0) {
					visibleIDList.AddRange(grassIDHeld);
				}
			}
			else {
				foreach (var child in children) {
					child.RetrieveLeaves(frustum, visibleIDList);
				}
			}
		}
	}

	public void Release() {
		foreach (var child in children) {
			child.Release();
		}
		grassIDHeld.Clear();
		children.Clear();
	}
}