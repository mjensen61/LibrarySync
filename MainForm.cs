/*
 * Created by SharpDevelop.
 * User: annas
 * Date: 6/6/2017
 * Time: 11:50 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Synchronization;
using Microsoft.Synchronization.FeedSync;
using Microsoft.Synchronization.Files;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Runtime.InteropServices;

namespace LibrarySync
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
        string replica1RootPath;
        string replica2RootPath;
        string XMLfilename;
        string customer_domain;
        string customer_domain_ver;

        public enum IniState
        {
            OK = 0,
            Error = 2
        }

        FileSyncClass fileSync;

        ProgressForm progressForm;

        public string syncmessage;

        public MainForm(string[] args)
        {
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();

            progressForm = new ProgressForm();
            customer_domain = "";
            customer_domain_ver = "";

            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            label4.Text = string.Format("Version {0}.{1}.{2} Rev {3} ",
                version.Major, version.Minor, version.Build, version.Revision);

            label4.Text = "Version" + productVersion;
            if (args.Length < 2
                || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1])
                )
            {
                replica1RootPath = @"SELECT SOURCE FOLDER!";
                replica2RootPath = @"SELECT DESTINATION FOLDER!";

                ReadConfig("Directories", "MasterRepoPath", ref replica1RootPath);
                ReadConfig("Directories", "LocalRepoPath",  ref replica2RootPath);

            }
            else
            {
                if (args.Length > 0) replica1RootPath = args[0];
                if (args.Length > 1)  replica2RootPath = args[1];
                if (args.Length > 2)  XMLfilename = args[2];
            }
        
            textBox2.Text = replica1RootPath;
            textBox3.Text = replica2RootPath;

            if (args.Length > 2)
            {
            	if(File.Exists(XMLfilename)) // Load domain from xml file
            	{
            		XmlDocument doc = new XmlDocument();
					doc.Load(XMLfilename);
					foreach(XmlNode node in doc.DocumentElement.ChildNodes)
					{						
						string caption = node.Name; //or loop through its children as well
						if(node.Name == "customer_domain")
						{
							customer_domain = node.Attributes["Value"].Value;
						}
						else if(node.Name == "customer_domain_ver")
						{
							customer_domain_ver = node.Attributes["Value"].Value;
						}
					}
            	}
            }
        }

        public IniState ReadConfig(string section, string key, ref string value)
        {
            IniFile ini;
            IniState retVal = IniState.Error;
            string bankname = string.Empty;
            string basePath = System.Environment.CurrentDirectory;

            if (File.Exists(basePath + "\\" + "LibrarySync.ini"))
            {
                ini = new IniFile(basePath + "\\" + "LibrarySync.ini");
                ini.IniReadValue(section, key, ref value);
                retVal = IniState.OK;
            }
            else
            {
                ini = new IniFile(basePath + "\\" + "LibrarySync.ini");
                ini.IniWriteValue("LibrarySync", "Version", "3.02.00");
                ini.IniWriteValue("Directories", "MasterRepoPath", replica1RootPath);
                ini.IniWriteValue("Directories", "LocalRepoPath",  replica2RootPath);
                retVal = IniState.OK;
            }
            return retVal;
        }

        public IniState WriteConfig()
        {
            IniFile ini;
            IniState retVal = IniState.Error;
            string basePath = System.Environment.CurrentDirectory;

            ini = new IniFile(basePath + "\\" + "LibrarySync.ini");
            ini.IniWriteValue("LibrarySync", "Version", "3.02.00");
            ini.IniWriteValue("Directories", "MasterRepoPath", replica1RootPath);
            ini.IniWriteValue("Directories", "LocalRepoPath", replica2RootPath);
            retVal = IniState.OK;

            return retVal;
        }

        void MainFormShown(object sender, EventArgs e)
		{
            PopulateTrees(checkBox1.Checked);
		}

		private void listViewFiles_Resize(object sender, EventArgs e)
        {
            ResizeListViewColumns();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            ResizeListViewColumns();
        }

        private void ResizeListViewColumns()
        {
            // width for each column
            int columnWidth = (listViewFiles.Width / 2);
            // adjust for wider path column
            int pathWidth = columnWidth - 140;
            // adjust for smaller created column
            int dateWidth = 120;
            // adjust sync column
            int syncWidth = 40;

            // set width
            listViewFiles.Columns[0].Width = pathWidth;
            listViewFiles.Columns[1].Width = dateWidth;
            listViewFiles.Columns[2].Width = syncWidth;
            listViewFiles.Columns[3].Width = pathWidth;
            listViewFiles.Columns[4].Width = dateWidth;
        }

        void PopulateTrees(bool twoway)
        {
            progressForm.Show();
            Cursor.Current = Cursors.WaitCursor;
            fileSync = new FileSyncClass(replica1RootPath, replica2RootPath);

            try
            {
                fileSync.ListDirectory(treeView1, replica1RootPath);
                fileSync.ListDirectory(treeView2, replica2RootPath);

                foreach (TreeNode tn in treeView1.Nodes)
                {
                    tn.Expand();
                }
                foreach (TreeNode tn in treeView2.Nodes)
                {
                    tn.Expand();
                }

                fileSync.ColourSourceTreeview(treeView1,customer_domain,customer_domain_ver);

                if (checkBox1.Checked)
                {
                    fileSync.ColourDestinationTreeview(treeView2);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                statusStrip1.Text = "\nException from File Sync Provider:\n" + e.ToString();
                Cursor.Current = Cursors.Default;
            }
            progressForm.Hide();
            Cursor.Current = Cursors.Default;
        }

        void PopulateGrid()
        {
            foreach (SyncNode row in fileSync.syncList)
            {
                FileInfo sourceInfo = row.sourceInfo;
                FileInfo destinationInfo = row.destinationInfo;
                ListViewItem srcItem;

                // if the dest file does not exist
                if (!File.Exists(destinationInfo.FullName)) 
                {
                    // build the string array
                    string[] listRow = new string[5];
                    listRow[0] = sourceInfo.FullName;
                    listRow[1] = sourceInfo.LastWriteTime.ToShortDateString() + " " + sourceInfo.LastWriteTime.ToShortTimeString();
                    listRow[2] = "->";
                    listRow[3] = "";
                    listRow[4] = "";
                    // create the list view item and add it to the list view
                    srcItem = new ListViewItem(listRow);
                    srcItem.BackColor = Color.PaleGreen;
                    listViewFiles.Items.Add(srcItem);
                }
                // if the dest file exists
                else
                {
                    // build the string array
                    string[] listRow = new string[5];
                    listRow[0] = sourceInfo.FullName;
                    listRow[1] = sourceInfo.LastWriteTime.ToShortDateString() + " " + sourceInfo.LastWriteTime.ToShortTimeString();
                    // compare the files
                    listRow[2] = fileSync.CompareFiles(sourceInfo, destinationInfo);
                    listRow[3] = destinationInfo.FullName;
                    listRow[4] = destinationInfo.LastWriteTime.ToShortDateString() + " " + destinationInfo.LastWriteTime.ToShortTimeString();
                    // create the list view item and add it to the list view

                    srcItem = new ListViewItem(listRow);
                    if (fileSync.CompareFiles(sourceInfo, destinationInfo) == "->")
                    {
                        srcItem.BackColor = Color.LightBlue;
                    }
                    else if (fileSync.CompareFiles(sourceInfo, destinationInfo) == "<-")
                    {
                        srcItem.BackColor = Color.LightPink;
                    }
                    else
                    {
                        srcItem.BackColor = Color.LightSalmon;
                    }

                    listViewFiles.Items.Add(srcItem);
                   
                }
            }
        }

		void Button1Click(object sender, EventArgs e)
		{
            try
            {
                statusStrip1.Text = "test";

                fileSync.ListDirectory(treeView1, replica1RootPath);
                fileSync.ListDirectory(treeView2, replica2RootPath);
            }
            catch (Exception err)
            {
            	MessageBox.Show(err.ToString());
                statusStrip1.Text = "\nException from File Sync Provider:\n" + err.ToString();
            }
		}

        private void button2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Select Master Folder:";
            folderBrowserDialog1.ShowNewFolderButton = false;
            folderBrowserDialog1.SelectedPath = textBox2.Text;
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (DialogResult.OK == result)
            {
                replica1RootPath = folderBrowserDialog1.SelectedPath;
                textBox2.Text = replica1RootPath;
                PopulateTrees(checkBox1.Checked);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Select Destination Folder:";
            folderBrowserDialog1.ShowNewFolderButton = false;
            folderBrowserDialog1.SelectedPath = textBox3.Text;
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (DialogResult.OK == result)
            {
                replica2RootPath = folderBrowserDialog1.SelectedPath;
                textBox3.Text = replica2RootPath;
                PopulateTrees(checkBox1.Checked);
            }
        }

        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                node.Checked = nodeChecked;
                if (node.Nodes.Count > 0)
                {
                    // If the current node has child nodes, call the CheckAllChildsNodes method recursively.
                    this.CheckAllChildNodes(node, nodeChecked);
                }
            }
        }

        private void treeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node.ImageIndex == 0)
            {
                CheckAllChildNodes(e.Node,e.Node.Checked);
            }
        }

        public void GetCheckedNodes(TreeNodeCollection nodes,bool reverse)//, string path)
        {
            foreach (System.Windows.Forms.TreeNode aNode in nodes)
            {
                if (aNode.ImageIndex == 0) // Is a folder
                {
                    if (aNode.Nodes.Count != 0 && aNode.Text != "Development")
                    {
                        GetCheckedNodes(aNode.Nodes,reverse);//, path + aNode.Text + "\\");
                    }
                    else if (aNode.Text == "Development" && aNode.Checked == true)
                    {
                        aNode.Checked = false;
                        MessageBox.Show("Development folder ignored!");
                    }
                }
                else
                {
                    if (aNode.Checked)
                    {
                        FileNode thisNode = (FileNode)aNode;
                        string filename = thisNode.fileInfo.FullName;
                        if(reverse)
                        {
                          filename = replica1RootPath + filename.Substring(replica2RootPath.Length);
                        }
                        else
                        {
                          filename = replica2RootPath + filename.Substring(replica1RootPath.Length);
                        }
                        FileInfo newfile = new FileInfo(filename);
                        
                        fileSync.syncListAdd(thisNode.fileInfo, newfile);
                    }
                }   
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listViewFiles.Items.Clear();
            fileSync.clearSyncList();

            string path = replica1RootPath;
            foreach (TreeNode aNode in treeView1.Nodes)
            {
                if (aNode.ImageIndex == 0) // Is a folder
                {
                    if (aNode.Nodes.Count != 0)
                    {
                        GetCheckedNodes(aNode.Nodes,false);//, path + aNode.Text + "\\");
                    }
                }
            }
            foreach (TreeNode aNode in treeView2.Nodes)
            {
                if (aNode.ImageIndex == 0) // Is a folder
                {
                    if (aNode.Nodes.Count != 0)
                    {
                        GetCheckedNodes(aNode.Nodes,true);//, path + aNode.Text + "\\");
                    }
                }
            }
            fileSync.getSyncList();
            PopulateGrid();
            WriteConfig();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            progressForm.Show();
            fileSync.copySyncList();
            listViewFiles.Items.Clear();
            PopulateTrees(checkBox1.Checked);
            progressForm.Hide();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            listViewFiles.Items.Clear();
            PopulateTrees(checkBox1.Checked);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            progressForm.Show();
            if (checkBox1.Checked == true)
            {
                checkBox1.Text = "<< Two Way >>";
            }
            else
            {
                checkBox1.Text = "One Way >>";
            }
            listViewFiles.Items.Clear();
            PopulateTrees(checkBox1.Checked);
            progressForm.Hide();
        }
        
		void Button6Click(object sender, EventArgs e)
		{
			Close();
		}
		
		void Button7Click(object sender, EventArgs e)
		{
			Process p = new Process();
			p.StartInfo.FileName = "https://github.com/mjensen61/LibrarySync-executable";
			p.Start();
		}

        private void label1_Click(object sender, EventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = "https://mjdrafting.com.au/library-sync/";
            p.Start();
        }
    }
}
