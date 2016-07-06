using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
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
            tbPath.Text = selectedPath;
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            DialogResult result = fbd.ShowDialog();

            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                selectedPath = fbd.SelectedPath;
                tbPath.Text = selectedPath;
                getDirs(selectedPath);
                assignSources();
                formatSize();
            }
        }


        private void getDirs(string selectedFolder)
        {
            label1.Visible = false;
            progressBar.Visible = true;
            progressBar.Step = 1;
            progressBar.Value = 0;

            initDataTable();

            ProcessDirectory(selectedFolder, 0, 0);

            formatDataViewGrid();

            progressBar.Visible = false;
            label1.Visible = true;
            progressBar.Step = 1;
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
            }
            catch (System.IO.PathTooLongException e)
            {
                log.Add(e.Message);
            }
            catch (UnauthorizedAccessException e)
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
                            InsertRow(fileName, fi, "File", fi.Length);
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Add(e.Message);
                }
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = null;

            try
            {
                subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            }
            catch (System.IO.PathTooLongException e)
            {
                someSkipped = logNonCriticalError("Some folders might have been skipped, due to the path being too long!", e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                someSkipped = logNonCriticalError("Some folders might have been skipped, due to no access permissions!", e.Message);
            }



            if (!someSkipped)
            {
                int progress = 0;
                progressBar.Value = 1;
                progressBar.Maximum = subdirectoryEntries.Length + 1;
                progressBar.Step = 4;

                foreach (string subdirectory in subdirectoryEntries)
                {

                    if (firstLevelDir == 0)
                    {
                        firstLevelPkId = InsertRow(subdirectory, new FileInfo(subdirectory), "Folder", 0);
                    }
                    else if (firstLevelDir > 0)
                    {
                        UpdateFolderSize(pkId, fiList);
                    }

                    ProcessDirectory(subdirectory, firstLevelDir + 1, firstLevelDir == 0 ? firstLevelPkId : pkId);

                    //fancy mod 4 based update
                    if ((progress & 3) == 0) progressBar.PerformStep();

                    progress++;
                }


                if (subdirectoryEntries.Length == 0 && firstLevelDir > 0)
                {
                    UpdateFolderSize(pkId, fiList);
                }
            }

        }

        private bool logNonCriticalError(string labelMessage, string logMessage)
        {
            log.Add(logMessage);
            label1.Text = labelMessage;
            return true;
        }

        private long InsertRow(string Name, FileInfo fi, string colType, long size)
        {
            long pkId = 0;

            DataRow drFolder = dtFolders.NewRow();
            drFolder["pkId"] = pkId = fi.GetHashCode();
            drFolder["colType"] = colType;
            drFolder["colName"] = Name;
            drFolder["colModifiedDate"] = fi.LastWriteTime;
            drFolder["colSize"] = size;
            dtFolders.Rows.Add(drFolder);
            return pkId;
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
                row[col] = Convert.ToInt64(row[col]) / 1024 == 0 ? 1 : Convert.ToInt64(row[col]) / 1024;
        }
        private void btnRefresh_Click(object sender, EventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                getDirs(selectedPath);
                assignSources();
                formatSize();
            }
            else
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
            if (Directory.Exists(tbPath.Text))
            {
                selectedPath = tbPath.Text;
                label1.Text = "Correct selection";
            }
            else
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
                    e.ToolTipText = "Double click to open this folder in Windows Explorer.";
                }
            }
        }
    }
}
