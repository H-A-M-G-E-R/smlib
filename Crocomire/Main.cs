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
            listBox1.Items.AddRange(handler.MDBList.Select(x => String.Format("{0} -> w: {1}, h: {2}, doorout: {3:X}, roomstates: {4}, doors: {5}, plms: {6}, s: {7}", new object[] { x.RoomId, x.Width, x.Height, x.DoorOut, x.RoomState.Count, x.DDB.Count, x.RoomState[0].PLMList.Count, x.LevelData.Size })).ToArray());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            handler.Write();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            handler.Read();

            /* get parlor */

            var parlor = handler.MDBList.Where(x => x.RoomId == "792FD").First();
            var mainStreetDoor = parlor.DDB.Where(x => x.RoomId == 0x93AA).First();

            /* change where this door goes */
            mainStreetDoor.RoomId = 0x92B3;

            handler.Write();
        }
    }
}
