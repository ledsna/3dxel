using ImGuiNET;
using Other.BuildGUI;
using UnityEngine;
using UnityEngine.SceneManagement;


public class DearImGUIWrapper : MonoBehaviour {
	public bool MouseInsideImguiWindow;

	private void OnEnable() => ImGuiUn.Layout += OnLayout;

	private void OnDisable() => ImGuiUn.Layout -= OnLayout;

	[SerializeField] private GrassCreator grassCreator;
	
	[SerializeField] private bool _drawMainMenuBar = true;
	private bool _drawDemoWindow;
	
	private Hierarchy hierarchy;
	private Statistics statistics;
	private GrassTool grassTool;
	
	private void Start() {
		var gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
		hierarchy = new Hierarchy(gameObjects);
		statistics = new Statistics();
		grassTool = new GrassTool(grassCreator, gameObjects);
	}
	
	private void Update() {
		MouseInsideImguiWindow = ImGui.IsAnyItemActive() || ImGui.IsAnyItemHovered();
	}






	private void DrawMainMenuBar() {
		if (ImGui.BeginMainMenuBar()) {
			if (ImGui.BeginMenu("Tools")) {
			 	ImGui.MenuItem("Frame Rate", shortcut: "Ctrl+F", ref statistics.enableFPS);
			 	ImGui.EndMenu();
			}
			
			if (ImGui.BeginMenu("Debug")) {
				ImGui.MenuItem("Create Grass", shortcut: "", ref grassTool.enable);
				ImGui.MenuItem("Hierarchy", shortcut: "", ref hierarchy.enable);
				ImGui.MenuItem("Demo Window", shortcut: "", ref _drawDemoWindow);
				ImGui.EndMenu();
			}
			
			if (ImGui.BeginMenu("Edit")) {
				if (ImGui.MenuItem("Exit", _drawMainMenuBar)) {
					_drawMainMenuBar = false;
				}
				if (ImGui.MenuItem("Close All")) {
					statistics.enableFPS = false;
					hierarchy.enable = false;
					hierarchy.inspector.enable = false;
					_drawDemoWindow = false;
					grassTool.enable = false;
				}
				ImGui.EndMenu();
			}
			ImGui.EndMainMenuBar();
		}
	}

	private void OnLayout() {
		if (_drawMainMenuBar)
			DrawMainMenuBar();
		
		if (_drawDemoWindow) {
			ImGui.ShowDemoWindow();
		}

		if (hierarchy.enable) {
			hierarchy.Draw();
		}

		if (statistics.enableFPS) {
			statistics.DrawFPS();
		}

		if (grassTool.enable) {
			grassTool.Draw();
		}
	}
}