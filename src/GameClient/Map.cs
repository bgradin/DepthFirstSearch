using System;
using System.Collections.Generic;
using System.IO;

namespace AnglerGameClient
{
	public static class Const
	{
		public const int TILE_SIZE = 64;
		public const int MAGIC_NUMBER = 0xE19CEA4;
	}

	public abstract class Tile
	{
		protected int x_coord;
		protected int y_coord;

		public Tile()
		{
			x_coord = -1;
			y_coord = -1;
		}

		public Tile(int x, int y)
		{
			x_coord = x;
			y_coord = y;
		}

		public int X { get { return this.x_coord; } }
		public int Y { get { return this.y_coord; } }
	}

	public class GraphicTile : Tile
	{
		public GraphicTile(int index)
		{
			Graphic = index;
		}

		public GraphicTile(int x, int y)
			: base(x, y)
		{
			Graphic = 0;
		}

		public GraphicTile(int x, int y, int graphicNumber)
			: base(x, y)
		{
			Graphic = graphicNumber;
		}

		public int Graphic { get; set; }
	}

	public class AnimatedTile : GraphicTile
	{
		public AnimatedTile(int x, int y) : base(x, y) { }
		public AnimatedTile(int x, int y, int graphic) : base(x, y, graphic) { Graphic = graphic; frame = 0; }

		public void Animate()
		{
			frame++;
			if (frame >= 4) //four animation states
				frame = 0;
		}

		public void Reset()
		{
			frame = 0;
		}

		private int frame;
		public int Frame { get { return this.frame; } }
	}

	public class SpecialTile : Tile
	{
		private int d_map, d_x, d_y; //dest map warpid, x, y for warps
		private int s_id; //spawn id
		private WarpAnim d_anim;

		public SpecialTileSpec Type { get; set; }

		public int WarpMap
		{
			get
			{
				if (Type != SpecialTileSpec.WARP)
					throw new FieldAccessException("This type of SpecialTile is not a warp.");
				return d_map;
			}
		}

		public int WarpX
		{
			get
			{
				if (Type != SpecialTileSpec.WARP)
					throw new FieldAccessException("This type of SpecialTile is not a warp.");
				return d_x;
			}
		}

		public int WarpY
		{
			get
			{
				if (Type != SpecialTileSpec.WARP)
					throw new FieldAccessException("This type of SpecialTile is not a warp.");
				return d_y;
			}
		}

		public WarpAnim WarpAnim
		{
			get
			{
				if (Type != SpecialTileSpec.WARP)
					throw new FieldAccessException("This type of SpecialTile is not a warp.");
				return this.d_anim;
			}
		}

		public int SpawnID
		{
			get
			{
				if (Type != SpecialTileSpec.GRASS && Type != SpecialTileSpec.WATER)
					throw new FieldAccessException("This type of SpecialTile is not Grass.");
				return s_id;
			}
		}

		public SpecialTile(int x, int y)
			: base(x, y)
		{
			Type = SpecialTileSpec.NONE;
		}

		/// <summary>
		/// Format - None/Wall/Jump: null; Grass/Water: spawn_id; Warp: dest_warp_id, dest_x, dest_y, warpanim;
		/// </summary>
		/// <param name="type">Type to set the special tyle as</param>
		/// <param name="list">See format in summary</param>
		public void SetType(SpecialTileSpec type, params object[] list)
		{
			//updates stored values when type == Type
			switch (type)
			{
				case SpecialTileSpec.NONE:
				case SpecialTileSpec.WALL:
				case SpecialTileSpec.JUMP:
					if (list != null)
						throw new ArgumentException("Invalid parameters for this TileType");
					Type = type;
					d_map = d_x = d_y = s_id = -1;
					break;
				case SpecialTileSpec.GRASS:
				case SpecialTileSpec.WATER:
				case SpecialTileSpec.CAVE:
					if (list == null || list.Length != 1 || !(list[0].GetType() == typeof(int)))
						throw new ArgumentException("Invalid parameters for this TileType");
					Type = type;
					d_map = d_x = d_y = -1;
					s_id = (int)list[0];
					break;
				case SpecialTileSpec.WARP: //TODO: add in WarpAnim
					if (list == null || list.Length != 4
						|| list[0].GetType() != typeof(int) || list[1].GetType() != typeof(int)
						|| list[2].GetType() != typeof(int) || list[3].GetType() != typeof(WarpAnim))
						throw new ArgumentException("Invalid parameters for this TileType");
					Type = type;
					s_id = -1;
					d_map = (int)list[0];
					d_x = (int)list[1];
					d_y = (int)list[2];
					d_anim = (WarpAnim)list[3];
					break;
				case SpecialTileSpec.NUM_VALS:
				default:
					throw new InvalidOperationException("TileType must be a valid value.");
			}
		}

		public void CopyTypeFrom(SpecialTile other)
		{
			this.Type = other.Type;
			this.s_id = other.s_id;
			this.d_map = other.d_map;
			this.d_x = other.d_x;
			this.d_y = other.d_y;
			this.d_anim = other.d_anim;
		}
	}

	//TODO: Add interactive tile for interactive layer
	public class InteractiveTile : Tile
	{
		public InteractiveTile(int x, int y) : base(x, y) { }

	}

	//TODO: Add NPC tile for npcs moving around the map and shit
	// Each NPC tile should be a spawn for a moving NPC
	// Requires implementation of NPC class
	public class NPCTile : Tile
	{
		private bool doesMove = false;
		public bool Moves { get { return this.doesMove; } }

		private uint moveSpeedSec = 0;
		public uint MoveSpeed { get { return this.moveSpeedSec; } }

		private int id;
		public int NPCID { get { return this.id; } }

		/// <summary>
		/// Create a stationary NPC spawn
		/// </summary>
		/// <param name="x">x coordinate of spawn</param>
		/// <param name="y">y coordinate of spawn</param>
		/// <param name="NPC">NPC id</param>
		public NPCTile(int x, int y, int NPC)
			: base(x, y)
		{
			this.id = NPC;
		}
		/// <summary>
		/// Create a moving NPC spawn
		/// </summary>
		/// <param name="x">x coordinate of spawn</param>
		/// <param name="y">y coordinate of spawn</param>
		/// <param name="NPC">NPC id</param>
		/// <param name="speed">movement speed (seconds between moves)</param>
		public NPCTile(int x, int y, int NPC, uint speed)
			: base(x, y)
		{
			this.id = NPC;
			this.doesMove = true;
			this.moveSpeedSec = speed;
		}
		/// <summary>
		/// Change from moving to not moving
		/// </summary>
		/// <param name="moving">specify whether or not the NPC moves</param>
		/// <param name="speed">movement speed (seconds between moves)</param>
		public void Change(bool moving, uint speed = 0)
		{
			this.doesMove = moving;
			this.moveSpeedSec = speed;
		}
	}

	public class Spawn
	{
		int id;
		SortedList<int, int> data;

		public int SpawnID { get { return this.id; } }
		public SortedList<int, int> Spawns { get { return this.data; } }
		public string Name { get; set; }

		public Spawn()
		{
			data = new SortedList<int, int>();
			id = 0;
			Name = "";
		}

		public Spawn(int _id, string name)
		{
			data = new SortedList<int, int>();
			id = _id;
			Name = name;
		}

		public Spawn(Spawn other)
		{
			data = new SortedList<int, int>(other.data);
			id = other.id;
			Name = other.Name;
		}

		public bool AddSpawnPair(int p_id, int freq, bool update = false)
		{
			//no duplicate values of p_id
			if (data.ContainsKey(p_id) && !update)
				return false;
			else if (!data.ContainsKey(p_id) && update)
				return false;

			//freq must not be greater than 100
			int sum = 0;
			foreach (KeyValuePair<int, int> pair in data)
			{
				if (pair.Key == p_id) //in case of update operation
					sum += freq;
				else
					sum += pair.Value;
			}
			if ((update ? sum : sum + freq) > 100)
				return false;

			if (!update)
				data.Add(p_id, freq);
			else
				data[p_id] = freq;

			return true;
		}

		public void RemoveSpawnPair(int p_id)
		{
			if (!data.ContainsKey(p_id))
				return;

			data.Remove(p_id);
		}

		public void Clear()
		{
			this.data.Clear();
		}
	}

	public class Map
	{
		public class MapStringComparer : IComparer<string>
		{
			int IComparer<string>.Compare(string a, string b)
			{
				string[] aParts = a.Split(new char[] { ':' });
				string[] bParts = b.Split(new char[] { ':' });

				int a1 = int.Parse(aParts[0]), a2 = int.Parse(aParts[1]);
				int b1 = int.Parse(bParts[0]), b2 = int.Parse(bParts[1]);

				if (a1 < b1)
					return -1;
				else if (a1 > b1)
					return 1;
				else
				{
					if (a2 < b2)
						return -1;
					else if (a2 > b2)
						return 1;
					else
						return 0;
				}
			}
		}

		private const int DEFAULT_HEIGHT = 40;
		private const int DEFAULT_WIDTH = 40;

		private SortedList<string, Tile>[] layers = new SortedList<string, Tile>[(int)LAYERS.NUM_VALS];
		private List<Spawn> spawns = new List<Spawn>();
		private int w, h;
		private string mapname, fileName;
		private bool isLoaded, isSaved;

		private int TileCount { get { return this.layers[0].Count; } }

		public int Width { get { return this.w; } }
		public int Height { get { return this.h; } }
		public string MapName { get { return this.mapname; } }
		public string FileName { get { return this.fileName; } set { this.fileName = value; } }

		public int Warp { get; set; }
		public MapType Type { get; set; }
		public Microsoft.Xna.Framework.Point PlayerSpawn { get; set; }

		public bool Saved { get { return this.isSaved; } }
		public bool Loaded { get { return this.isLoaded; } }
		public SortedList<string, Tile>[] Layers { get { return this.layers; } }
		public List<Spawn> Spawns { get { return this.spawns; } }

		public List<Shape> SolidObjects { get; private set; }
		public Microsoft.Xna.Framework.Rectangle VisibleBounds { get; set; }
		public Point UpperLeftCorner { get; set; }

		/// <summary>
		/// construct a map directly from a file
		/// </summary>
		/// <param name="fileName">Name of file (absolute or relative to working dir)</param>
		public Map(string fileName)
		{
			LoadFromFile(fileName);
			SolidObjects = new List<Shape>();
		}

		/// <summary>
		/// construct a map directly from a stream
		/// </summary>
		/// <param name="stream">Stream from which to load the map</param>
		public Map(Stream stream)
		{
			LoadFromStream(stream);
			SolidObjects = new List<Shape>();
		}

		/// <summary>
		/// creating a new map in the editor
		/// </summary>
		/// <param name="name">Name of the map</param>
		/// <param name="width">Width of map</param>
		/// <param name="height">Height of map</param>
		public Map(string name, int width = DEFAULT_WIDTH, int height = DEFAULT_HEIGHT)
		{
			init(name, width, height);
			SolidObjects = new List<Shape>();
		}

		/// <summary>
		/// Create a 'deep' copy of a map from another map
		/// </summary>
		/// <param name="other">The other map, to copy from</param>
		public Map(Map other)
		{
			this.fileName = other.fileName;
			this.h = other.h;
			this.w = other.w;
			this.mapname = other.mapname;
			this.isLoaded = other.isLoaded;
			this.isSaved = other.isSaved;

			this.layers = new SortedList<string, Tile>[(int)LAYERS.NUM_VALS];
			for (int i = 0; i < this.layers.Length; ++i)
			{
				this.layers[i] = new SortedList<string, Tile>(other.layers[i], new MapStringComparer());
			}

			this.spawns = new List<Spawn>();
			foreach (Spawn s in other.spawns)
			{
				this.spawns.Add(new Spawn(s));
			}

			this.Warp = other.Warp;
			this.Type = other.Type;
			this.PlayerSpawn = other.PlayerSpawn;
			this.SolidObjects = other.SolidObjects;
		}

		private void init(string name, int width, int height)
		{
			for (int i = 0; i < layers.Length; ++i)
				layers[i] = new SortedList<string, Tile>(width * height, new MapStringComparer());

			for (int a = 0; a < width; a++)
			{
				for (int b = 0; b < height; b++)
					AddTile(a, b, LAYERS.Graphic, new GraphicTile(a, b));
			}

			w = width;
			h = height;
			PlayerSpawn = new Microsoft.Xna.Framework.Point(0, 0);
			mapname = name;
			fileName = "";
			isLoaded = false;
			isSaved = false;
			this.Warp = -1;
			this.Type = MapType.TrenchCave;
			this.PlayerSpawn = new Microsoft.Xna.Framework.Point(0, 0);
		}

		public bool LoadFromStream(Stream s)
		{
			isLoaded = false;

			byte[] all;
			using(var memoryStream = new MemoryStream())
			{
				s.CopyTo(memoryStream);
				all = memoryStream.ToArray();
			}

			CRC32 crc = new CRC32();
			uint crcVal = crc.Check(all, 0, (uint)all.Length - 4); //crc everything except the value of the crc

			s.Seek(-sizeof(int), SeekOrigin.End); //last four bytes (int) is CRC value
			uint fileVal = (uint)s.ReadInt();
			if (crcVal != fileVal)
				return isLoaded;

			s.Seek(0, SeekOrigin.Begin);

			if (s.ReadInt() != Const.MAGIC_NUMBER)
				return isLoaded;

			spawns = new List<Spawn>();

			int loadedLayers = 0;
			while (s.Position < s.Length - 4)
			{
				switch ((MapField)s.ReadInt())
				{
					case MapField.MapInfo:
						w = s.ReadInt();
						h = s.ReadInt();
						Type = (MapType)s.ReadInt();
						mapname = s.ReadString();
						Warp = s.ReadInt();
						PlayerSpawn = new Microsoft.Xna.Framework.Point(s.ReadInt(), s.ReadInt());
						int numLayers = s.ReadInt();

						if (numLayers != this.layers.Length)
							throw new Exception("Invalid number of layers!");
						break;

					case MapField.MapLayer:
						int numTiles = s.ReadInt();
						var newLayer = layers[loadedLayers] = new SortedList<string, Tile>(numTiles);

						for (int i = 0; i < numTiles; i++)
						{
							Tile toAdd = null;
							LAYERS layer = LAYERS.Graphic;

							switch ((TileType)s.ReadInt())
							{
								case TileType.Animated:
									toAdd = new AnimatedTile(s.ReadInt(), s.ReadInt(), s.ReadInt());
									layer = LAYERS.Graphic;
									break;

								case TileType.Graphic:
									toAdd = new GraphicTile(s.ReadInt(), s.ReadInt(), s.ReadInt());
									break;

								case TileType.Special:
									SpecialTile st = new SpecialTile(s.ReadInt(), s.ReadInt());

									SpecialTileSpec tt = (SpecialTileSpec)s.ReadInt();
									object[] param = null;
									switch (tt)
									{
										case SpecialTileSpec.CAVE:
										case SpecialTileSpec.GRASS:
										case SpecialTileSpec.WATER:
											param = new object[] { s.ReadInt() };
											break;
										case SpecialTileSpec.WARP:
											param = new object[] { s.ReadInt(), s.ReadInt(), s.ReadInt(), (WarpAnim)s.ReadInt() };
											break;
									}

									st.SetType(tt, param);
									break;
							}

							if (toAdd != null)
								AddTile(toAdd.X, toAdd.Y, layer, toAdd);
						}

						layers[loadedLayers++] = newLayer;
						break;

					case MapField.SpawnInfo:
						Spawn sp = new Spawn(s.ReadInt(), s.ReadString());

						int numPairs = s.ReadInt();
						for (int i = 0; i < numPairs; i++)
							sp.AddSpawnPair(s.ReadInt(), s.ReadInt());
						break;
				}
			}

			// Make sure we don't have any null layers (they cause problems later)
			for (int i = 0; i < layers.Length; i++)
			{
				if (layers[i] == null)
					layers[i] = new SortedList<string, Tile>();
			}

			fileName = "";
			isLoaded = true;
			isSaved = true;

			return isLoaded;
		}

		public bool LoadFromFile(string filename)
		{
			isLoaded = false; //use instance var isLoaded as return value

			try
			{
				using (BinaryReader br = new BinaryReader(File.OpenRead(filename)))
				{
					LoadFromStream(br.BaseStream);
					fileName = filename;
					return isLoaded;
				}
			}
			catch
			{
				return isLoaded;
			}
		}

		public bool Save(string filename, bool saveAs = false)
		{
			if (isSaved && !saveAs)
				return true;

			// Initially, write to a memory stream so we can CRC the data
			uint crcVal = 0;
			byte[] allBytes = null;
			using (MemoryStream ms = new MemoryStream())
			{
				ms.WriteInt(Const.MAGIC_NUMBER);

				// Map info
				ms.WriteInt((int)MapField.MapInfo);
				ms.WriteInt(w);
				ms.WriteInt(h);
				ms.WriteInt((int)Type);
				ms.WriteString(MapName);
				ms.WriteInt(Warp);
				ms.WriteInt(PlayerSpawn.X);
				ms.WriteInt(PlayerSpawn.Y);
				ms.WriteInt(layers.Length);

				// Write map layers
				foreach (SortedList<string, Tile> layer in this.layers)
				{
					ms.WriteInt((int)MapField.MapLayer);

					// Write number of tiles
					ms.WriteInt(layer.Count);

					foreach (Tile t in layer.Values)
					{
						if (t is AnimatedTile)
						{
							AnimatedTile at = (AnimatedTile)t;

							ms.WriteInt((int)TileType.Animated);
							ms.WriteInt(at.X);
							ms.WriteInt(at.Y);
							ms.WriteInt(at.Graphic);
						}
						else if (t is GraphicTile)
						{
							GraphicTile gt = (GraphicTile)t;

							ms.WriteInt((int)TileType.Graphic);
							ms.WriteInt(gt.X);
							ms.WriteInt(gt.Y);
							ms.WriteInt(gt.Graphic);
						}
						else if (t is SpecialTile)
						{
							SpecialTile st = (SpecialTile)t;

							ms.WriteInt((int)TileType.Special);
							ms.WriteInt(st.X);
							ms.WriteInt(st.Y);
							ms.WriteInt((int)st.Type);

							switch (st.Type)
							{
								case SpecialTileSpec.CAVE:
								case SpecialTileSpec.GRASS:
								case SpecialTileSpec.WATER:
									ms.WriteInt((int)st.SpawnID);
									break;
								case SpecialTileSpec.WARP:
									ms.WriteInt(st.WarpMap);
									ms.WriteInt(st.WarpX);
									ms.WriteInt(st.WarpY);
									ms.WriteInt((int)st.WarpAnim);
									break;
							}
						}
					}
				}

				// Write spawns
				int numSpawnsWritten = 0;
				foreach (Spawn sp in Spawns)
				{
					// If this happens, we shouldn't write any more. Might as well stop now.
					if (numSpawnsWritten > this.Spawns.Count)
						break;

					ms.WriteInt((int)MapField.SpawnInfo);
					ms.WriteInt(sp.SpawnID);
					ms.WriteString(sp.Name);
					ms.WriteInt(sp.Spawns.Count);

					int numPairsWritten = 0;
					foreach (KeyValuePair<int, int> pair in sp.Spawns)
					{
						// Same error condition as before
						if (numPairsWritten > sp.Spawns.Count)
							break;

						ms.WriteInt(pair.Key);
						ms.WriteInt(pair.Value);

						numPairsWritten++;
					}

					numSpawnsWritten++;
				}

				allBytes = new byte[ms.Length];
				ms.Seek(0, SeekOrigin.Begin);
				ms.Read(allBytes, 0, allBytes.Length);
				CRC32 crc = new CRC32();
				crcVal = crc.Check(allBytes);
			}

			using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(filename)))
			{
				bw.Write(allBytes);
				bw.Write(ByteConverter.ToBytes(crcVal));
			}

			isSaved = true;
			fileName = filename;

			return isSaved;
		}

		public Tile GetTile(int x, int y, LAYERS layer)
		{
			Tile ret = null;

			try
			{
				ret = layers[(int)layer][x + ":" + y];
			}
			catch
			{
			}

			return ret;
		}

		public void AddTile(int x, int y, LAYERS layer, Tile t)
		{
			isSaved = false;

			if (layers[(int)layer].ContainsKey(x + ":" + y))
				layers[(int)layer][x + ":" + y] = t;
			else
				layers[(int)layer].Add(x + ":" + y, t);
		}

		public void AddTile(Microsoft.Xna.Framework.Vector2 location, LAYERS layer, Tile t)
		{
			isSaved = false;

			if (layers[(int)layer].ContainsKey((int)location.X + ":" + (int)location.Y))
				layers[(int)layer][(int)location.X + ":" + (int)location.Y] = t;
			else
				layers[(int)layer].Add((int)location.X + ":" + (int)location.Y, t);
		}

		public bool EraseTile(int x, int y, LAYERS layer)
		{
			switch (layer)
			{
				case LAYERS.Graphic:
					(GetTile(x, y, LAYERS.Graphic) as GraphicTile).Graphic = 0;
					break;
				case LAYERS.Special:
					(GetTile(x, y, LAYERS.Special) as SpecialTile).Type = SpecialTileSpec.NONE;
					break;
				case LAYERS.NPC:
					throw new NotImplementedException();
				case LAYERS.Interactive:
					throw new NotImplementedException();
			}

			isSaved = false;

			return true;
		}

		public void ClearLayer(LAYERS layer)
		{
			int i = 0;
			do
			{
				Tile tile = layers[(int)layer].Values[i++];
				EraseTile(tile.X, tile.Y, layer);
			} while (i < layers[(int)layer].Values.Count);

			isSaved = false;
		}

		public void Resize(int new_w, int new_h)
		{
			if (new_w == w && new_h == h)
				return;
			if (new_w < w || new_h < h) //only resize containers if smaller
			{
				foreach (SortedList<string, Tile> layer in layers)
				{
					for (int i = 0; i < h; ++i)
					{
						for (int j = 0; j < w; ++j)
						{
							if (i < new_h && j == 0) //only set to new_w on first iteration
								j = new_w;

							if (layer.ContainsKey(i + ":" + j))
								layer.Remove(i + ":" + j);
						}
					}
				}
			}
			else
			{
				foreach (SortedList<string, Tile> layer in layers)
					layer.Capacity = new_w * new_h;

				for (int i = 0; i < new_w; i++)
				{
					for (int j = 0; j < new_h; j++)
					{
						if (i >= w || j >= h)
							AddTile(i, j, LAYERS.Graphic, new GraphicTile(i, j));
					}
				}
			}

			w = new_w;
			h = new_h;
			isSaved = false;
		}

		public void Rename(string newName)
		{
			mapname = newName;
			isSaved = false;
		}

		public void SetSpawnData(List<Spawn> spawns)
		{
			this.spawns = new List<Spawn>(spawns);
			isSaved = false;
		}
	}

	public class MapBuffer
	{
		private List<Map> buffer;
		private int pointer;

		public bool UndoReady
		{
			get { return pointer > 0; }
		}

		public bool RedoReady
		{
			get { return buffer.Count > pointer + 1; }
		}

		public bool MostRecent
		{
			get { return pointer == buffer.Count; }
		}

		public int Count
		{
			get { return buffer.Count; }
		}

		public MapBuffer()
		{
			buffer = new List<Map>();
			pointer = -1;
		}

		public void PushCurrentState(Map map)
		{
			buffer.Add(map);
		}

		public void AddState(Map map)
		{
			if (buffer.Count > pointer)
			{
				buffer.RemoveRange(pointer, buffer.Count - pointer);
			}

			buffer.Add(new Map(map));
			++pointer;
		}

		public Map GetUndo()
		{
			if (pointer == 0)
				return null;
			--pointer;
			return new Map(buffer[pointer]);
		}

		public Map GetRedo()
		{
			++pointer;
			return new Map(buffer[pointer]);
		}

		public void Clear()
		{
			buffer.Clear();
			pointer = 0;
		}

		public void UpdateFilePath(string path)
		{
			foreach (Map map in buffer)
			{
				map.FileName = path;
			}
		}
	}
}
