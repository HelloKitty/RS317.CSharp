using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Rs317.Sharp;

namespace Rs317.Tools.Cache.Parser
{
	class Program
	{
		public static FileCache[] caches { get; } = new FileCache[5];

		private static ICacheStreamLoaderStrategy CacheLoader { get; } = new FileDirectoryBasedCacheStreamLoader("./cache/");

		public static async Task Main(string[] args)
		{
			Stream cache_dat = await CacheLoader.LoadCacheDatFileAsync()
				.ConfigureAwait(false);

			Stream[] cache_idx = new Stream[5];

			for(int j = 0; j < 5; j++)
				cache_idx[j] = await CacheLoader.LoadCacheIndexFileAsync(j)
					.ConfigureAwait(false);

			for(int i = 0; i < 5; i++)
				caches[i] = new FileCache(cache_dat, cache_idx[i], i + 1);

			//Archive archiveConfig = requestArchive(2, "config", "config", expectedCRCs[2], 30);
			Archive config = requestArchive(2, "config", "config", 30);
			Archive versionsArchive = requestArchive(5, "versions", "versions", 30);

			GameObjectDefinition.load(config);
			MapIndexArchiveDeserializer mapIndexDeserializer = new MapIndexArchiveDeserializer(versionsArchive);
			IReadOnlyList<MapIndex> indices = mapIndexDeserializer.Deserialize();

			Console.WriteLine($"GameObjectDefinitions Loaded: {GameObjectDefinition.DefinitionCount}");

			foreach (MapIndex index in indices)
			{
				Console.WriteLine($"Index X: {index.X} Y: {index.Y}");
			}

			int mapObjectCount = 0;
			int namedObjectCount = 0;
			int actionableObjectCount = 0;

			//MapObjectsListArchiveDeserializer mapObjectsDeserializer = new MapObjectsListArchiveDeserializer(caches[MapConstants.MAP_INDEX], indices.First(i => i.X == 3200 && i.Y == 3200));
			foreach(MapIndex index in indices)
			{
				MapObjectsListArchiveDeserializer mapObjectsDeserializer = new MapObjectsListArchiveDeserializer(caches[MapConstants.MAP_INDEX], index);
				IReadOnlyList<MapObject> mapObjects = mapObjectsDeserializer.Deserialize();
				mapObjectCount += mapObjects.Count;

				foreach(MapObject mapObject in mapObjects)
				{
					GameObjectDefinition definition = GameObjectDefinition.getDefinition(mapObject.Id);

					if(String.IsNullOrWhiteSpace(definition.name))
						continue;

					namedObjectCount++;
					Console.WriteLine($"MapObject: Type: {mapObject.ObjectType} X: {mapObject.LocalX} Y: {mapObject.LocalY} Type: {mapObject.ObjectType}");
					Console.WriteLine($"Definition Id: {definition.id} Name: {definition.name} Actions: {definition.actions?.Aggregate("", (s, s1) => $"{s} {s1}")}");
					Console.WriteLine($"IsWalkable: {definition.walkable} Scale: X: {definition.scaleX} Y: {definition.scaleY} Models: {definition.modelIds?.Aggregate("", (s, i) => $"{s} {i}")}");

					if(definition.actions != null)
						for(int i = 0; i < definition.actions.Length; i++)
							if (!String.IsNullOrWhiteSpace(definition.actions[i]))
							{
								actionableObjectCount++;
								break;
							}
				}
			}

			Console.WriteLine($"MapIndex: {indices.Count} Total Map Objects: {mapObjectCount} WithNames: {namedObjectCount} WithActions: {actionableObjectCount}");

			/*foreach (MapObject mapObject in mapObjects)
			{
				GameObjectDefinition definition = GameObjectDefinition.getDefinition(mapObject.Id);

				if (String.IsNullOrWhiteSpace(definition.name))
					continue;

				Console.WriteLine($"MapObject: Type: {mapObject.ObjectType} X: {mapObject.LocalX} Y: {mapObject.LocalY} Type: {mapObject.ObjectType}");
				Console.WriteLine($"Definition Id: {definition.id} Name: {definition.name} Actions: {definition.actions?.Aggregate("", (s, s1) => $"{s} {s1}")}");
				Console.WriteLine($"IsWalkable: {definition.walkable} Scale: X: {definition.scaleX} Y: {definition.scaleY} Models: {definition.modelIds?.Aggregate("", (s, i) => $"{s} {i}")}");
			}*/

			/*while (true)
			{
				Console.Write($"Enter Definition Id: ");
				string id = Console.ReadLine();
				GameObjectDefinition definition = GameObjectDefinition.getDefinition(int.Parse(id));
				Console.WriteLine($"Definition Id: {definition.id} Name: {definition.name} Actions: {definition.actions?.Aggregate("", (s, s1) => $"{s} {s1}")}");
				Console.WriteLine($"IsWalkable: {definition.walkable} Scale: X: {definition.scaleX} Y: {definition.scaleY} Models: {definition.modelIds?.Aggregate("", (s, i) => $"{s} {i}")}");
			}*/

			/*for (int i = 0; i < GameObjectDefinition.DefinitionCount; i++)
			{
				GameObjectDefinition definition = GameObjectDefinition.getDefinition(i);
				Console.WriteLine($"Definition Id: {definition.id} Name: {definition.name} Actions: {definition.actions?.Aggregate("", (s, s1) => $"{s} {s1}")}");
				Console.ReadKey();
			}*/

			/*for (int i = 0; i < GameObjectDefinition.DefinitionCount; i++)
			{
				GameObjectDefinition definition = GameObjectDefinition.getDefinition(i);
				if (definition != null && definition.name != null && definition.name.ToLower().Contains("party"))
				{
					Console.WriteLine($"Definition Id: {definition.id} Name: {definition.name} Actions: {definition.actions?.Aggregate("", (s, s1) => $"{s} {s1}")}");
					Console.WriteLine($"IsWalkable: {definition.walkable} Scale: X: {definition.scaleX} Y: {definition.scaleY} Models: {definition.modelIds?.Aggregate("", (s, i2) => $"{s} {i2}")}");

					Console.ReadKey();
				}
			}*/

			Console.WriteLine("Finished");
		}

		protected static Archive requestArchive(int i, String s, String s1, int k)
		{
			byte[] abyte0 = null;
			try
			{
				if(caches[0] != null)
					abyte0 = caches[0].decompress(i);
			}
			catch(Exception _ex)
			{
				Console.WriteLine($"Failed to load Cache: {_ex.Message}. Will try other methods. \n StackTrace: {_ex.StackTrace}");
			}

			if(abyte0 != null)
			{
				Archive streamLoader = new Archive(abyte0);
				return streamLoader;
			}
			else
				throw new InvalidOperationException($"Failed to load cache: {s}:{s1}. Index: {i}");
		}
	}
}
