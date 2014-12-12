using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Crocomire
{
    public partial class Main : Form
    {
        ROMHandler handler = new ROMHandler("D:\\sm.smc");
        public Main()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            handler = new ROMHandler("D:\\sm.smc");
            handler.Read();            
            listBox1.Items.Clear();
            listBox1.Items.AddRange(handler.MDBList.Select(x => String.Format("{0} -> w: {1}, h: {2}, doorout: {3:X}, roomstates: {4}, doors: {5}, plms: {6}, s: {7}", new object[] { x.RoomId, x.Width, x.Height, x.DoorOut, x.RoomState.Count, x.DDB.Count, x.RoomState[0].PLMList.Count, x.RoomState[0].LevelData.Size })).ToArray());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            handler.Write();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            listBox1.Items.Add("Opening ROM");
            handler.Read();
            int i = 0;
            foreach(var room in handler.MDBList)
            {
                foreach(var state in room.RoomState)
                {
                    i += state.PLMList.Count;
                    state.PLMList.Clear();
                }
            }
            listBox1.Items.Add(i.ToString() + " PLMs removed");
            handler.Write();
            listBox1.Items.Add("Closed ROM");
        }

        private void listBox1_Click(object sender, EventArgs e)
        {
            string roomStr = (string)listBox1.SelectedItem;
            string roomId = roomStr.Substring(0, 5);

            var room = handler.MDBList.Where(r => r.RoomId == roomId).First();

            Graphics g = pictureBox1.CreateGraphics();
            g.Clear(Color.White);
            for(int y = 0; y < room.RoomState[0].LevelData.Height; y++)
                for(int x = 0; x < room.RoomState[0].LevelData.Width; x++)
                {
                    var clip = room.RoomState[0].LevelData.Layer1[x, y].Clip;
                    var bts = room.RoomState[0].LevelData.Layer1[x, y].BTS;
                    switch(clip)
                    {
                        case 0x08:
                            g.FillRectangle(Brushes.Black, x*3, y*3, 3, 3);
                            break;
                        case 0x01:
                            g.DrawRectangle(Pens.Green, x * 3, y * 3, 3, 3);
                            break;
                        case 0x09:
                            g.DrawRectangle(Pens.Blue, x * 3, y * 3, 3, 3);
                            break;
                        case 0x00:
                            break;
                        default:
                            g.DrawRectangle(Pens.Red, x*3, y*3, 3, 3);
                            break;                        
                    }
                }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string roomStr = (string)listBox1.SelectedItem;
            string roomId = roomStr.Substring(0, 5);
            listBox1.Items.Clear();
            listBox1.Items.Add("Opening Z-Factor");
            var zFactor = new ROMHandler("D:\\zf.smc");
            zFactor.Read();


            var zfParlor = zFactor.MDBList.Where(r => r.RoomId == roomId).First();

            listBox1.Items.Add("Opening SM");
            var sm = new ROMHandler("D:\\sm.smc");
            sm.Read();

            listBox1.Items.Add("Free memory: " + sm.Mem.FreeMemory.Sum(x => x.Value.Sum(y => y.Size)).ToString());


            listBox1.Items.Add("Repointing/Relocating Room");
            /* get memory for MDB header, state select and default state */
            zfParlor.RoomAddress = sm.Mem.Allocate(0x8F, 11 + zfParlor.StateSelectSize + 26);
            zfParlor.DoorOut = sm.Mem.Allocate(0x8F, (4 * zfParlor.DDB.Count) + 4);

            foreach (var door in zfParlor.DDB)
            {
                door.Pointer = sm.Mem.Allocate(0x83, 12);
                door.RoomId = 0x92FD;
                //door.Code = 0x0000;
                if (door.DoorASM != null && door.DoorASM.Length > 0)
                    door.Code = sm.Mem.Allocate(0x8F, door.DoorASM.Length);
            }

            foreach (var roomState in zfParlor.RoomState)
            {
                if (roomState.Pointer != 0xE5E6)
                    roomState.Pointer = sm.Mem.Allocate(0x8F, 26);

                /* create new blank scrolling data for testing purposes */
                //if (roomState.Scroll > 0x8000)
                //{
                //    for (int y = 0; y < zfParlor.Height; y++)
                //    {
                //        for (int x = 0; x < zfParlor.Width; x++)
                //        {
                //            roomState.ScrollData[x, y] = 2;
                //        }
                //    }
                //}

                roomState.EnemyPopList.Clear();
                roomState.EnemySetList.Clear();
                roomState.PLMList.Clear();

                /* clear this out for now, we'll use it later to allocate correctly */
                roomState.RoomData = 0;
                roomState.EnemyPop = sm.Mem.Allocate(0xA1, (18 * roomState.EnemyPopList.Count) + 3);
                roomState.EnemySet = sm.Mem.Allocate(0xB4, (6 * roomState.EnemySetList.Count) + 4);
                roomState.FX1 = sm.Mem.Allocate(0x83, 16);
                roomState.FX2 = 0x0000;
                roomState.BGDataPtr = sm.Mem.Allocate(0x8F, roomState.BGData.Sum(bgd => bgd.Size) + 2);
                //roomState.LayerHandling = 0;

                
                if(roomState.LayerHandlingCode != null && roomState.LayerHandlingCode.Length > 0)
                    roomState.LayerHandling = sm.Mem.Allocate(0x8F, roomState.LayerHandlingCode.Length);
                
                
                foreach(var bg in roomState.BGData)
                {
                    /* compress data to get compressed size + 0x20 (for variance) */
                    var compressed = Lunar.Compress(bg.Data);
                    bg.Pointer = sm.Mem.Allocate(compressed.Length + 0x20);
                }

                
                if (roomState.Scroll > 0x0000)
                {
                    roomState.Scroll = sm.Mem.Allocate(0x8F, roomState.ScrollData.Length + roomState.ScrollMod.Sum(x => x.Length) + 4);
                }

                roomState.PLM = sm.Mem.Allocate(0x8F, (6 * roomState.PLMList.Count) + 4);
            }

            foreach (var roomState in zfParlor.RoomState)
            {
                if (roomState.RoomData == 0)
                {
                    var compressed = Lunar.Compress(roomState.LevelData.RawData);
                    roomState.RoomData = sm.Mem.Allocate(compressed.Length + 0x20);
                }
            }

            sm.MDBList.Add(zfParlor);

            /* repoint doors to lead to new room */
            var parlor = sm.MDBList.Where(r => r.RoomId == "792FD").First();
            foreach (var ddb in parlor.DDB)
            {
                if (ddb.RoomId == 0x990D)
                {
                    ddb.RoomId = zfParlor.RoomAddress;
                    ddb.X = Convert.ToByte(txtDoorX.Text);
                    ddb.Y = Convert.ToByte(txtDoorY.Text);
                    ddb.Code = 0x0000;
                }
            }
            listBox1.Items.Add("Free memory: " + sm.Mem.FreeMemory.Sum(x => x.Value.Sum(y => y.Size)).ToString());
            listBox1.Items.Add("Saving new ROM");
            sm.Write();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            handler = new ROMHandler("D:\\zf.smc");
            handler.Read();
            listBox1.Items.Clear();
            listBox1.Items.AddRange(handler.MDBList.Select(x => String.Format("{0} -> w: {1}, h: {2}, doorout: {3:X}, roomstates: {4}, doors: {5}, plms: {6}, s: {7}", new object[] { x.RoomId, x.Width, x.Height, x.DoorOut, x.RoomState.Count, x.DDB.Count, x.RoomState[0].PLMList.Count, x.RoomState[0].LevelData.Size })).ToArray());

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
