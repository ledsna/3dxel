using System.IO;
using System.Collections.Generic;
using UnityEngine;

public static class GrassDataManager {
	public static bool TryLoadGrassData(string filePath, out List<GrassData> grassData) {
		grassData = new List<GrassData>();
		// Create a FileStream and BinaryReader
		var data = new GrassData();
		using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			using (BinaryReader binaryReader = new BinaryReader(fileStream)) {
				while (fileStream.Position < fileStream.Length) {
					data.position = ReadVector3(binaryReader);
					data.normal = ReadVector3(binaryReader);
					grassData.Add(data);
				}
			}

		return true;
	}

	public static bool TrySaveGrassData(string filePath, List<GrassData> grassData) {
		using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
			using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
			{
				foreach (var data in grassData) {
					SaveVector3(data.position, binaryWriter);
					SaveVector3(data.normal, binaryWriter);
				}
			}

		return true;
	}

	private static void SaveVector3(Vector3 vector, BinaryWriter writer) {
		writer.Write(vector.x);
		writer.Write(vector.y);
		writer.Write(vector.z);
	}

	private static Vector3 ReadVector3(BinaryReader binaryReader) {
		Vector3 res;
		res.x = binaryReader.ReadSingle();
		res.y = binaryReader.ReadSingle();
		res.z = binaryReader.ReadSingle();
		return res;
	}
}