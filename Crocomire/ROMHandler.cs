using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Crocomire
{
    class ROMHandler
    {
        private string _fileName;
        private int _plmBank = 0x8F;
        private int _scrollBank = 0x8F;
        private Disassembler _disAsm;
        private BinaryReader _bReader;
        private BinaryWriter _bWriter;
        public List<MDB> MDBList { get; set; }
        public MemoryManager Mem { get; set; }

        public ROMHandler(string fileName)
        {
            _fileName = fileName;
            _disAsm = new Disassembler();
            Mem = new MemoryManager();
        }

        public void Read()
        {
            _bReader = new BinaryReader(new FileStream(_fileName, FileMode.Open));
            ReadMDB();
            _bReader.Close();
        }

        public void Write()
        {
            Lunar.ExpandROM(_fileName, 32);
            _bWriter = new BinaryWriter(new FileStream(_fileName, FileMode.Open));
            cleanRom();
            WriteMDB();
            _bWriter.Close();
        }

        /* writes 0xFF to all segments of free space */
        private void cleanRom()
        {
            foreach(var bank in Mem.FreeMemory)
            {
                foreach(var segment in bank.Value)
                {
                    _bWriter.Seek((int)Lunar.ToPC((uint)(bank.Key << 16) + segment.Start), SeekOrigin.Begin);
                    for (int i = 0; i < segment.Size; i++)
                        _bWriter.Write((byte)0xFF);
                }
            }
        }


        public void RemoveRoom(MDB room)
        {
            Mem.Free(0x8F, room.RoomAddress, 11 + room.StateSelectSize + 26);
            Mem.Free(0x8F, room.DoorOut, (2 * room.DDB.Count));

            foreach (var door in room.DDB)
            {
                //Mem.Free(0x83, door.Pointer, 12);
                //if (door.DoorASM != null && door.DoorASM.Length > 0)
                //    Mem.Free(0x8F, door.Code, door.DoorASM.Length);
            }

            foreach (var roomState in room.RoomState)
            {
                if (roomState.Pointer != 0xE5E6)
                    Mem.Free(0x8F, roomState.Pointer, 26);

                /* clear this out for now, we'll use it later to allocate correctly */
                if(roomState.EnemyPop > 0x8000)
                    Mem.Free(0xA1, roomState.EnemyPop, (18 * roomState.EnemyPopList.Count) + 3);
                
                if(roomState.EnemySet > 0x8000)
                    Mem.Free(0xB4, roomState.EnemySet, (6 * roomState.EnemySetList.Count) + 4);
                
                //if(roomState.FX1 > 0x8000)
                //    Mem.Free(0x83, roomState.FX1, 16);

                //if (roomState.BGDataPtr > 0x8000)
                //    Mem.Free(0x8F, roomState.BGDataPtr, roomState.BGData.Sum(bgd => bgd.Size) + 2);


                if (roomState.LayerHandlingCode != null && roomState.LayerHandlingCode.Length > 0)
                    Mem.Free(0x8F, roomState.LayerHandling, roomState.LayerHandlingCode.Length);

                /*
                foreach (var bg in roomState.BGData)
                {
                    var compressed = Lunar.Compress(bg.Data);
                    Mem.Free((int)(bg.Pointer >> 16), (ushort)(bg.Pointer & 0xFFFF), compressed.Length);
                }
                */

                if (roomState.Scroll > 0x8000)
                {
                    Mem.Free(0x8F, roomState.Scroll, roomState.ScrollData.Length + roomState.ScrollMod.Sum(x => x.Data.Length));
                }

                if (roomState.PLM > 0x8000)
                    Mem.Free(0x8F, roomState.PLM, (6 * roomState.PLMList.Count));
            }

            foreach (var roomState in room.RoomState)
            {
                var compressed = Lunar.Compress(roomState.LevelData.RawData);
                Mem.Free((int)(roomState.RoomData >> 16), (ushort)(roomState.RoomData & 0xFFFF), compressed.Length);
            }

            MDBList.Remove(room);
        }

        /* adds a new room to the MDBList with complete relocating */
        public void AddRoom(MDB room)
        {
            /* get memory for MDB header, state select and default state */
            room.RoomAddress = Mem.Allocate(0x8F, 11 + room.StateSelectSize + 26);
            room.RoomId = String.Format("7{0:X}", room.RoomAddress);
            room.DoorOut = Mem.Allocate(0x8F, (2 * room.DDB.Count));

            foreach (var door in room.DDB)
            {
                door.Pointer = Mem.Allocate(0x83, 12);
                //door.Code = 0x0000;
                if (door.DoorASM != null && door.DoorASM.Length > 0)
                    door.Code = Mem.Allocate(0x8F, door.DoorASM.Length);
            }

            foreach (var roomState in room.RoomState)
            {
                if (roomState.Pointer != 0xE5E6)
                    roomState.Pointer = Mem.Allocate(0x8F, 26);

                /* remove invalid enemies */
                foreach (var enemy in roomState.EnemyPopList.ToList())
                {
                    if (enemy.EnemyData > 0xF793)
                    {
                        roomState.EnemyPopList.Remove(enemy);
                        var enemySet = roomState.EnemySetList.Where(es => es.EnemyUsed == enemy.EnemyData).FirstOrDefault();
                        if (enemySet != null)
                        {
                            roomState.EnemySetList.Remove(enemySet);
                        }
                    }
                }

                /* clear this out for now, we'll use it later to allocate correctly */
                roomState.RoomData = 0;
                
                if(roomState.EnemyPop > 0x8000)
                    roomState.EnemyPop = Mem.Allocate(0xA1, (18 * roomState.EnemyPopList.Count) + 3);

                if(roomState.EnemySet > 0x8000)
                    roomState.EnemySet = Mem.Allocate(0xB4, (6 * roomState.EnemySetList.Count) + 4);
                
                if(roomState.FX1 > 0x8000)
                    roomState.FX1 = Mem.Allocate(0x83, 16);

                if(roomState.BGDataPtr > 0x8000)
                    roomState.BGDataPtr = Mem.Allocate(0x8F, roomState.BGData.Sum(bgd => bgd.Size) + 2);

                if (roomState.LayerHandlingCode != null && roomState.LayerHandlingCode.Length > 0)
                    roomState.LayerHandling = Mem.Allocate(0x8F, roomState.LayerHandlingCode.Length);


                foreach (var bg in roomState.BGData)
                {
                    /* compress data to get compressed size + 0x20 (for variance) */
                    var compressed = Lunar.Compress(bg.Data);
                    bg.Pointer = Mem.Allocate(compressed.Length + 0x20);
                }

                if (roomState.Scroll > 0x0001)
                {
                    roomState.Scroll = Mem.Allocate(0x8F, roomState.ScrollData.Length + roomState.ScrollMod.Sum(x => x.Data.Length) + 4);
                    ushort ptr = (ushort)(roomState.Scroll + roomState.ScrollData.Length);
                    foreach(var scrollMod in roomState.ScrollMod)
                    {
                        ushort oldPointer = scrollMod.Pointer;
                        scrollMod.Pointer = ptr;

                        /* update the plms that has this pointer */
                        var plms = roomState.PLMList.Where(p => p.Command == 0xB703 && p.Args == oldPointer);
                        foreach(var plm in plms)
                        {
                            plm.Args = scrollMod.Pointer;
                        }
                        ptr += (ushort)scrollMod.Data.Length;
                    }
                    
                }

                if(roomState.PLM > 0x0000)
                    roomState.PLM = Mem.Allocate(0x8F, (6 * roomState.PLMList.Count) + 4);
            }

            foreach (var roomState in room.RoomState)
            {
                if (roomState.RoomData == 0)
                {
                    var compressed = Lunar.Compress(roomState.LevelData.RawData);
                    roomState.RoomData = Mem.Allocate(compressed.Length + 0x20);
                }
            }

            MDBList.Add(room);

        }

        private void ReadMDB()
        {
            MDBList = new List<MDB>();
            byte[] data = new byte[0xFFFF];

            /* check where the PLM bank is */
            _bReader.BaseStream.Seek(0x204AC, SeekOrigin.Begin);
            _plmBank = (int)_bReader.ReadByte();                

            /* check for z-factor scroll bank (maybe other hacks later as well) */
            _bReader.BaseStream.Seek(0x007FC0, SeekOrigin.Begin);
            var romName = String.Join("", _bReader.ReadChars(0x15)).Trim();
            if(romName == "M3 Z-Factor Hack")
            {
                _scrollBank = 0xE0;
            }

            /* scan until we find a MDB header entry */
            for(int x = 0x8000; x < 0xFFFF; x++)
            {
                _bReader.BaseStream.Seek(0x070000 + x, SeekOrigin.Begin);
                data =_bReader.ReadBytes(11);
                if(data[7] == 0xA0 && data[8] < 0x05 && (data[6] == 0x70 || data[6] == 0x90 || data[6] == 0xA0))
                {
                    /* read the MDB header data */
                    var m = new MDB();
                    m.RoomId = String.Format("{0:X}", 0x070000 + x);
                    m.RoomAddress = (ushort)x;
                    m.Unknown1 = data[0];
                    m.Region = data[1];
                    m.XPos = data[2];
                    m.YPos = data[3];
                    m.Width = data[4];
                    m.Height = data[5];
                    m.Unknown2 = data[6];
                    m.Unknown3 = data[7];
                    m.Unknown4 = data[8];
                    _bReader.BaseStream.Seek(-2, SeekOrigin.Current);
                    m.DoorOut = _bReader.ReadUInt16();
                    
                    /* read MDB stateselect */

                    ushort roomStatePtr;
                    ushort testCode = _bReader.ReadUInt16();
                    while(testCode != 0xE5E6)
                    {
                        byte testValue = 0;
                        ushort testValueDoor = 0;

                        if (testCode == 0xE612 || testCode == 0xE629)
                            testValue = _bReader.ReadByte();
                        else if (testCode == 0xE5EB)
                            testValueDoor = _bReader.ReadUInt16();

                        roomStatePtr = _bReader.ReadUInt16();

                        var ros = new RoomState();
                        ros.TestCode = testCode;
                        ros.TestValue = testValue;
                        ros.TestValueDoor = testValueDoor;
                        ros.Pointer = roomStatePtr;

                        m.RoomState.Add(ros);

                        testCode = _bReader.ReadUInt16();
                    }

                    var ds = new RoomState();
                    ds.TestCode = 0xE5E6;
                    ds.TestValue = 0;
                    ds.Pointer = 0xE5E6;
                    data = _bReader.ReadBytes(3);
                    ds.RoomData = (uint)((data[2] << 16) + (data[1] << 8) + data[0]);
                    ds.GraphicsSet = _bReader.ReadByte();
                    ds.Music = _bReader.ReadUInt16();
                    ds.FX1 = _bReader.ReadUInt16();
                    ds.EnemyPop = _bReader.ReadUInt16();
                    ds.EnemySet = _bReader.ReadUInt16();
                    ds.Layer2ScrollData = _bReader.ReadUInt16();
                    ds.Scroll = _bReader.ReadUInt16();
                    ds.Unused = _bReader.ReadUInt16();
                    ds.FX2 = _bReader.ReadUInt16();
                    ds.PLM = _bReader.ReadUInt16();
                    ds.BGDataPtr = _bReader.ReadUInt16();
                    ds.LayerHandling = _bReader.ReadUInt16();

                    m.RoomState.Add(ds);

                    foreach(var rs in m.RoomState.Where(r => r.Pointer != 0xE5E6))
                    {
                        _bReader.BaseStream.Seek(0x070000 + rs.Pointer, SeekOrigin.Begin);
                        data = _bReader.ReadBytes(3);
                        rs.RoomData = (uint)((data[2] << 16) + (data[1] << 8) + data[0]);
                        rs.GraphicsSet = _bReader.ReadByte();
                        rs.Music = _bReader.ReadUInt16();
                        rs.FX1 = _bReader.ReadUInt16();
                        rs.EnemyPop = _bReader.ReadUInt16();
                        rs.EnemySet = _bReader.ReadUInt16();
                        rs.Layer2ScrollData = _bReader.ReadUInt16();
                        rs.Scroll = _bReader.ReadUInt16();
                        rs.Unused = _bReader.ReadUInt16();
                        rs.FX2 = _bReader.ReadUInt16();
                        rs.PLM = _bReader.ReadUInt16();
                        rs.BGDataPtr = _bReader.ReadUInt16();
                        rs.LayerHandling = _bReader.ReadUInt16();
                    }

                   
                    foreach(var roomState in m.RoomState)
                    {
                        if (roomState.Scroll > 0x0001 && roomState.Scroll != 0x8000)
                        {
                            /* read MDB Scroll data*/
                            _bReader.BaseStream.Seek(Lunar.ToPC((uint)(_scrollBank<<16) + roomState.Scroll), SeekOrigin.Begin);
                            roomState.ScrollData = new byte[m.Width, m.Height];
                            for (int y = 0; y < m.Height; y++)
                            {
                                for (int xx = 0; xx < m.Width; xx++)
                                {
                                    roomState.ScrollData[xx, y] = _bReader.ReadByte();
                                }
                            }
                        }
                        
                        /* read room PLMs */
                        if (roomState.PLM != 0x0000)
                        {
                            _bReader.BaseStream.Seek(Lunar.ToPC((uint)(_plmBank<<16) + roomState.PLM), SeekOrigin.Begin);
                            while (true)
                            {
                                ushort command = _bReader.ReadUInt16();
                                if (command == 0x0000)
                                    break;

                                var plm = new PLM();
                                plm.Command = command;
                                plm.X = _bReader.ReadByte();
                                plm.Y = _bReader.ReadByte();
                                plm.Args = _bReader.ReadUInt16();

                                roomState.PLMList.Add(plm);
                            }
                        }

                        /* read MDB Scroll modifications */
                        foreach(var plm in roomState.PLMList)
                        {
                            if(plm.Command == 0xB703)
                            {
                                _bReader.BaseStream.Seek(0x070000 + plm.Args, SeekOrigin.Begin);                                
                                bool ok = false;
                                byte[] tmp = new byte[100];
                                int i = 0;
                                while (true)
                                {
                                    byte screen = _bReader.ReadByte();
                                    if (screen == 0x80)
                                    {
                                        tmp[i] = 0x80;
                                        ok = true;
                                        break;
                                    }
                                    byte scroll = _bReader.ReadByte();
                                    if (scroll > 0x02)
                                    {
                                        ok = false;
                                        break;
                                    }
                                    tmp[i] = screen;
                                    tmp[i + 1] = scroll;
                                    i += 2;
                                }

                                if (ok)
                                {
                                    byte[] scrollModData = new byte[i + 1];
                                    var scrollMod = new ScrollMod();
                                    scrollMod.Pointer = plm.Args;                                    
                                    Array.Copy(tmp, scrollModData, i + 1);
                                    scrollMod.Data = scrollModData;
                                    roomState.ScrollMod.Add(scrollMod);
                                }
                            }
                        }

                        /* read enemy pop */
                        _bReader.BaseStream.Seek(Lunar.ToPC((uint)(0xA10000 + roomState.EnemyPop)), SeekOrigin.Begin);
                        while(true)
                        {
                            ushort pointer = _bReader.ReadUInt16();
                            if(pointer == 0xFFFF)
                            {
                                roomState.EnemiesToKill = _bReader.ReadByte();
                                break;
                            }

                            var enemyPop = new EnemyPop();
                            enemyPop.EnemyData = pointer;
                            enemyPop.X = _bReader.ReadUInt16();
                            enemyPop.Y = _bReader.ReadUInt16();
                            enemyPop.InitialGFX = _bReader.ReadUInt16();
                            enemyPop.Prop1 = _bReader.ReadUInt16();
                            enemyPop.Prop2 = _bReader.ReadUInt16();
                            enemyPop.RoomArg1 = _bReader.ReadUInt16();
                            enemyPop.RoomArg2 = _bReader.ReadUInt16();

                            roomState.EnemyPopList.Add(enemyPop);
                        }

                        /* read enemy set */
                        _bReader.BaseStream.Seek(Lunar.ToPC((uint)(0xB40000 + roomState.EnemySet)), SeekOrigin.Begin);
                        while(true)
                        {
                            ushort pointer = _bReader.ReadUInt16();
                            if (pointer == 0xFFFF)
                                break;

                            var enemySet = new EnemySet();
                            enemySet.EnemyUsed = pointer;
                            enemySet.Palette = _bReader.ReadUInt16();

                            roomState.EnemySetList.Add(enemySet);
                        }

                        /* read FX1 */
                        _bReader.BaseStream.Seek(Lunar.ToPC((uint)(0x830000 + roomState.FX1)), SeekOrigin.Begin);
                        bool retry = false;
                        while(true)
                        {
                            ushort select = _bReader.ReadUInt16();
                            if (select == 0xFFFF)
                                break;

                            if(select == 0x0000 || m.DDB.Select(d => d.Pointer).Contains(select))
                            {
                                var fx1 = new FX1();
                                fx1.Select = select;
                                fx1.SurfaceStart = _bReader.ReadUInt16();
                                fx1.SurfaceNew = _bReader.ReadUInt16();
                                fx1.SurfaceSpeed = _bReader.ReadUInt16();
                                fx1.SurfaceDelay = _bReader.ReadByte();
                                fx1.Layer3Type = _bReader.ReadByte();
                                fx1.A = _bReader.ReadByte();
                                fx1.B = _bReader.ReadByte();
                                fx1.C = _bReader.ReadByte();
                                fx1.PaletteFX = _bReader.ReadByte();
                                fx1.AnimateTile = _bReader.ReadByte();
                                fx1.Blend = _bReader.ReadByte();
                                roomState.FX1Data = fx1;
                                break;
                            }
                            else
                            {
                                if (retry == false)
                                {
                                    retry = true;
                                    _bReader.BaseStream.Seek(Lunar.ToPC((uint)(0x830000 + roomState.FX1 + 0x10)), SeekOrigin.Begin);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        if(roomState.BGDataPtr > 0x8000)
                        {
                            _bReader.BaseStream.Seek(Lunar.ToPC((uint)(0x8F0000 + roomState.BGDataPtr)), SeekOrigin.Begin);

                            while(true)
                            {
                                /* read header */
                                ushort header = _bReader.ReadUInt16();
                                if (header != 0x04)
                                    break;

                                var bg = new BG();
                                bg.Header = header;

                                /* regular bg data */
                                var ptr = _bReader.ReadBytes(3);
                                bg.Pointer = (uint)((ptr[2] << 16) + (ptr[1] << 8) + ptr[0]);
                                bg.Unknown = _bReader.ReadBytes(0x14);
                                roomState.BGData.Add(bg);
                            }

                            /* loop through BGData and read in graphics */
                            foreach (var bg in roomState.BGData)
                            {
                                _bReader.BaseStream.Seek(Lunar.ToPC(bg.Pointer), SeekOrigin.Begin);
                                byte[] decompressed = Lunar.Decompress(_bReader.ReadBytes(0x10000));
                                bg.Data = decompressed;
                            }
                        }

                        /* layer 1_2 handling code */
                        if(roomState.LayerHandling > 0x8000)
                        {
                            _bReader.BaseStream.Seek(Lunar.ToPC((uint)(0x8F0000 + roomState.LayerHandling)), SeekOrigin.Begin);
                            byte[] tmp = new byte[0x1000];
                            tmp = _bReader.ReadBytes(0x1000);
                            var code = _disAsm.Disassemble(tmp);
                            roomState.LayerHandlingCode = code;
                        }

                        try
                        {
                            /* see if we can get a reference to the level data from eariler read roomState data */
                            var copyRoomState = m.RoomState.Where(r => r.RoomData == roomState.RoomData && r.LevelData != null).FirstOrDefault();
                            if (copyRoomState != null)
                            {
                                roomState.LevelData = copyRoomState.LevelData;
                            } 
                            else
                            {
                                _bReader.BaseStream.Seek(Lunar.ToPC(roomState.RoomData), SeekOrigin.Begin);
                                byte[] decompressed = Lunar.Decompress(_bReader.ReadBytes(0x10000));
                                roomState.LevelData = new LevelData(decompressed, m.Width, m.Height);
                            }
                        }
                        catch
                        {
                            roomState.LevelData = new LevelData();
                        }
                    }

                    /* read MDB DoorOut */
                    _bReader.BaseStream.Seek(0x070000 + m.DoorOut, SeekOrigin.Begin);
                    int doors = (m.RoomState[0].LevelData.Doors.Count > 0 ? m.RoomState[0].LevelData.Doors.Max(d => d.Block.BTS) + 1 : 0);
                    for (int i = 0; i < doors; i++)
                    {
                        ushort pointer = _bReader.ReadUInt16();
                        var ddb = new DDB();
                        ddb.Pointer = pointer;
                        m.DDB.Add(ddb);
                    }

                    /* read DDB Doorout */
                    foreach (var ddb in m.DDB)
                    {
                        _bReader.BaseStream.Seek(Lunar.ToPC((uint)(0x830000 + ddb.Pointer)), SeekOrigin.Begin);
                        ddb.RoomId = _bReader.ReadUInt16();
                        ddb.Bitflags = _bReader.ReadByte();
                        ddb.Index = _bReader.ReadByte();
                        ddb.CloseX = _bReader.ReadByte();
                        ddb.CloseY = _bReader.ReadByte();
                        ddb.X = _bReader.ReadByte();
                        ddb.Y = _bReader.ReadByte();
                        ddb.Distance = _bReader.ReadUInt16();
                        ddb.Code = _bReader.ReadUInt16();

                        /* read door ASM */
                        if (ddb.Code > 0x8000)
                        {
                            _bReader.BaseStream.Seek(Lunar.ToPC((uint)(0x8F0000 + ddb.Code)), SeekOrigin.Begin);
                            byte[] tmp = new byte[0x1000];
                            tmp = _bReader.ReadBytes(0x1000);
                            var code = _disAsm.Disassemble(tmp);
                            ddb.DoorASM = code;
                        }
                    }

                    MDBList.Add(m);
                }
            }
        }

        public void WriteMDB()
        {
            foreach(var room in MDBList)
            {
                /* Write MDB Header */
                _bWriter.Seek(0x070000 + room.RoomAddress, SeekOrigin.Begin);
                _bWriter.Write(room.Unknown1);
                _bWriter.Write(room.Region);
                _bWriter.Write(room.XPos);
                _bWriter.Write(room.YPos);
                _bWriter.Write(room.Width);
                _bWriter.Write(room.Height);
                _bWriter.Write(room.Unknown2);
                _bWriter.Write(room.Unknown3);
                _bWriter.Write(room.Unknown4);
                _bWriter.Write(room.DoorOut);

                foreach (var roomState in room.RoomState.Where(r => r.TestCode != 0xE5E6))
                {
                    _bWriter.Write(roomState.TestCode);
                    if (roomState.TestCode == 0xE612 || roomState.TestCode == 0xE629)
                    {
                        _bWriter.Write(roomState.TestValue);
                    }
                    else if(roomState.TestCode == 0xE5EB)
                    {
                        _bWriter.Write(roomState.TestValueDoor);
                    }

                    _bWriter.Write(roomState.Pointer);
                }
                _bWriter.Write((ushort)0xE5E6);
                
                /* write default state */
                var ds = room.RoomState.Where(r => r.TestCode == 0xE5E6).First();
                _bWriter.Write((byte)(ds.RoomData & 0xFF));
                _bWriter.Write((byte)((ds.RoomData >> 8) & 0xFF));
                _bWriter.Write((byte)((ds.RoomData >> 16) & 0xFF));

                _bWriter.Write(ds.GraphicsSet);
                _bWriter.Write(ds.Music);
                _bWriter.Write(ds.FX1);
                _bWriter.Write(ds.EnemyPop);
                _bWriter.Write(ds.EnemySet);
                _bWriter.Write(ds.Layer2ScrollData);
                _bWriter.Write(ds.Scroll);
                _bWriter.Write(ds.Unused);
                _bWriter.Write(ds.FX2);
                _bWriter.Write(ds.PLM);
                _bWriter.Write(ds.BGDataPtr);
                _bWriter.Write(ds.LayerHandling);


                /* write roomstates */
                foreach (var roomState in room.RoomState.Where(r => r.TestCode != 0xE5E6))
                {
                    _bWriter.BaseStream.Seek(0x070000 + roomState.Pointer, SeekOrigin.Begin);
                    _bWriter.Write((byte)(roomState.RoomData & 0xFF));
                    _bWriter.Write((byte)((roomState.RoomData >> 8) & 0xFF));
                    _bWriter.Write((byte)((roomState.RoomData >> 16) & 0xFF));

                    _bWriter.Write(roomState.GraphicsSet);
                    _bWriter.Write(roomState.Music);
                    _bWriter.Write(roomState.FX1);
                    _bWriter.Write(roomState.EnemyPop);
                    _bWriter.Write(roomState.EnemySet);
                    _bWriter.Write(roomState.Layer2ScrollData);
                    _bWriter.Write(roomState.Scroll);
                    _bWriter.Write(roomState.Unused);
                    _bWriter.Write(roomState.FX2);
                    _bWriter.Write(roomState.PLM);
                    _bWriter.Write(roomState.BGDataPtr);
                    _bWriter.Write(roomState.LayerHandling);
                }

                foreach (var roomState in room.RoomState)
                {
                    if (roomState.Scroll > 0x0001 && roomState.Scroll != 0x8000)
                    {
                        /* write scroll data */
                        _bWriter.Seek((int)Lunar.ToPC((uint)(_scrollBank << 16) + roomState.Scroll), SeekOrigin.Begin);
                        for (int y = 0; y < room.Height; y++)
                        {
                            for (int x = 0; x < room.Width; x++)
                            {
                                _bWriter.Write(roomState.ScrollData[x, y]);
                            }
                        }

                        foreach (var scrollMod in roomState.ScrollMod)
                        {
                            _bWriter.Seek(0x070000 + scrollMod.Pointer, SeekOrigin.Begin);
                            _bWriter.Write(scrollMod.Data);
                        }
                    }

                    if (roomState.PLM != 0x0000)
                    {
                        /* write PLMs */
                        _bWriter.Seek((int)Lunar.ToPC((uint)(_plmBank << 16) + roomState.PLM), SeekOrigin.Begin);
                        foreach (var plm in roomState.PLMList)
                        {
                            _bWriter.Write(plm.Command);
                            _bWriter.Write(plm.X);
                            _bWriter.Write(plm.Y);
                            _bWriter.Write(plm.Args);
                        }
                        _bWriter.Write((ushort)0);
                    }

                    /* write FX1 */
                    if (roomState.FX1Data != null)
                    {
                        _bWriter.Seek((int)Lunar.ToPC((uint)0x830000 + roomState.FX1), SeekOrigin.Begin);
                        _bWriter.Write(roomState.FX1Data.Select);
                        _bWriter.Write(roomState.FX1Data.SurfaceStart);
                        _bWriter.Write(roomState.FX1Data.SurfaceNew);
                        _bWriter.Write(roomState.FX1Data.SurfaceSpeed);
                        _bWriter.Write(roomState.FX1Data.SurfaceDelay);
                        _bWriter.Write(roomState.FX1Data.Layer3Type);
                        _bWriter.Write(roomState.FX1Data.A);
                        _bWriter.Write(roomState.FX1Data.B);
                        _bWriter.Write(roomState.FX1Data.C);
                        _bWriter.Write(roomState.FX1Data.PaletteFX);
                        _bWriter.Write(roomState.FX1Data.AnimateTile);
                        _bWriter.Write(roomState.FX1Data.Blend);
                    }

                    /* write enemy pop */
                    if (roomState.EnemyPop != 0x0000)
                    {
                        _bWriter.Seek((int)Lunar.ToPC((uint)0xA10000 + roomState.EnemyPop), SeekOrigin.Begin);
                        foreach (var enemyPop in roomState.EnemyPopList)
                        {
                            _bWriter.Write(enemyPop.EnemyData);
                            _bWriter.Write(enemyPop.X);
                            _bWriter.Write(enemyPop.Y);
                            _bWriter.Write(enemyPop.InitialGFX);
                            _bWriter.Write(enemyPop.Prop1);
                            _bWriter.Write(enemyPop.Prop2);
                            _bWriter.Write(enemyPop.RoomArg1);
                            _bWriter.Write(enemyPop.RoomArg2);
                        }

                        _bWriter.Write((ushort)0xFFFF);
                        _bWriter.Write((byte)roomState.EnemiesToKill);
                    }

                    /* write enemy set */
                    if (roomState.EnemySet != 0x0000)
                    {
                        _bWriter.Seek((int)Lunar.ToPC((uint)0xB40000 + roomState.EnemySet), SeekOrigin.Begin);
                        foreach (var enemySet in roomState.EnemySetList)
                        {
                            _bWriter.Write(enemySet.EnemyUsed);
                            _bWriter.Write(enemySet.Palette);
                        }

                        _bWriter.Write((ushort)0xFFFF);
                    }

                    /* write bg data */
                    if(roomState.BGDataPtr > 0x8000 && roomState.BGData.Count > 0)
                    {
                        _bWriter.Seek((int)Lunar.ToPC((uint)0x8F0000 + roomState.BGDataPtr), SeekOrigin.Begin);
                        foreach (var bg in roomState.BGData)
                        {
                            _bWriter.Write(bg.Header);
                            _bWriter.Write((byte)(bg.Pointer & 0xFF));
                            _bWriter.Write((byte)((bg.Pointer >> 8) & 0xFF));
                            _bWriter.Write((byte)((bg.Pointer >> 16) & 0xFF));
                            _bWriter.Write(bg.Unknown);
                        }

                        _bWriter.Write((ushort)0x0000);

                        foreach (var bg in roomState.BGData)
                        {
                            _bWriter.Seek((int)Lunar.ToPC(bg.Pointer), SeekOrigin.Begin);
                            byte[] compressedData = Lunar.Compress(bg.Data);
                            _bWriter.Write(compressedData);
                        }
                    }

                    /* layer 1_2 handling code */
                    if (roomState.LayerHandling > 0x8000)
                    {
                        _bWriter.BaseStream.Seek(Lunar.ToPC((uint)(0x8F0000 + roomState.LayerHandling)), SeekOrigin.Begin);
                        _bWriter.Write(roomState.LayerHandlingCode);
                    }

                    /* write level data */
                    if (roomState.LevelData.Size > 0)
                    {
                        byte[] compressedData = Lunar.Compress(roomState.LevelData.RawData);
                        _bWriter.Seek((int)Lunar.ToPC(roomState.RoomData), SeekOrigin.Begin);
                        _bWriter.Write(compressedData);
                    }
                }

                /* Write doorout block */
                _bWriter.Seek(0x070000 + room.DoorOut, SeekOrigin.Begin);
                foreach (var ddb in room.DDB)
                {
                    _bWriter.Write(ddb.Pointer);
                }

                /* write DDB */
                foreach (var ddb in room.DDB)
                {
                    _bWriter.Seek((int)Lunar.ToPC((uint)0x830000 + ddb.Pointer), SeekOrigin.Begin);
                    _bWriter.Write(ddb.RoomId);
                    _bWriter.Write(ddb.Bitflags);
                    _bWriter.Write(ddb.Index);
                    _bWriter.Write(ddb.CloseX);
                    _bWriter.Write(ddb.CloseY);
                    _bWriter.Write(ddb.X);
                    _bWriter.Write(ddb.Y);
                    _bWriter.Write(ddb.Distance);
                    _bWriter.Write(ddb.Code);

                    if(ddb.DoorASM != null && ddb.Code > 0x8000)
                    {
                        _bWriter.Seek((int)Lunar.ToPC((uint)0x8F0000 + ddb.Code), SeekOrigin.Begin);
                        _bWriter.Write(ddb.DoorASM);
                    }
                }


            }
        }
    }
}
