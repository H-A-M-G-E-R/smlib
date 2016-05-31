using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SMLib;
using System.IO;

namespace MDBTool
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fileName = fileDialog.FileName;
                var handler = new ROMHandler(fileName);
                handler.Read();

                var path = Application.StartupPath + "\\" + fileName.Substring(fileName.LastIndexOf("\\") + 1, fileName.Length - fileName.LastIndexOf("\\") - 5) + "\\";
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch { }

                using (var file = new StreamWriter(path + "mdb.txt"))
                {
                    foreach (var room in handler.MDBList.OrderBy(r => r.RoomId))
                    {
                        file.WriteLine(room.RoomId);
                    }
                }

                var levelEntries = new List<uint>();
                foreach (var room in handler.MDBList)
                {
                    foreach (var state in room.RoomState)
                    {
                        if (!levelEntries.Contains(state.RoomData))
                        {
                            levelEntries.Add(state.RoomData);
                        }
                    }
                }

                using (var file = new StreamWriter(path + "level_entries.txt"))
                {
                    foreach (var entry in levelEntries.OrderBy(l => l))
                    {
                        file.WriteLine(String.Format("{0:X}", SMLib.Lunar.ToPC(entry)));
                    }
                }

                MessageBox.Show(handler.MDBList.Count.ToString() + " rooms written to MDB");

            }
        }
    }
}
