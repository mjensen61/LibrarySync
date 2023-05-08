using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

using System.Text;


namespace LibrarySync
{

    class SyncNode
    {
        public FileInfo sourceInfo { get; set; }
        public FileInfo destinationInfo { get; set; }

        public SyncNode(FileInfo source, FileInfo destination)
        {
            sourceInfo = source;
            destinationInfo = destination;
        }
    }

    class FileNode : TreeNode
    {
        public FileInfo fileInfo { get; set; }

        public FileNode(FileInfo fi)
        {
            this.Text = fi.Name;
            this.ToolTipText = fi.DirectoryName;
            this.fileInfo = fi;
        }
    }

    class DirectoryNode : TreeNode
    {
        public DirectoryInfo directoryInfo { get; set; }

        public DirectoryNode(DirectoryInfo di)
        {
            this.Text = di.Name;
            this.ToolTipText = di.FullName;
            this.directoryInfo = di;
        }
    }

    class FileSyncClass
    {
        string sourceReplicaRootPath;
        string destinationReplicaRootPath;
        IEnumerable<FileInfo> sourceFileList;
        IEnumerable<FileInfo> destinationFileList;
        int treeviewDepth = 0;
        string customer_domain = "";
        string customer_domain_ver = "";

        public List<SyncNode> syncList;

        // The lists
        public List<FileInfo> sourceAndDestination = new List<FileInfo>();
        public List<FileInfo> destinationAndSource = new List<FileInfo>();
        public List<FileInfo> sourceOnly = new List<FileInfo>();
        public List<FileInfo> destinationOnly = new List<FileInfo>();
        public List<FileInfo> sourceNewer = new List<FileInfo>();
        public List<FileInfo> destinationNewer = new List<FileInfo>();

        public FileSyncClass(string pathA, string pathB)  // Constructor
        {
            sourceReplicaRootPath = pathA;
            destinationReplicaRootPath = pathB;

            VerifyFolder(sourceReplicaRootPath);
            VerifyFolder(destinationReplicaRootPath);

            System.IO.DirectoryInfo dir1 = new System.IO.DirectoryInfo(pathA);
            System.IO.DirectoryInfo dir2 = new System.IO.DirectoryInfo(pathB);

            // Take a snapshot of the file system.  
            sourceFileList = dir1.GetFiles("*.*", System.IO.SearchOption.AllDirectories);
            destinationFileList = dir2.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

            syncList = new List<SyncNode>();
        }

        public void VerifyFolder(string Path)
        {
            bool folderExists = Directory.Exists(Path);
            if (!folderExists)
                Directory.CreateDirectory(Path);
        }

        public void ListDirectory(TreeView treeView, string path)
        {
            treeView.Nodes.Clear();
            var rootDirectoryInfo = new DirectoryInfo(path);
            TreeNode aRootNode = CreateDirectoryNode(rootDirectoryInfo);
            treeView.Nodes.Add(aRootNode);
            aRootNode.Expand();
        }

        // SyncList functions
        public void clearSyncList()
        {
            syncList.Clear();
        }

        public void syncListAdd(FileInfo sourceInfo, FileInfo destinationInfo)
        {
            SyncNode newnode = new SyncNode(sourceInfo, destinationInfo);
            syncList.Add(newnode);
        }

        public string getSyncList()
        {
            string retval = "";
            foreach (SyncNode aNode in syncList)
            {
                retval = retval + aNode.sourceInfo.FullName;
                retval = retval + "\t";
                retval = retval + aNode.destinationInfo.FullName;
                retval = retval + "\r\n";
            }
            return retval;
        }

        public int copySyncList()
        {
            int doOverwrite = -1; // -2: Stop copying files; -1: Do not overwrite this file; 0: Overwrite this file; 1: Overwrite all;
            int doReadonly = -1;  // -2: Do not copy all RO files; -1: Do not copy this RO file; 0: copy this RO file; 1 Copy all RO files;
            int errorfound = 0;

            foreach (SyncNode aNode in syncList)
            {
//==========================================================================================================
//                         |
//                         V
//       no      {  Does File Exist   }   yes
//       |                                 |
//       V                                 V
// [ copy file ]        no {  Does user want to overwrite  } yes     (check doOverwrite > -1 = yes)
//       |              |                                     |
//       V              V                                     V
// [skip to next] [skip to next]               no  {  Is file read only  }  yes
//                                             |                             |
//                                             V                             V
//                                       [ copy file ]        no   { Can we overwrite RO file }  yes     (check doReadonly > -1 = yes)
//                                             |              |                                   |
//                                             V              V                                   V
//                                      [skip to next]  [skip to next]                      [ copy file ]
//                                                                                                |
//                                                                                                V
//                                                                                         [skip to next]
//==========================================================================================================
            	// Set up permissions
            	if (File.Exists(aNode.destinationInfo.FullName))
            	{
            		if(doOverwrite < 1) // doOverwrite not set, User must request to overwrite files.
            		{
            			DialogResult result = MessageBox.Show(aNode.destinationInfo.Name +
                    	    	" exists.\n\n" +
                    	    	"Do you wish to overwrite ALL files?\n\n" +
                    	    	" - Answering Yes will overwrite ALL files.\n" +
                    	    	" - Answering No will overwrite this file only.\n" +
                    	    	" - Cancel will stop copying.",
                    	    	"Please confirm",
                    	    	MessageBoxButtons.YesNoCancel,
                    	    	MessageBoxIcon.Question
                    	);
	                	switch (result)
	                	{
	                	    case DialogResult.Yes:
	                	        doOverwrite = 1; // Overwrite all files
	                	        break;
	                	    case DialogResult.No:
	                	        doOverwrite = 0; // Overwrite this file
	                	        break;
	                	    case DialogResult.Cancel: // Stop copying
	                	        doOverwrite = -2;
	                	        break;
	                	    default:
	                	        doOverwrite = -1;
	                	        break;
	                	}
            		}

            		if(aNode.destinationInfo.IsReadOnly && doReadonly > -2)
               		{
	                	if(doReadonly < 1) // doReadonly not set, User must request to overwrite read only files.
	                	{
                    		DialogResult result = MessageBox.Show(aNode.destinationInfo.Name +
                    	   		" is READ ONLY.\n\n" +
                    	   		"Do you wish to ignore read only flags?\n\n" +
                       			" - Answering Yes will ignore ALL read only files.\n" +
                       			" - Answering No will ignore this file's flag only.\n" +
                       			" - Cancel will ignore read only.",
                       			"Please confirm",
                       			MessageBoxButtons.YesNoCancel,
                       			MessageBoxIcon.Question
                    		);
	                		switch (result)
	                		{
		                	    case DialogResult.Yes:
	                    	    	doReadonly = 1;
	                    	    	break;
	                    		case DialogResult.No:
		                	        doReadonly = 0;
	                    	    	break;
	                    		case DialogResult.Cancel: // Stop copying
		                	        doReadonly = -2;
	                    	    	break;
	                    		default:
		                	        doReadonly = -2;
	                    	    	break;
	                		}
	            		}
               		}
               	}
            	
            	if(doOverwrite == -2)
            	{
            		break; // force exit from loop
            	}
            	    
                try // try to copy the file
                {
                    VerifyFolder(aNode.destinationInfo.DirectoryName);
                    // if file exists, readonly and overwrite is set to >0 change flag and copy
                    if (File.Exists(aNode.destinationInfo.FullName) && aNode.destinationInfo.IsReadOnly && doOverwrite >= 0 && doReadonly >= 0)
                    {
                        // unset read-only
                        var attr = File.GetAttributes(aNode.destinationInfo.FullName);
                        attr = attr & ~FileAttributes.ReadOnly;
                        File.SetAttributes(aNode.destinationInfo.FullName,attr);
                        
                        File.Copy(aNode.sourceInfo.FullName, aNode.destinationInfo.FullName, true);
                    }
                    // if file exists, is not read only and overwrite is set to >0 change flag and copy, set overwrite back to -1
                    else if (File.Exists(aNode.destinationInfo.FullName) && !aNode.destinationInfo.IsReadOnly && doOverwrite >= 0)
                    {
                        File.Copy(aNode.sourceInfo.FullName, aNode.destinationInfo.FullName, true);
                    }
                    // if file does not exist copy the file
                    else if (!File.Exists(aNode.destinationInfo.FullName))
                    {
                  	    File.Copy(aNode.sourceInfo.FullName, aNode.destinationInfo.FullName, false);
                    }                    
                }
                catch
                {
     	           		MessageBox.Show("Error copying file.");
                        errorfound = -1;
                }
                // reset flags
                if(doOverwrite == 0)
                {
                	doOverwrite = -1;
                }
               	if(doReadonly == 0)
               	{
               		doReadonly = -1;
               	}
            }
            return errorfound;
        }

        public TreeNode CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            var directoryNode = new DirectoryNode(directoryInfo);
            directoryNode.ImageIndex = 0;
            directoryNode.BackColor = Color.White;
            directoryNode.directoryInfo = directoryInfo;

            foreach (var directory in directoryInfo.GetDirectories())
            {
            	if (directory.Name != "Development" && directory.Name != "Installation")
            	{
                	directoryNode.Nodes.Add(CreateDirectoryNode(directory));
            	}
            }
            foreach (var file in directoryInfo.GetFiles())
            {
                var filenode = new FileNode(file);
                directoryNode.Nodes.Add(filenode);
                filenode.ImageIndex = 1;
            }
            return directoryNode;
        }

        public void ColourSourceNode(TreeNode myNode,bool branchexists)
        {
     		string sourcepath = sourceReplicaRootPath;
     		string destinationpath = destinationReplicaRootPath;
     		treeviewDepth++;
     		
            if (myNode.ImageIndex == 0)
            {
                foreach (TreeNode newNode in myNode.Nodes)
                {
                	var aNode = (DirectoryNode)myNode; // casting needed
                	if(Directory.Exists(aNode.directoryInfo.FullName.Replace(sourcepath,destinationpath)))
                	{
                		myNode.BackColor = Color.PaleGreen;
                        ColourSourceNode(newNode, branchexists);
                        if (treeviewDepth < 2) myNode.Expand();
                    }
                	else
                	{
                	   	myNode.BackColor = Color.LightCoral;
                        ColourSourceNode(newNode,false);
                        if (treeviewDepth < 2) myNode.Expand();
                        // If caller requested this folder then check it
                        if (treeviewDepth == 1 && myNode.Text == customer_domain && newNode.Text == customer_domain_ver)
                        {
                        	newNode.BackColor = Color.Fuchsia;
                        	newNode.Checked = true;
                        }
                    }
                }
            }
            else if (myNode.ImageIndex == 1)
            {
                FileNode aNode = (FileNode)myNode;
               	if(File.Exists(aNode.fileInfo.FullName.Replace(sourcepath,destinationpath)))
               	{
               		FileInfo f2 = new FileInfo(aNode.fileInfo.FullName.Replace(sourcepath,destinationpath));
               		if(CompareFiles(aNode.fileInfo, f2) == "->")
               		{
               		    myNode.BackColor = Color.LightSalmon;
                        if(branchexists) myNode.Checked = true;
               		}
               		else
               		{
               			myNode.BackColor = Color.LightGreen;
               		}
                }
                else
                {
                	myNode.BackColor = Color.LightSalmon;
                    if (branchexists) myNode.Checked = true;
                }
            }
            treeviewDepth--;
        }

        public void ColourSourceTreeview(TreeView myTreeView, string domain, string version)
        {
            customer_domain = domain;
            customer_domain_ver = version;
            foreach (TreeNode myNode in myTreeView.Nodes[0].Nodes)
            {
            	treeviewDepth = 0;
                ColourSourceNode(myNode,true);
            }
        }

        public void ColourDestinationNode(TreeNode myNode, bool branchexists)
        {
            if (myNode.ImageIndex == 0)
            {
                foreach (TreeNode newNode in myNode.Nodes)
                {
                	var aNode = (DirectoryNode)myNode;
                	if(Directory.Exists(aNode.directoryInfo.FullName.Replace(destinationReplicaRootPath,sourceReplicaRootPath)))
                	{
                		myNode.BackColor = Color.PaleGreen;
                        ColourSourceNode(newNode, branchexists);
                    }
                	else
                	{
                	   	myNode.BackColor = Color.LightCoral;
                	}
                }
            }
            else if (myNode.ImageIndex == 1)
            {
                FileNode aNode = (FileNode)myNode;
               	if(File.Exists(aNode.fileInfo.FullName.Replace(destinationReplicaRootPath,sourceReplicaRootPath)))
               	{
               		FileInfo f2 = new FileInfo(aNode.fileInfo.FullName.Replace(destinationReplicaRootPath,sourceReplicaRootPath));
               		if(CompareFiles(aNode.fileInfo, f2) == "->")
               		{
               		    myNode.BackColor = Color.LightSalmon;
                        myNode.Expand();
                        if (branchexists) myNode.Checked = true;
               		}
               		else
               		{
               			myNode.BackColor = Color.LightGreen;
               		}
                }
                else
                {
                	myNode.BackColor = Color.LightSalmon;
                    if (branchexists) myNode.Checked = true;
                }
            }
        }

        public void ColourDestinationTreeview(TreeView myTreeView)
        {
            customer_domain = "";
            customer_domain_ver = "";
            foreach (TreeNode myNode in myTreeView.Nodes[0].Nodes)
            {
                ColourDestinationNode(myNode,true);
            }
        }

        public string ComparePaths()
        {
            string retval = "";
            
            foreach (var f1 in sourceFileList)
            {
                string filePath = destinationReplicaRootPath + "\\" + f1.FullName.Substring(sourceReplicaRootPath.Length + 1);
                if (File.Exists(filePath))
                {
                    FileInfo f2 = new FileInfo(filePath);
                    //string D1 = DateTime.CompareFiles(f1.LastWriteTimeUtc, f2.LastWriteTimeUtc);
                    string D1 = CompareFiles(f1, f2);
                    if (D1 == "->")
                    {
                        sourceNewer.Add(f1);
                    }
                    else if (D1 == "->")
                    {
                        destinationNewer.Add(f2);
                    }
                    else
                    {
                        sourceAndDestination.Add(f1);
                        destinationAndSource.Add(f2);
                    }
                }
            }

            return retval;
        }
        /// <summary>
        /// Compares files based on date.
        /// </summary>
        /// <param name="file1">Source file.</param>
        /// <param name="file2">Destination file.</param>
        /// <returns>Symbol determining which should be synchonized.</returns>
        public string CompareFiles(FileInfo f1, FileInfo f2)
        {
            #region Compare by last time accessed
            // compare the year
            if (f1.LastWriteTime.Year > f2.LastWriteTime.Year)
            {
                // file1 is newer
                return "->";
            }
            else if (f1.LastWriteTime.Year < f2.LastWriteTime.Year)
            {
                // file2 is newer
                return "<-";
            }
            // the year is the same, compare furthur
            else
            {
                // compare months
                if (f1.LastWriteTime.Month > f2.LastWriteTime.Month)
                {
                    // file1 is newer
                    return "->";
                }
                else if (f1.LastWriteTime.Month < f2.LastWriteTime.Month)
                {
                    // file2 is newer
                    return "<-";
                }
                // the month is the same, compare furthur
                else
                {
                    // compare days
                    if (f1.LastWriteTime.Day > f2.LastWriteTime.Day)
                    {
                        // file1 is newer
                        return "->";
                    }
                    else if (f1.LastWriteTime.Day < f2.LastWriteTime.Day)
                    {
                        // file2 is newer
                        return "<-";
                    }
                    // the day is the same, compare furthur
                    else
                    {
                        // compare hour
                        if (f1.LastWriteTime.Hour > f2.LastWriteTime.Hour)
                        {
                            // file1 is newer
                            return "->";
                        }
                        else if (f1.LastWriteTime.Hour < f2.LastWriteTime.Hour)
                        {
                            // file2 is newer
                            return "<-";
                        }
                        // the hour is the same, compare furthur
                        else
                        {
                            // compare minutes
                            if (f1.LastWriteTime.Minute > f2.LastWriteTime.Minute)
                            {
                                // file1 is newer
                                return "->";
                            }
                            else if (f1.LastWriteTime.Minute < f2.LastWriteTime.Minute)
                            {
                                // file2 is newer
                                return "<-";
                            }
                            // the minute is the same, compare furthur
                            else
                            {
                                // compare seconds and file sizes
                                if (f1.LastWriteTime.Second > f2.LastWriteTime.Second && f1.Length != f2.Length)
                                {
                                    // file1 is newer
                                    return "->";
                                }
                                else if (f1.LastWriteTime.Second < f2.LastWriteTime.Second && f1.Length != f2.Length)
                                {
                                    // file2 is newer
                                    return "<-";
                                }
                                else
                                {
                                    // seconds match and files are same size; consider equal
                                    return "=";
                                }
                            }
                        }
                    }
                }
            }
            #endregion
        }
    }

    #region ========================================================= Read initialisation file =========================================================
    public enum IniState
    {
        OK = 0,
        Error = 2
    }
    public class IniFile
    {
        public string path;
        // string Path = System.Environment.CurrentDirectory+"\\"+"ConfigFile.ini";

        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                 string key, string def, StringBuilder retVal,
            int size, string filePath);
        public IniFile(string Path)
        {
            path = Path;
        }

        public IniState IniWriteValue(string Section, string Key, string Value)
        {
            IniState retval = IniState.OK;

            WritePrivateProfileString(Section, Key, Value, this.path);

            return retval;
        }

        public IniState IniReadValue(string Section, string Key, ref string Value)
        {
            IniState retval = IniState.Error;

            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            255, this.path);
            if (i > 0) // Characters found
            {
                Value = temp.ToString();
                retval = IniState.OK;
            }

            return retval;

        }
    }
    #endregion
}