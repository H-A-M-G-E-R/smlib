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


            listBox1.Items.Add("Removing rooms");
            /* wipe out some vanilla rooms to make some space */
            foreach (var deleteRoom in sm.MDBList.Where(r => r.RoomAddress > 0xB000 && r.RoomAddress < 0xD000).ToList())
                sm.RemoveRoom(deleteRoom);

            listBox1.Items.Add("Free memory: " + sm.Mem.FreeMemory.Sum(x => x.Value.Sum(y => y.Size)).ToString());

            listBox1.Items.Add("Adding room");
            sm.AddRoom(zfParlor);
            
            listBox1.Items.Add(String.Format("New room added and located at: 7{0:X}", zfParlor.RoomAddress));

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
