using UnityEngine;

[CreateAssetMenu(fileName = "GrassDataAsset", menuName = "Grass System", order = 0)]
public class GrassDataAsset : ScriptableObject {
	[SerializeField] public string filePath;
}