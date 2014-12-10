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

        private void button4_Click(object sender, EventArgs e)
        {
            handler.Read();
            var parlor = handler.MDBList.Where(r => r.RoomId == "792FD").First();
            
            /* change parlor top left door to be solid blocks */
            parlor.RoomState[0].LevelData.Layer1[0, 6].Clip = 0x08;
            parlor.RoomState[0].LevelData.Layer1[1, 6].Clip = 0x08;
            parlor.RoomState[0].LevelData.Layer1[0, 7].Clip = 0x08;
            parlor.RoomState[0].LevelData.Layer1[1, 7].Clip = 0x08;
            parlor.RoomState[0].LevelData.Layer1[0, 8].Clip = 0x08;
            parlor.RoomState[0].LevelData.Layer1[1, 8].Clip = 0x08;
            parlor.RoomState[0].LevelData.Layer1[0, 9].Clip = 0x08;
            parlor.RoomState[0].LevelData.Layer1[1, 9].Clip = 0x08;

            parlor.RoomState[0].LevelData.Layer1[0, 6].BTS = 0x00;
            parlor.RoomState[0].LevelData.Layer1[1, 6].BTS = 0x00;
            parlor.RoomState[0].LevelData.Layer1[0, 7].BTS = 0x00;
            parlor.RoomState[0].LevelData.Layer1[1, 7].BTS = 0x00;
            parlor.RoomState[0].LevelData.Layer1[0, 8].BTS = 0x00;
            parlor.RoomState[0].LevelData.Layer1[1, 8].BTS = 0x00;
            parlor.RoomState[0].LevelData.Layer1[0, 9].BTS = 0x00;
            parlor.RoomState[0].LevelData.Layer1[1, 9].BTS = 0x00;
            handler.Write();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            handler.Read();

            var terminator = handler.MDBList.Where(x => x.RoomId == "7990D").First();

            /* get memory for MDB header, state select and default state */
            terminator.RoomAddress = handler.Mem.Allocate(0x8F, 11 + terminator.StateSelectSize + 26);
            terminator.DoorOut = handler.Mem.Allocate(0x8F, (4 * terminator.DDB.Count) + 4);

            foreach(var door in terminator.DDB)
            {
                door.Pointer = handler.Mem.Allocate(0x83, 12);
                if (door.DoorASM.Length > 0)
                    door.Code = handler.Mem.Allocate(0x8F, door.DoorASM.Length);
            }

            foreach(var roomState in terminator.RoomState)
            {
                if(roomState.Pointer != 0xE5E6)
                    roomState.Pointer = handler.Mem.Allocate(0x8F, 26);
                
                roomState.EnemyPop = handler.Mem.Allocate(0xA1, (18 * roomState.EnemyPopList.Count) + 3);
                roomState.EnemySet = handler.Mem.Allocate(0xB4, (6 * roomState.EnemySetList.Count) + 4);
                roomState.FX1 = handler.Mem.Allocate(0x83, 16);
                if (roomState.Scroll > 0x0000)
                {
                    roomState.Scroll = handler.Mem.Allocate(0x8F, roomState.ScrollData.Length + roomState.ScrollMod.Sum(x => x.Length) + 4);
                }
                roomState.PLM = handler.Mem.Allocate(0x8F, (6 * roomState.PLMList.Count) + 4);
            }

            /* repoint doors to lead to new room */
            foreach(var room in handler.MDBList)
            {
                foreach(var ddb in room.DDB)
                {
                    if (ddb.RoomId == 0x990D)
                        ddb.RoomId = terminator.RoomAddress;
                }
            }

            handler.Write();
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
    }
}
