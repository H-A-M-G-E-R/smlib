using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Crocomire
{
    [Serializable()]
    class MDB
    {
        public ushort RoomAddress { get; set; }
        public string RoomId { get; set; }
        public byte XPos { get; set; }
        public byte YPos { get; set; }
        public byte Width { get; set; }
        public byte Height { get; set; }
        public ushort DoorOut { get; set; }
        public byte Region { get; set; }
        public byte Unknown1 { get; set; }
        public byte Unknown2 { get; set; }
        public byte Unknown3 { get; set; }
        public byte Unknown4 { get; set; }
        public List<RoomState> RoomState { get; set; }
        public List<DDB> DDB { get; set; }
        public string Name { get; set; }

        public int StateSelectSize
        {
            get
            {
                int size = 0;
                foreach(var roomState in RoomState.Where(r => r.TestCode != 0xE5E6))
                {
                    size += 4;
                    if (roomState.TestCode == 0xE612 || roomState.TestCode == 0xE629)
                    {
                        size++;
                    }
                    else if (roomState.TestCode == 0xE5EB)
                    {
                        size += 2;
                    }
                }
                return size + 4;
            }
        }

        public MDB()
        {
            RoomState = new List<RoomState>();
            DDB = new List<DDB>();
        }

        public void Save(string path)
        {
            if (Name == null || Name == "")
                Name = Guid.NewGuid().ToString();

            var stream = File.Open(path + "\\" + Name + ".room", FileMode.Create);
            BinaryFormatter bin = new BinaryFormatter();
            bin.Serialize(stream, this);
            stream.Close();
        }

        public static MDB Load(string fileName)
        {
            var stream = File.Open(fileName, FileMode.Open);
            BinaryFormatter bin = new BinaryFormatter();
            var mdb = (MDB)bin.Deserialize(stream);
            stream.Close();
            return mdb;            
        }

    }

    [Serializable()]
    class StateSelect
    {
        public ushort RoomState { get; set; }

    }

    [Serializable()]
    class RoomState
    {
        public ushort TestCode { get; set; }
        public byte TestValue { get; set; }
        public ushort TestValueDoor { get; set; }
        public ushort Pointer { get; set; }
        public uint RoomData { get; set; }
        public byte GraphicsSet { get; set; }
        public ushort Music { get; set; }
        public ushort FX1 { get; set; }
        public ushort EnemyPop { get; set; }
        public ushort EnemySet { get; set; }
        public ushort Layer2ScrollData { get; set; }
        public ushort Scroll { get; set; }
        public ushort Unused { get; set; }
        public ushort FX2 { get; set; }
        public ushort PLM { get; set; }
        public ushort BGDataPtr { get; set; }
        public ushort LayerHandling { get; set; }
        public byte[] LayerHandlingCode { get; set; }
        public LevelData LevelData { get; set; }

        public byte[,] ScrollData { get; set; }
        public List<BG> BGData { get; set; }

        public List<ScrollMod> ScrollMod { get; set; }
        public List<PLM> PLMList { get; set; }
        
        public FX1 FX1Data { get; set; }

        public List<EnemyPop> EnemyPopList { get; set; }
        public List<EnemySet> EnemySetList { get; set; }
        public byte EnemiesToKill { get; set; }
        public RoomState()
        {
            ScrollMod = new List<ScrollMod>();
            PLMList = new List<PLM>();
            EnemyPopList = new List<EnemyPop>();
            EnemySetList = new List<EnemySet>();
            BGData = new List<BG>();
            EnemiesToKill = 0;
            ScrollData = null;
        }
    }
    
    [Serializable()]
    class ScrollMod
    {
        public ushort Pointer { get; set; }
        public byte[] Data { get; set; }
    }

    [Serializable()]
    class BG
    {
        public ushort Header {get; set; }
        public uint Pointer { get; set; }
        public byte[] Unknown { get; set; }
        public byte[] Data { get; set; }
        public int Size { get { return 5 + Unknown.Length; } }
    }

    [Serializable()]
    class PLM
    {
        public ushort Command { get; set; }
        public byte X { get; set; }
        public byte Y { get; set; }
        public ushort Args { get; set; }
    }

    [Serializable()]
    class DDB
    {
        public ushort Pointer { get; set; }
        public ushort RoomId { get; set; }
        public byte Bitflags { get; set; }
        public byte Index { get; set; }
        public byte CloseX { get; set; }
        public byte CloseY { get; set; }
        public byte X { get; set; }
        public byte Y { get; set; }
        public ushort Distance { get; set; }
        public ushort Code { get; set; }
        public byte[] DoorASM { get; set; }
    }

    [Serializable()]
    class EnemyPop
    {
        public ushort EnemyData { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public ushort InitialGFX { get; set; }
        public ushort Prop1 { get; set; }
        public ushort Prop2 { get; set; }
        public ushort RoomArg1 { get; set; }
        public ushort RoomArg2 { get; set; }
    }

    [Serializable()]
    class EnemySet
    {
        public ushort EnemyUsed { get; set; }
        public ushort Palette { get; set; }
    }

    [Serializable()]
    class FX1
    {
        public ushort Select { get; set; }
        public ushort SurfaceStart { get; set; }
        public ushort SurfaceNew { get; set; }
        public ushort SurfaceSpeed { get; set; }
        public byte SurfaceDelay { get; set; }
        public byte Layer3Type { get; set; }
        public byte A { get; set; }
        public byte B { get; set; }
        public byte C { get; set; }
        public byte PaletteFX { get; set; }
        public byte AnimateTile { get; set; }
        public byte Blend { get; set; }
    }

    [Serializable()]
    class LevelData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public ushort Size { get; set; }
        public Block[,] Layer1 { get; set; }
        public Block[,] Layer2 { get; set; }
        public byte[] RawData
        {
            get
            {
                int rawDataSize = 2 + Size + (Size / 2) + (Layer2 != null && Layer2.Length > 0 ? Layer2.Length * 2 : 0);
                int w = Width;
                int h = Height;

                byte[] data = new byte[rawDataSize];
                data[0] = (byte)(Size & 0xFF);
                data[1] = (byte)((Size >> 8) & 0xFF);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int o = ((w * y) + x);
                        int t = (o * 2) + 2;
                        int b = (Size + 2) + o;

                        Layer1[x, y].Tile = (ushort)((Layer1[x, y].Clip << 12) + (Layer1[x, y].Tile & 0x0FFF));

                        data[t] = (byte)(Layer1[x, y].Tile & 0xFF);
                        data[t + 1] = (byte)((Layer1[x, y].Tile >> 8) & 0xFF);
                        data[b] = Layer1[x, y].BTS;

                        if (Layer2 != null && Layer2.Length > 0)
                        {
                            int l2 = (Size + 2 + (Size / 2) + (o * 2));
                            data[l2] = (byte)(Layer2[x, y].Tile & 0xFF);
                            data[l2 + 1] = (byte)((Layer2[x, y].Tile >> 8) & 0xFF);
                        }
                    }
                }
                return data;
            }
        }
        public List<Door> Doors { get; set; }

        [Serializable()]
        public struct Block
        {
            public ushort Tile;
            public byte BTS;
            public byte Clip;
        }

        [Serializable()]
        public struct Door
        {
            public Block Block;
            public int x;
            public int y;
        }

        public LevelData()
        {
            Layer1 = new Block[1, 1];
            Doors = new List<Door>();
            Size = 0;
        }

        public LevelData(byte[] data, int width, int height)
        {
            int w = (width * 16);
            int h = (height * 16);

            Height = h;
            Width = w;
            Layer1 = new Block[w, h];
            Doors = new List<Door>();

           // if (data.Length < 100)
           //     return;

            Size = (ushort)((data[1] << 8) + data[0]);

            for(int y = 0; y < h; y++)
            {
                for(int x = 0; x < w; x++)
                {
                    int o = ((w * y) + x);
                    int t = (o * 2) + 2;
                    int b = (Size + 2) + o;

                    Layer1[x, y].Tile = (ushort)((data[t + 1] << 8) + data[t]);
                    Layer1[x, y].BTS = data[b];
                    Layer1[x, y].Clip = (byte)((Layer1[x, y].Tile >> 12));
                    if (data.Length > (Size + 2 + (Size / 2)))
                    {
                        if(Layer2 == null)
                            Layer2 = new Block[w, h];

                        int l2 = (Size + 2 + (Size/2) + (o*2));
                        if (l2 + 1 > (data.Length - 1))
                        {
                            Layer2[x, y].Tile = (ushort)(data[l2]);
                        }
                        else
                        {
                            Layer2[x, y].Tile = (ushort)((data[l2 + 1] << 8) + data[l2]);
                        }
                    }

                    if(Layer1[x, y].Clip == 0x09)
                    {
                        Doors.Add(new Door() { Block = Layer1[x, y], x = x, y = y });
                    }
                }
            }
        }

    }

}
