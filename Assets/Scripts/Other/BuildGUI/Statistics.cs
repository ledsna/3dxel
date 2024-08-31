using ImGuiNET;

namespace Other.BuildGUI {
	public class Statistics {

		public bool enableFPS;
		
		public void DrawFPS() {
			ImGui.Begin("FPS", ref enableFPS);
			float framerate = ImGui.GetIO().Framerate;
			ImGui.Text($"{framerate:0} FPS\n{(1000.0f / framerate):0.0} ms");
			ImGui.End();
		}
	}
}