using ImGuiNET;
using UnityEngine;

namespace Other.BuildGUI {
	public class GrassTool {

		public GrassTool(GrassCreator grassCreator, GameObject[] gameObjects) {
			_grassCreator = grassCreator;
			meshFilterTree = BuildTree<MeshFilter>(gameObjects);
		}
		
		private GrassCreator _grassCreator;
		private MeshFilter _selectedMeshFilter;
		private int _countGrassPerMesh;
		private string _selectedMeshFilterName = "";
		private TreeNode<MeshFilter> meshFilterTree;
		
		public bool enable;
		
		public void Draw() {
			ImGui.Begin("Grass Creator", ref enable);
			if (_grassCreator is null) {
				ImGui.Text(
					"Grass Creator is null. Please, in editor add component to DearImguiWrapper field 'Grass Creator'");
			}
			else {
				ImGui.Text($"Grass on Scene: {_grassCreator.GrassHolder.grassData.Count}");
		
				ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.0f, 0.9f, 0.0f, 0.5f));
				if (ImGui.Button("Generate Grass") && _selectedMeshFilter != null) {
					_grassCreator.TryGeneratePoints(_selectedMeshFilter.gameObject, _countGrassPerMesh);
				}
				ImGui.PopStyleColor();
				ImGui.SameLine();
				ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.9f, 0.0f, 0.0f, 0.5f));
				if (ImGui.Button("Release Grass")) {
					_grassCreator.GrassHolder.Release();
				}
				ImGui.PopStyleColor();
				ImGui.SameLine();
				if (ImGui.Button("Update")) {
					_grassCreator.GrassHolder.OnEnable();
				}
				
				ImGui.InputText("Selected", ref _selectedMeshFilterName, 256, ImGuiInputTextFlags.ReadOnly);
				ImGui.InputInt("Count Grass", ref _countGrassPerMesh, 1000);
				ImGui.Text("Scene:");
				ImGui.BeginChild("Scrolling");
				DrawTree(meshFilterTree);
				
				ImGui.EndChild();
			}
		
			ImGui.End();
		}
		
		private void DrawTree(TreeNode<MeshFilter> tree) {
			foreach (var node in tree.children) {
				var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
				if (node.children.Count == 0)
					flags |= ImGuiTreeNodeFlags.Leaf;

				bool clickable = node.component is not null;

				if (!clickable) {
					ImGui.PushStyleColor(ImGuiCol.Header, Vector4.one);
					ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Vector4.one);
					ImGui.PushStyleColor(ImGuiCol.HeaderActive, Vector4.one);
				}

				bool nodeOpen = ImGui.TreeNodeEx(node.name, flags);
				if (ImGui.IsItemClicked() && clickable) {
					_selectedMeshFilterName = node.name;
					_selectedMeshFilter = node.component;
				}

				if (!clickable) {
					ImGui.PopStyleColor(3);
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