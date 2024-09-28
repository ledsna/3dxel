using UnityEditor;
using UnityEngine;

namespace Editor {
	[CustomPropertyDrawer(typeof(SeparatorAttribute))]
	public class SeparatorDrawer : DecoratorDrawer {
		public override void OnGUI(Rect position) {
			var separatorAttribute = attribute as SeparatorAttribute;
			var separatorRect = new Rect(position.xMin, position.yMin + separatorAttribute.Spacing, position.width,
			                             separatorAttribute.Height);
			EditorGUI.DrawRect(separatorRect, Color.gray);
		}

		public override float GetHeight() {
			var separatorAttribute = attribute as SeparatorAttribute;
			return separatorAttribute.Spacing * 2 + separatorAttribute.Height;
		}
	}
	
}