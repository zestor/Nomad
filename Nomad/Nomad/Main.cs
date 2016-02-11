using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace Nomad
{
    public partial class Main : Form
    {
        private DataManager dm;

        public Main()
        {
            InitializeComponent();
            dm = new DataManager();
            startupLoadFiles();
        }
        
        private string GetFileName(string file)
        {
            string[] FileParts = file.Split('\\');
            return FileParts[FileParts.Length - 1]; 
        }

        private void ClearGrid()
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();
            Application.DoEvents();
        }

        private void startupLoadFiles()
        {
            ClearGrid();
            dm.startupLoadFiles(dataGridView1);
            label1.Text = string.Format("[{0}] Contains {1} Jobs", GetFileName(dm.File1FileName), dm.GetFileLength1);
            label2.Text = string.Format("[{0}] Contains {1} Jobs", GetFileName(dm.File2FileName), dm.GetFileLength2);
        }

        private void getFile1()
        {
            ClearGrid();

            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Filter = "JSON Files|*.json";

            DialogResult dr = openFileDialog1.ShowDialog();

            if (dr == DialogResult.OK)
            {
                dm.File1FileName = openFileDialog1.FileName;
                dm.LoadGrid1(dm.File1FileName, dataGridView1);
                label1.Text = string.Format("[{0}] Contains {1} Jobs", GetFileName(dm.File1FileName), dm.GetFileLength1);
            }
        }

        private void getFile2()
        {
            ClearGrid();

            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Filter = "JSON Files|*.json";

            DialogResult dr = openFileDialog1.ShowDialog();

            if (dr == DialogResult.OK)
            {
                dm.File2FileName = openFileDialog1.FileName;
                if (dm.File1FileName == string.Empty)
                {
                    dm.File1FileName = openFileDialog1.FileName;
                    dm.LoadGrid1(dm.File2FileName, dataGridView1);
                    dm.LoadGrid2(dm.File2FileName, dataGridView1);
                }
                else
                {
                    dm.LoadGrid2(dm.File2FileName, dataGridView1);
                    dm.CompareFiles(dataGridView1);
                }
                label1.Text = string.Format("[{0}] Contains {1} Jobs", GetFileName(dm.File1FileName), dm.GetFileLength1);
                label2.Text = string.Format("[{0}] Contains {1} Jobs", GetFileName(dm.File2FileName), dm.GetFileLength2);
            }
        }

        private void btnFile1_Click(object sender, EventArgs e)
        {
            getFile1();
        }

        private void btnFile2_Click(object sender, EventArgs e)
        {
            getFile2();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            textBox1.Width = panel2.Width - 10;
            textBox2.Width = panel2.Width - 10;
            textBox3.Width = panel2.Width - 10;

            if (dataGridView1.CurrentRow != null)
            {
                textBox1.Text = (string)dataGridView1.CurrentRow.Cells[dm.GRID_POS_SLUG].Value;
                textBox2.Text = (string)dataGridView1.CurrentRow.Cells[dm.GRID_POS_TITLE].Value;
                textBox3.Text = (string)dataGridView1.CurrentRow.Cells[dm.GRID_POS_LOCATION].Value;
                webBrowser1.DocumentText = (string)dataGridView1.CurrentRow.Cells[dm.GRID_POS_DESCRIPTION].Value;
            }
        }

        private void btnNewFile_Click(object sender, EventArgs e)
        {
            ClearGrid();

            toolStripStatusLabel1.Text = "Downloading 1,000 Jobs Please Wait ...";
            Application.DoEvents();
            dm.CreateNewFile(dataGridView1);
            toolStripStatusLabel1.Text = string.Empty;
            label1.Text = string.Format("[{0}] Contains {1} Jobs", GetFileName(dm.File1FileName), dm.GetFileLength1);
            label2.Text = string.Format("[{0}] Contains {1} Jobs", GetFileName(dm.File2FileName), dm.GetFileLength2);
        }

        private void btnFilterClear_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                r.Visible = true;
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            Process.Start("iexplore.exe", String.Format("https://careers.avanade.com/login?jobId={0}", textBox1.Text.Trim()));
        }

        private void btnNotInterested_Click(object sender, EventArgs e)
        {
            dm.NotInterested(dataGridView1);
        }

        private void btnHideNotInterested_Click(object sender, EventArgs e)
        {
            dm.ToggleNotInterestedRows(dataGridView1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start("iexplore.exe", String.Format("https://careers.avanade.com/experienced/jobs/{0}", textBox1.Text.Trim()));
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                string title = (string)r.Cells[dm.GRID_POS_TITLE].Value;
                string location = (string)r.Cells[dm.GRID_POS_LOCATION].Value;

                if (title != null)
                {
                    if (title.ToUpper().Contains(txtFilter.Text.Trim().ToUpper()) || location.ToUpper().Contains(txtFilter.Text.Trim().ToUpper()))
                    {
                        r.Visible = true;
                    }
                    else
                    {
                        r.Visible = false;
                    }
                }
            }
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            textBox1.Width = panel2.Width - 80;
            textBox2.Width = panel2.Width - 80;
            textBox3.Width = panel2.Width - 80;
            splitContainer1.Width = this.Width;
            splitContainer1.Height = this.Height - panel1.Height - 100;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void openFile1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getFile1();
        }

        private void openFile2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getFile2();
        }

        private void compareFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dm.CompareFiles(dataGridView1);
        }


    }
}
