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
    }
}
