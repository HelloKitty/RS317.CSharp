using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Rs317.Sharp
{
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	public sealed class TerrainRenderer : MonoBehaviour
	{
		private Mesh TerrainMesh;

		private Vector3[] vertexData = new Vector3[resX * resZ];
		private Color32[] vertexColorData = new Color32[resX * resZ];
		private int[] triangleData = new int[((resX - 1) * (resZ - 1)) * 6];

		const int resX = 105; // 2 minimum
		const int resZ = 105;

		private Texture2D terrainTexture;

		[SerializeField]
		private bool useFlatShading = false;

		void Start()
		{
			TerrainMesh = new Mesh();
			terrainTexture = new Texture2D(105, 105, TextureFormat.RGBA32, false, true);

			for(int z = 0; z < resZ; z++)
			{
				for(int x = 0; x < resX; x++)
				{
					vertexData[x + z * resX] = new Vector3(x, 0f, z);
				}
			}

			int nbFaces = (resX - 1) * (resZ - 1);
			int t = 0;
			for(int face = 0; face < nbFaces; face++)
			{
				// Retrieve lower left corner from face ind
				int i = face % (resX - 1) + (face / (resZ - 1) * resX);

				triangleData[t++] = i + resX;
				triangleData[t++] = i + 1;
				triangleData[t++] = i;

				triangleData[t++] = i + resX;
				triangleData[t++] = i + resX + 1;
				triangleData[t++] = i + 1;
			}

			Vector2[] uvs = new Vector2[vertexData.Length];
			for (int v = 0; v < resZ; v++)
			{
				for (int u = 0; u < resX; u++)
				{
					uvs[u + v * resX] = new Vector2((float) u / (resX - 1), (float) v / (resZ - 1));
				}
			}


			TerrainMesh.vertices = vertexData;
			TerrainMesh.triangles = triangleData;
			TerrainMesh.uv = uvs;

			GetComponent<MeshFilter>().sharedMesh = TerrainMesh;
			GetComponent<MeshRenderer>().sharedMaterial.mainTexture = terrainTexture;
		}

		void FixedUpdate()
		{
			if (RsUnityClient.intGroundArray == null)
				return;

			/*float[,] heights = new float[105, 105];
			for (int i = 0; i < vertexData.Length; i++)
			{
				int x = (int) vertexData[i].x;
				int y = (int) vertexData[i].y;

				int localX = Mathf.FloorToInt((x / 1f));
				int localY = Mathf.FloorToInt((y / 1f));
				heights[x, y] = -RsUnityClient.intGroundArray[0][localY][localX] / 100.0f;

				vertexData[i] = new Vector3(x, heights[x, y], y);
			}


			TerrainMesh.vertices = vertexData;
			TerrainMesh.triangles = triangleData;
			TerrainMesh.RecalculateNormals();
			TerrainMesh.RecalculateBounds();
			TerrainMesh.Optimize();*/

			float[,] heights = new float[105, 105];
			for(int z = 0; z < resZ; z++)
			{
				for(int x = 0; x < resX; x++)
				{
					int localX = Mathf.FloorToInt((x / 1f));
					int localY = Mathf.FloorToInt((z / 1f));
					heights[x, z] = -RsUnityClient.intGroundArray[0][localY][localX] / 100.0f;

					vertexData[x + z * resX] = new Vector3(x, heights[x, z], z);
					//vertexColorData[x + z * resX] = ComputeColorFromRGB(RsUnityClient.worldController.groundColorArray[0][z][x]);
					vertexColorData[x + z * resX] = ComputeColorFromRGB(ColorUtils.HSLToRGBMap[RsUnityClient.worldController.groundColorArray[0][z][x]], RsUnityClient.worldController.groundColorAmbientOcculusionArray[0][z][x]);
					/*if (RsUnityClient.worldController != null && RsUnityClient.worldController.groundArray != null)
					{
						Tile tile = RsUnityClient.worldController.groundArray[0][Math.Min(z, 103)][Math.Min(x, 103)];

						if (tile != null)
						{
							if(tile.shapedTile != null)
							{
								vertexColorData[x + z * resX] = ComputeColorFromRGB(tile.shapedTile.overlayRGB);
							}
							else if (tile.plainTile != null)
								vertexColorData[x + z * resX] = ComputeColorFromRGB(tile.plainTile.colourRGB);
						}
					}
					else
					{
						Debug.LogWarning($"Unable to draw terrain");
					}*/
				}
			}

			TerrainMesh.vertices = vertexData;
			TerrainMesh.triangles = triangleData;
			TerrainMesh.SetColors(vertexColorData);

			TerrainMesh.RecalculateNormals();
			TerrainMesh.RecalculateTangents();

			/*for(int z = 0; z < resZ; z++)
			{
				for(int x = 0; x < resX; x++)
				{
					int localX = Mathf.FloorToInt((x / 1f));
					int localY = Mathf.FloorToInt((z / 1f));

					if (RsUnityClient.worldController != null && RsUnityClient.worldController.groundArray != null)
					{
						Tile tile = RsUnityClient.worldController.groundArray[0][Math.Min(z, 103)][Math.Min(x, 103)];

						if(tile.plainTile != null)
							vertexColorData[x + z * resX] = ComputeColorFromRGB(tile.plainTile.colourRGB);
						else
							vertexColorData[x + z * resX] = new Color32(0, 0, 0, 0);
					}
					else
					{
						Debug.LogWarning($"Unable to draw terrain");
					}
				}
			}

			terrainTexture.SetPixels32(vertexColorData);
			terrainTexture.Apply(false);
			GetComponent<MeshRenderer>().sharedMaterial.mainTexture = terrainTexture;*/
		}

		private Color32 ComputeColorFromRGB(int color)
		{
			return new Color(((color >> 16) & 0xFF) / 255.0f, ((color >> 8) & 0xFF) / 255.0f, (color & 0xFF) / 255.0f).linear;
		}

		private Color32 ComputeColorFromRGB(int color, int alpha)
		{
			return new Color(((color >> 16) & 0xFF) / 255.0f, ((color >> 8) & 0xFF) / 255.0f, (color & 0xFF) / 255.0f, alpha / 255.0f).linear;
		}
	}
}
