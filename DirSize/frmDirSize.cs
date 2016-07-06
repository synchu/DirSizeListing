using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DirSize
{
    public partial class frmDirSize : Form
    {
        private BindingSource dsFolders = new BindingSource();
        DataTable dtFolders = null;
        string selectedPath = "C:\\temp";
        List<string> log = new List<string>();
        bool bLog = false;


        public frmDirSize()
        {
            InitializeComponent();
        }

        private void frmDirSize_Load(object sender, EventArgs e)
        {
            textBox1.Text = selectedPath;
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            DialogResult result = fbd.ShowDialog();

            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                selectedPath = fbd.SelectedPath;
                textBox1.Text = selectedPath;
                getDirs(selectedPath);
                assignSources();
                formatSize();
            }
        }


        private void getDirs(string selectedFolder)
        {
            label1.Visible = false;
            progressBar1.Visible = true;
            progressBar1.Step = 1;
            progressBar1.Value = 0;

            initDataTable();
            ProcessDirectory(selectedFolder, 0, 0);
           
            formatDataViewGrid();
       
            progressBar1.Visible = false;
            label1.Visible = true;
            progressBar1.Step = 1;
        }

        private void initDataTable()
        {

            if (dtFolders != null)
            {
                dtFolders.Dispose();
            }

            dtFolders = new DataTable("Folders");
            DataColumn pkId = dtFolders.Columns.Add("pkId", typeof(long));
            dtFolders.Columns.Add("colType", typeof(string));
            dtFolders.Columns.Add("colName", typeof(string));
            dtFolders.Columns.Add("colSize", typeof(Int64));
            dtFolders.Columns.Add("colModifiedDate", typeof(DateTime));

            dtFolders.PrimaryKey = new DataColumn[] { pkId };
        }

        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        
        private void ProcessDirectory(string targetDirectory, int firstLevelDir, Int64 pkId)
        {
            // Process the list of files found in the directory.
            
            
            List<FileInfo> fiList = new List<FileInfo>();
            DataRow drFolder = null;
            long firstLevelPkId = 0;
            string[] fileEntries = null;
            bool someSkipped = false;


            try
            {
              fileEntries = Directory.GetFiles(targetDirectory);
            } catch (System.IO.PathTooLongException e)
            {
                log.Add(e.Message);
            } catch (UnauthorizedAccessException e)
            {
                log.Add(e.Message);
            }


            if (firstLevelDir > -1)
            {
                try
                {
                    foreach (string fileName in fileEntries)
                    {
                        FileInfo fi = ProcessFile(fileName);
                        fiList.Add(fi);
                        if (firstLevelDir == 0)
                        {
                            drFolder = dtFolders.NewRow();
                            drFolder["pkId"] = fi.GetHashCode();
                            drFolder["colType"] = "File";
                            drFolder["colName"] = fileName;
                            drFolder["colModifiedDate"] = fi.LastWriteTime;
                            drFolder["colSize"] = fi.Length;
                            dtFolders.Rows.Add(drFolder);
                        }
                    }
                } catch (Exception e)
                {
                    log.Add(e.Message);
                }
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = null;

            try
            {
               subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            } catch (System.IO.PathTooLongException e)
            {
                log.Add(e.Message);
                someSkipped = true;
                label1.Text = "Some folders might have been skipped, due to the path being too long!";                
            } catch (UnauthorizedAccessException e)
            {
                log.Add(e.Message);
                someSkipped = true;
                label1.Text = "Some folders might have been skipped, due to no access permissions!";
            }

            

            if (!someSkipped)
            {
                int progress = 0;
                progressBar1.Value = 1;
                progressBar1.Maximum = subdirectoryEntries.Length + 1;
                progressBar1.Step = 4;

                foreach (string subdirectory in subdirectoryEntries)
                {

                    if (firstLevelDir == 0)
                    {
                        drFolder = dtFolders.NewRow();
                        drFolder["pkId"] = firstLevelPkId = new FileInfo(subdirectory).GetHashCode();
                        drFolder["colType"] = "Folder";
                        drFolder["colName"] = subdirectory;
                        drFolder["colModifiedDate"] = new FileInfo(subdirectory).LastWriteTime;
                        drFolder["colSize"] = 0;
                        dtFolders.Rows.Add(drFolder);
                    }
                    else if (firstLevelDir > 0)
                    {
                        UpdateFolderSize(pkId, fiList);
                    }

                    ProcessDirectory(subdirectory, firstLevelDir + 1, firstLevelDir == 0 ? firstLevelPkId : pkId);

                    if (progress % 4 == 0) progressBar1.PerformStep();
                    
                    progress++;
                }


                if (subdirectoryEntries.Length == 0 && firstLevelDir > 0)
                {
                    UpdateFolderSize(pkId, fiList);
                }
            }

        }

        private void UpdateFolderSize(long pkId, List<FileInfo> fiList)
        {
            DataRow[] foundRows = dtFolders.Select("pkId=" + pkId.ToString());
            foundRows[0]["colSize"] = long.Parse(foundRows[0]["colSize"].ToString()) + getDirSize(fiList);
            fiList.Clear();
        }

        private void formatDataViewGrid()
        {
            this.dgvFolders.Columns["colSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.dgvFolders.Columns["colModifiedDate"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        private static FileInfo ProcessFile(string path)
        {
            return new FileInfo(path);
        }

        private Int64 getDirSize(List<FileInfo> fiList)
        {
            Int64 size = 0;
            foreach (FileInfo fi in fiList)
            {
                size += fi.Length;
                log.Add(fi.Name);
            }
            return size;
        }

        private void assignSources()
        {
            dsFolders.DataSource = dtFolders;
            dgvFolders.DataSource = dsFolders;

        }

        private void formatSize()
        {
            var col = dtFolders.Columns["colSize"];
            foreach (DataRow row in dtFolders.Rows)
                row[col] = Convert.ToInt64(row[col])/1024 == 0? 1: Convert.ToInt64(row[col]) / 1024;
        }
        private void btnRefresh_Click(object sender, EventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(selectedPath)) {
                getDirs(selectedPath);
                assignSources();
                formatSize();
            } else
            {
                MessageBox.Show(this, "Please, select a folder first!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (bLog)
            {
                log.Sort();
                StreamWriter fs = new StreamWriter(selectedPath + "\\log.txt");
                foreach (string s in log)
                {
                    fs.WriteLine(s);
                }
                fs.Flush();
                fs.Close();
            }

        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (Directory.Exists(textBox1.Text))
            {
                selectedPath = textBox1.Text;
                label1.Text = "Correct selection";
            } else
            {
                label1.Text = "Please, type or select an existing folder!";
            }
        }

        private void dgvFolders_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > 0 && e.ColumnIndex > 0)
            {
                if (Directory.Exists(dgvFolders.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString()))
                {
                    Process.Start(dgvFolders.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString());
                }
            }
        }

        private void dgvFolders_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {

            if (e.RowIndex > 0 && e.ColumnIndex > 0)
            {
                if (Directory.Exists(dgvFolders.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString()))
                {
                    e.ToolTipText = "Double click to open this folder in Windows Explorer!";
                }
            }
        }
    }
}
