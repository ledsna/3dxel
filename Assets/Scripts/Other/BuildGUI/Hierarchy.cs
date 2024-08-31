using System.Collections.Generic;
using ImGuiNET;
using UnityEngine;



namespace Other.BuildGUI {

	public class TreeNode<T> where T : Component {
		public T component;
		public List<TreeNode<T>> children;
		public string name;

		public TreeNode(GameObject obj) {
			children = new List<TreeNode<T>>();
			if (obj is null) {
				return;
			}

			name = obj.name;
			obj.TryGetComponent(out component);
		}

		public override string ToString() {
			return $"{name}, Children: {children.Count}";
		}
	}
	
	
	public class Hierarchy {
	

		private TreeNode<Transform> transformTree;
		private GameObject selectedGameObject;


		public Inspector inspector;

		public bool enable;

		public Hierarchy(GameObject[] gameObjects) {
			transformTree = BuildTree<Transform>(gameObjects);
			inspector = new Inspector();
		}

		public void Draw() {
			ImGui.Begin("Hierarchy", ref enable);
			ImGui.BeginChild("Scrolling");
			DrawTree(transformTree);
			ImGui.EndChild();
			ImGui.End();
			if (inspector.enable) {
				inspector.Draw(selectedGameObject);
			}
		}

		private void DrawTree<T>(TreeNode<T> treeNode) where T : Component {
			foreach (var node in treeNode.children) {
				var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
				if (node.children.Count == 0)
					flags |= ImGuiTreeNodeFlags.Leaf;

				bool nodeOpen = ImGui.TreeNodeEx(node.name, flags);

				if (ImGui.IsItemClicked()) {
					selectedGameObject = node.component.gameObject;
					inspector.enable = true;
					inspector.selectedObjectComponents = null;
				}

				if (nodeOpen) {
					DrawTree(node);

					ImGui.TreePop();
				}
			}
		}
		private TreeNode<T> BuildTree<T>(GameObject[] rootObjects) where T : Component {
			var tree = new TreeNode<T>(null);

			foreach (GameObject rootObject in rootObjects) {
				var rootNode = new TreeNode<T>(rootObject);
				AddChildrenRecursively(rootNode, rootObject.transform);
				if (rootNode.children.Count != 0 || rootNode.component is not null) {
					tree.children.Add(rootNode);
				}
			}

			return tree;
		}

		private void AddChildrenRecursively<T>(TreeNode<T> parentNode, Transform parentTransform) where T : Component {
			foreach (Transform childTransform in parentTransform) {
				var childNode = new TreeNode<T>(childTransform.gameObject);
				AddChildrenRecursively(childNode, childTransform);
				if (childNode.children.Count != 0 ||
				    childNode.component != null && !childNode.component.gameObject.isStatic) {
					parentNode.children.Add(childNode);
				}
			}
		}
		
	}
}