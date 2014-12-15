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
        ROMHandler handler;
        private MDB currentRoom;
        public Main()
        {
            InitializeComponent();
            currentRoom = new MDB();
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }


        private void btnOpen_Click(object sender, EventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            if(fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fileName = fileDialog.FileName;
                handler = new ROMHandler(fileName);
                handler.Read();
                refreshRoomList();
            }
        }



        private void refreshRoomList()
        {
            lvRooms.Items.Clear();
            foreach(var room in handler.MDBList)
            {
                lvRooms.Items.Add(new ListViewItem(new string[] { room.RoomId, room.Width.ToString(), room.Height.ToString() }));
            }
        }

        private void refreshRoom(MDB room)
        {
            /* update room graphics */
            redrawRoom(room);

            /* update textboxes */
            txtAddress.Text = String.Format("{0:X}", room.RoomAddress);
            txtDoorOut.Text = String.Format("{0:X}", room.DoorOut);
            txtX.Text = room.XPos.ToString();
            txtY.Text = room.YPos.ToString();
            txtWidth.Text = room.Width.ToString();
            txtHeight.Text = room.Height.ToString();
            txtRegion.Text = room.Region.ToString();

            var dsBindingList = new BindingList<DDB>(room.DDB);
            var dsBindingSource = new BindingSource(dsBindingList, null);
            dgvDDB.DataSource = dsBindingSource;
            dgvDDB.Columns.Remove("DoorASM");

            dgvDDB.Columns[0].DefaultCellStyle.Format = "X04";
            dgvDDB.Columns[1].DefaultCellStyle.Format = "X04";
            dgvDDB.Columns[2].DefaultCellStyle.Format = "X04";
            dgvDDB.Columns[3].DefaultCellStyle.Format = "X04";
            dgvDDB.Columns[8].DefaultCellStyle.Format = "X04";
            dgvDDB.Columns[9].DefaultCellStyle.Format = "X04";


            var rsBindingList = new BindingList<RoomState>(room.RoomState);
            var rsBindingSource = new BindingSource(rsBindingList, null);
            dgvRoomStates.DataSource = rsBindingSource;
            dgvRoomStates.Columns.Remove("LevelData");
            dgvRoomStates.Columns.Remove("LayerHandlingCode");
            dgvRoomStates.Columns.Remove("FX1Data");
            dgvRoomStates.Columns.Remove("Unused");
            dgvRoomStates.Columns.Remove("TestValueDoor");

            dgvRoomStates.Columns[0].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[1].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[2].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[3].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[4].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[5].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[6].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[7].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[8].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[9].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[10].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[11].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[12].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[13].DefaultCellStyle.Format = "X04";
            dgvRoomStates.Columns[14].DefaultCellStyle.Format = "X04";

            currentRoom = room;
        }

        private void redrawRoom(MDB room)
        {
            /* update level view */
            pictureBox1.Width = room.Width * 16 * 3;
            pictureBox1.Height = room.Height * 16 * 3;
            Bitmap bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            for (int y = 0; y < room.RoomState[0].LevelData.Height; y++)
            {
                for (int x = 0; x < room.RoomState[0].LevelData.Width; x++)
                {
                    var clip = room.RoomState[0].LevelData.Layer1[x, y].Clip;
                    var bts = room.RoomState[0].LevelData.Layer1[x, y].BTS;
                    switch (clip)
                    {
                        case 0x08:
                            g.FillRectangle(Brushes.Black, x * 3, y * 3, 3, 3);
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
                            g.DrawRectangle(Pens.Red, x * 3, y * 3, 3, 3);
                            break;
                    }
                }
            }

            pictureBox1.Image = bmp;
            pictureBox1.Invalidate();
        }

        private void lvRooms_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (lvRooms.SelectedItems.Count > 0)
            {
                var roomId = lvRooms.SelectedItems[0].Text;
                var room = handler.MDBList.Where(r => r.RoomId == roomId).FirstOrDefault();
                if (room == null)
                    return;
                btnAddRoom.Visible = false;
                refreshRoom(room);
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void dgvRoomStates_SelectionChanged(object sender, EventArgs e)
        {
            var roomState = (RoomState)dgvRoomStates.CurrentRow.DataBoundItem;
            var dsBindingList = new BindingList<PLM>(roomState.PLMList);
            var dsBindingSource = new BindingSource(dsBindingList, null);
            dgvPLM.DataSource = dsBindingSource;

            dgvPLM.Columns[0].DefaultCellStyle.Format = "X04";
            dgvPLM.Columns[1].DefaultCellStyle.Format = "X04";
            dgvPLM.Columns[2].DefaultCellStyle.Format = "X04";
            dgvPLM.Columns[3].DefaultCellStyle.Format = "X04";


            var rsBindingList = new BindingList<EnemyPop>(roomState.EnemyPopList);
            var rsBindingSource = new BindingSource(rsBindingList, null);
            dgvEnemyPop.DataSource = rsBindingSource;

            dgvEnemyPop.Columns[0].DefaultCellStyle.Format = "X04";
            dgvEnemyPop.Columns[3].DefaultCellStyle.Format = "X04";
            dgvEnemyPop.Columns[4].DefaultCellStyle.Format = "X04";
            dgvEnemyPop.Columns[5].DefaultCellStyle.Format = "X04";
            dgvEnemyPop.Columns[6].DefaultCellStyle.Format = "X04";
            dgvEnemyPop.Columns[7].DefaultCellStyle.Format = "X04";

            var raBindingList = new BindingList<EnemySet>(roomState.EnemySetList);
            var raBindingSource = new BindingSource(raBindingList, null);
            dgvEnemySet.DataSource = raBindingSource;
            dgvEnemySet.Columns[0].DefaultCellStyle.Format = "X04";
        }

        private void txtDoorOut_Leave(object sender, EventArgs e)
        {
            currentRoom.DoorOut = Convert.ToUInt16(txtDoorOut.Text, 16);
        }

        private void txtX_Leave(object sender, EventArgs e)
        {
            currentRoom.XPos = Byte.Parse(txtX.Text);
        }

        private void txtY_Leave(object sender, EventArgs e)
        {
            currentRoom.YPos = Byte.Parse(txtY.Text);
        }

        private void txtRegion_Leave(object sender, EventArgs e)
        {
            currentRoom.Region = Byte.Parse(txtRegion.Text);
        }

        private void txtName_Leave(object sender, EventArgs e)
        {
            currentRoom.Name = txtName.Text;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            handler.Write();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (lvRooms.CheckedItems.Count == 0)
            {
                MessageBox.Show("No room selected for export");
                return;
            }
            var fileSaveDialog = new FolderBrowserDialog();
            if(fileSaveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach(ListViewItem listViewItem in lvRooms.CheckedItems)
                {
                    var roomId = listViewItem.Text;
                    var room = handler.MDBList.Where(r => r.RoomId == roomId).FirstOrDefault();
                    if(room != null)
                    {
                        room.Save(fileSaveDialog.SelectedPath);
                    }
                }
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fileName = fileDialog.FileName;
                var room = MDB.Load(fileName);

                /* wipe out all the pointers for this room */
                //room.RoomAddress = 0xFFFF;
                //room.DoorOut = 0xFFFF;
                //foreach(var roomState in room.RoomState)
                //{
                //    if (roomState.BGDataPtr > 0)
                //        roomState.BGDataPtr = 0xFFFF;
                //    if (roomState.EnemyPop > 0)
                //        roomState.EnemyPop = 0xFFFF;
                //    if (roomState.EnemySet > 0)
                //        roomState.EnemySet = 0xFFFF;
                //    if (roomState.FX1 > 0)
                //        roomState.FX1 = 0xFFFF;
                //    if (roomState.LayerHandling > 0)
                //        roomState.LayerHandling = 0xFFFF;
                //    if (roomState.RoomData > 0)
                //        roomState.RoomData = 0xFFFFFF;
                //    if (roomState.PLM > 0)
                //        roomState.PLM = 0xFFFF;
                //    if(roomState.Scroll > 0)
                //        roomState.Scroll = 0xFFFF;
                //    if(roomState.Pointer != 0xE5E6)
                //        roomState.Pointer = 0xFFFF;
                //}
                
                //foreach(var ddb in room.DDB)
                //{
                //    ddb.Pointer = 0xFFFF;
                //}

                refreshRoom(room);
                btnAddRoom.Visible = true;
            }
        }

        private void btnAddRoom_Click(object sender, EventArgs e)
        {
            handler.AddRoom(currentRoom);
            btnAddRoom.Visible = false;
            refreshRoomList();
            refreshRoom(currentRoom);

            for (int i = 0; i < lvRooms.Items.Count; i++)
                if(lvRooms.Items[i].Text == currentRoom.RoomId)
                {
                    lvRooms.Items[i].Selected = true;
                    lvRooms.Select();
                    lvRooms.EnsureVisible(i);
                    break;
                }

        }

        private void dgvDDB_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if(dgvDDB.Columns[e.ColumnIndex].DefaultCellStyle.Format == "X04")
            {
                e.Value = Convert.ToUInt16(e.Value.ToString(), 16);
                e.ParsingApplied = true;
            }
        }

        private void dgvRoomStates_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (dgvRoomStates.Columns[e.ColumnIndex].DefaultCellStyle.Format == "X04")
            {
                e.Value = Convert.ToUInt16(e.Value.ToString(), 16);
                e.ParsingApplied = true;
            }
        }

        private void dgvPLM_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (dgvPLM.Columns[e.ColumnIndex].DefaultCellStyle.Format == "X04")
            {
                e.Value = Convert.ToUInt16(e.Value.ToString(), 16);
                e.ParsingApplied = true;
            }
        }

        private void dgvEnemySet_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (dgvEnemySet.Columns[e.ColumnIndex].DefaultCellStyle.Format == "X04")
            {
                e.Value = Convert.ToUInt16(e.Value.ToString(), 16);
                e.ParsingApplied = true;
            }
        }

        private void dgvEnemyPop_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (dgvEnemyPop.Columns[e.ColumnIndex].DefaultCellStyle.Format == "X04")
            {
                e.Value = Convert.ToUInt16(e.Value.ToString(), 16);
                e.ParsingApplied = true;
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem listViewItem in lvRooms.CheckedItems)
            {
                var roomId = listViewItem.Text;
                var room = handler.MDBList.Where(r => r.RoomId == roomId).FirstOrDefault();
                if (room != null)
                {
                    handler.RemoveRoom(room);
                }
            }
            refreshRoomList();
        }
    }
}
