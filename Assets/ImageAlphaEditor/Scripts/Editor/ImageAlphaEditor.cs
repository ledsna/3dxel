using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using UnityEditor.UIElements;

namespace Editor {
	public class ImageAlphaEditor : EditorWindow {
		private ObjectField textureField;
		private DropdownField alphaDropdown;
		private GradientField alphaGradient;
		private VisualElement imagePreview;
		private SliderInt alphaSlider;
		private Button exportButton;
		private string outputName;
		private Texture2D selectedTexture;
		private Texture2D outputTexture;
		private VisualElement customTexValues;
		private DropdownField textureOption;
		private IntegerField widthField;
		private IntegerField heightField;
		private Button createTexButton;
		private ColorField tint;
		private ComputeShader shader;
		private IntegerField alphaInput;

		[MenuItem("Tools/Image Alpha Editor")]
		public static void OpenEditorWindow() {
			ImageAlphaEditor wnd = GetWindow<ImageAlphaEditor>();
			wnd.titleContent = new GUIContent("Image Alpha Editor");
			wnd.maxSize = new Vector2(320, 480);
			wnd.minSize = wnd.maxSize;
		}


		private void CreateGUI() {
			shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ImageAlphaEditor/Scripts/AlphaEdit.compute");
			VisualElement root = rootVisualElement;
			var visualTree =
				AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
					"Assets/ImageAlphaEditor/Resources/UI Documents/ImageAlphaEditorWindow.uxml");
			VisualElement tree = visualTree.Instantiate();
			root.Add(tree);

			// Assign Elements
			// Q — query 
			textureField = root.Q<ObjectField>("texture-field");
			alphaDropdown = root.Q<DropdownField>("alpha-dropdown");
			textureOption = root.Q<DropdownField>("texture-option");
			alphaGradient = root.Q<GradientField>();
			imagePreview = root.Q<VisualElement>("image-preview");
			customTexValues = root.Q<VisualElement>("custom-tex-values");
			alphaSlider = root.Q<SliderInt>();
			exportButton = root.Q<Button>("export-button");
			createTexButton = root.Q<Button>("create-tex-button");
			widthField = root.Q<IntegerField>("width-field");
			heightField = root.Q<IntegerField>("height-field");
			tint = root.Q<ColorField>("tint");
			alphaInput = root.Q<IntegerField>("alpha-input");

			// Assign Callbacks
			textureField.RegisterValueChangedCallback<Object>(TextureSelected);
			alphaDropdown.RegisterValueChangedCallback<string>(AlphaOptionSelected);
			textureOption.RegisterValueChangedCallback<string>(TextureOptionSelected);
			alphaSlider.RegisterValueChangedCallback<int>(AlphaSliderChanged);
			alphaInput.RegisterValueChangedCallback<int>(AlphaInputChanged);
			alphaGradient.RegisterValueChangedCallback<Gradient>(AlphaGradientChanged);
			tint.RegisterValueChangedCallback<Color>(TintChanged);
			exportButton.clicked += () => ExportImage(outputTexture);
			createTexButton.clicked += CreateTexture;

			imagePreview.style.backgroundImage = null;
			TextureOptionSelected(null);
			AlphaOptionSelected(null);
		}

		#region Button Methods

		private void CreateTexture() {
			int texWidth = widthField.value, texHeight = heightField.value;
			selectedTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
			for (int y = 0; y < texHeight; y++)
				for (int x = 0; x < texWidth; x++)
					selectedTexture.SetPixel(x, y, Color.white);
			selectedTexture.Apply();
			outputName = "CustomTex";

			SetPreviewDimensions(texWidth, texHeight);

			ApplyAlphaGradient();
		}

		private void SetPreviewDimensions(int texWidth, int texHeight) {
			var greaterWidth = (texWidth > texHeight);
			float xRation = 1, yRatio = 1;
			if (greaterWidth)
				yRatio = (float)texHeight / texWidth;
			else
				xRation = (float)texWidth / texHeight;
			imagePreview.style.width = 250 * xRation;
			imagePreview.style.height = 250 * yRatio;
		}

		private void ExportImage(Texture2D texture2D) {
			var path = EditorUtility.SaveFilePanel(
				"Save Edited Texture",
				Application.dataPath,
				outputName + ".png",
				"png"
			);
			var bytes = texture2D.EncodeToPNG();
			if (string.IsNullOrEmpty(path))
				return;
			File.WriteAllBytes(path, bytes);

			var filePath = Path.GetRelativePath("Assets", path);
			AssetDatabase.ImportAsset(filePath);
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = (Texture2D)AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D));
		}

		#endregion

		#region Alpha Methods

		private void ApplyAlphaGradient() {
			if (selectedTexture == null) {
				exportButton.SetEnabled(false);
				return;
			}

			exportButton.SetEnabled(true);

			outputTexture = new Texture2D(selectedTexture.width, selectedTexture.height, TextureFormat.RGBA32, false);
			switch (alphaDropdown.index) {
				case 0:
					outputTexture = AlphaWhole();
					break;
				case 1:
					outputTexture = GradientRight(selectedTexture);
					break;
				case 2:
					outputTexture = GradientLeft(selectedTexture);
					break;
				case 3:
					outputTexture = GradientBottom(selectedTexture);
					break;
				case 4:
					outputTexture = GradientTop(selectedTexture);
					break;
			}

			imagePreview.style.backgroundImage = outputTexture;
		}

		private Texture2D GradientTop(Texture2D texture2D) {
			throw new System.NotImplementedException();
		}

		private Texture2D GradientBottom(Texture2D texture2D) {
			throw new System.NotImplementedException();
		}

		private Texture2D GradientLeft(Texture2D texture2D) {
			throw new System.NotImplementedException();
		}

		private Texture2D GradientRight(Texture2D texture2D) {
			throw new System.NotImplementedException();
		}

		private Texture2D AlphaWhole() {
			float alpha = alphaSlider.value / 255f;
			return TexOpacity(alpha);
		}

		#endregion

		#region Compute Shader Methods
		
		private Texture2D TexOpacity(float alpha) {
			var kernelHandle = shader.FindKernel("AlphaWhole");
			var res = new Texture2D(selectedTexture.width, selectedTexture.height, TextureFormat.RGBA32, false);
			
			var resultRenderTex = new RenderTexture(selectedTexture.width, res.height, 32) {
				enableRandomWrite = true
			};
			resultRenderTex.Create();
			var inputTex = new RenderTexture(res.width, res.height, 32) {
				enableRandomWrite = true
			};
			inputTex.Create();

			// Settings Input Texture
			RenderTexture.active = inputTex;
			Graphics.Blit(selectedTexture, inputTex);
			shader.SetTexture(kernelHandle, "Input", inputTex);
			
			// Settings Output Texture
			RenderTexture.active = resultRenderTex;
			shader.SetTexture(kernelHandle, "Result", resultRenderTex);
			
			// Settings Other things
			shader.SetFloat("opacity", alpha);
			shader.SetVector("tint", tint.value);
			int threadGroupX = resultRenderTex.width / 8, threadGroupY = resultRenderTex.height/8;
			if (resultRenderTex.width % 8 > 0)
				threadGroupX++;
			if (resultRenderTex.height % 8 > 0)
				threadGroupY++;
			shader.Dispatch(kernelHandle, threadGroupX, threadGroupY, 1);
			res.ReadPixels(new Rect(0,0, resultRenderTex.width, resultRenderTex.height), 0,0);
			res.Apply();
			RenderTexture.active = null;
			return res;
		}

		#endregion

		#region Event Callbacks

		private void TintChanged(ChangeEvent<Color> evt) {
			ApplyAlphaGradient();
		}

		private void AlphaGradientChanged(ChangeEvent<Gradient> evt) {
			ApplyAlphaGradient();
		}

		private void AlphaInputChanged(ChangeEvent<int> evt) {
			alphaSlider.SetValueWithoutNotify(evt.newValue);
			ApplyAlphaGradient();
		}

		private void AlphaSliderChanged(ChangeEvent<int> evt) {
			alphaInput.SetValueWithoutNotify(evt.newValue);
			ApplyAlphaGradient();
		}

		private void TextureOptionSelected(ChangeEvent<string> evt) {
			if (textureOption.value != textureOption.choices[0]) {
				textureField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
				customTexValues.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
			}
			else {
				textureField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
				customTexValues.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
			}

			selectedTexture = null;
			textureField.value = null;
			imagePreview.style.backgroundImage = null;
			ApplyAlphaGradient();
		}

		private void TextureSelected(ChangeEvent<Object> evt) {
			if (evt.newValue == null) {
				selectedTexture = null;
				imagePreview.style.backgroundImage = null;
				return;
			}

			outputName = evt.newValue.name + "Adjusted";
			selectedTexture = evt.newValue as Texture2D;
			SetPreviewDimensions(selectedTexture.width, selectedTexture.height);
			ApplyAlphaGradient();
		}

		private void AlphaOptionSelected(ChangeEvent<string> evt) {
			if (alphaDropdown.value != alphaDropdown.choices[0]) {
				alphaSlider.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
				alphaGradient.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
			}
			else {
				alphaSlider.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
				alphaGradient.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
			}

			ApplyAlphaGradient();
		}

		#endregion
	}
}