using System;
using System.Reflection;
using ImGuiNET;
using UnityEngine;

namespace Other.BuildGUI {
	public class Inspector{
		private bool _private;
		private bool _public;

		public Component[] selectedObjectComponents;
		public bool enable;
	
		public void Draw(GameObject selectedGameObject) {
			ImGui.Begin("Inspector", ref enable);
			if (selectedGameObject == null) {
				ImGui.End();
				return;
			}

			ImGui.Text($"'{selectedGameObject.name}'");
			if (selectedObjectComponents == null) {
				selectedObjectComponents = selectedGameObject.GetComponents(typeof(Component));
			}


			ImGui.Text($"Tag: {selectedGameObject.tag}, Layer: {selectedGameObject.layer}");

			ImGui.Text("Filters");
			ImGui.SameLine();
			ImGui.Checkbox("Private", ref _private);
			ImGui.SameLine();
			ImGui.Checkbox("Public", ref _public);
		
			ImGui.Text("Components");
			ImGui.BeginChild("Scrolling");
			foreach (var component in selectedObjectComponents) {
				if (ImGui.TreeNode(component.GetType().Name)) {
					Type componentType = component.GetType();


					var flags = BindingFlags.Default;
					if (_private) {
						flags |= BindingFlags.NonPublic;
					}

					if (_public) {
						flags |= BindingFlags.Public;
					}

					PropertyInfo[] propertyInfos = null;
					FieldInfo[] fieldInfos = null;

					if (ImGui.BeginTabBar("Property Or Filter")) {
						if (ImGui.BeginTabItem("Fields")) {
							fieldInfos = componentType.GetFields(flags | BindingFlags.Instance);
							ImGui.EndTabItem();
						}

						if (ImGui.BeginTabItem("Properties")) {
							propertyInfos = componentType.GetProperties(flags | BindingFlags.Instance);
							ImGui.EndTabItem();
						}

						ImGui.EndTabBar();
					}


					// Create two columns
					ImGui.Columns(2, "PropertiesTableColumns", true);

					// Headers
					ImGui.Text("Fields");
					ImGui.NextColumn();
					ImGui.Text("Value");
					ImGui.NextColumn();
					ImGui.Separator();

					if (fieldInfos != null) {
						foreach (var field in fieldInfos) {
							ImGui.Text($"{field.Name}");
							ImGui.NextColumn();
							try {
								ImGui.Text(field.GetValue(component).ToString());
							}
							catch {
								ImGui.Text("UNREADABLE");
							}

							ImGui.NextColumn();
							ImGui.Separator();
						}
					}

					if (propertyInfos != null) {
						foreach (var field in propertyInfos) {
							try {
								var value = field.GetValue(component).ToString();
								ImGui.Text($"{field.Name}");
								ImGui.NextColumn();
								ImGui.Text(value);
								ImGui.NextColumn();
								ImGui.Separator();
							}
							catch {
								continue;
							}
						}
					}
					ImGui.Columns(1);
					ImGui.TreePop();
				}
			}

			ImGui.EndChild();
			ImGui.End();
		}
	}
}