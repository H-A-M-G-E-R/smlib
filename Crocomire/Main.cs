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
            listBox1.Items.AddRange(handler.MDBList.Select(x => String.Format("{0} -> w: {1}, h: {2}, doorout: {3:X}, roomstates: {4}, doors: {5}, plms: {6}", new object[] { x.RoomId, x.Width, x.Height, x.DoorOut, x.RoomState.Count, x.DDB.Count, x.RoomState[0].PLMList.Count })).ToArray());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            handler.Write();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            handler.Read();

            var parlor = handler.MDBList.Where(x => x.RoomAddress == 0x92FD).First();            

            /* relocate/repoint parlor */
            parlor.RoomAddress = 0xF000;
            parlor.RoomId = "7F000";
            parlor.DoorOut = 0xFEE0;

            ushort plm = 0xFC00;
            ushort scroll = 0xFA00;


            foreach (var roomState in parlor.RoomState)
            {
                roomState.PLM = plm;
                roomState.Scroll = scroll;
                roomState.RoomData = 0xCEB330;

                plm += (ushort)((roomState.PLMList.Count * 6) + 4);
                scroll += (ushort)(roomState.ScrollData.Length + roomState.ScrollMod.Sum(x => x.Length) + 0x10);
            }     

            /* find doors pointing to this room and repoint them */
            foreach(var room in handler.MDBList)
            {
                foreach(var door in room.DDB)
                {
                    if(door.RoomId == 0x92FD)
                    {
                        door.RoomId = 0xF000;
                    }
                }
            }
            handler.Write();
        }
    }
}
