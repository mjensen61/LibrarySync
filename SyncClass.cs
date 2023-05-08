/*
 * Created by SharpDevelop.
 * User: annas
 * Date: 9/16/2017
 * Time: 5:37 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Collections;
using System.Data;
using System.Diagnostics;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Files;


namespace LibrarySync
{
 
	public enum FileSyncReplicaOptions
	{ 
    	 OneWay
	     ,TwoWay  
	}
	
	public static class Common
	{
        /// <summary>
        /// Walks Entire Path and returns Generic List of the files in the entire directory and sub directories
        /// </summary>
        /// <param name="strPath"></param>
        /// <param name="blShuttingDown"></param>
        /// <returns></returns>
        public static System.Collections.Generic.List<System.IO.FileInfo> WalkDirectory(string strPath, ref bool blShuttingDown)
        {
            if (Directory.Exists(strPath))
            {
                System.Collections.Generic.List<System.IO.FileInfo> Files;
                Files = new System.Collections.Generic.List<System.IO.FileInfo>();
                System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(strPath);
                WalkDirectoryTree(rootDir, ref Files, ref blShuttingDown);
                return Files;
            }
            else
            {
                return null;
            }
        }
        
        /// <summary>
        /// Recursive algorithm to get all files in a directory and sub directories
        /// </summary>
        /// <param name="root"></param>
        /// <param name="AllFiles"></param>
        /// <param name="blShuttingDown"></param>
        private static void WalkDirectoryTree(System.IO.DirectoryInfo root, ref System.Collections.Generic.List<System.IO.FileInfo>  AllFiles, ref bool blShuttingDown)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;
            EventLog _evt = new EventLog();
 
            // First, process all the files directly under this folder 
            try
            {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater 
            // than the application provides. 
            catch (UnauthorizedAccessException e)
            {
                // This code just writes out the message and continues to recurse. 
                // You may decide to do something different here. For example, you 
                // can try to elevate your privileges and access the file again.
                _evt.WriteEntry(e.Message);
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                _evt.WriteEntry(e.Message);
            }
 
            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    if (blShuttingDown)
                    {
                        _evt.WriteEntry("Shutting Down, about to walk: " + fi.FullName);
                        return;
                    }
                    // In this example, we only access the existing FileInfo object. If we 
                    // want to open, delete or modify the file, then 
                    // a try-catch block is required here to handle the case 
                    // where the file has been deleted since the call to TraverseTree().

                    //Console.WriteLine(fi.FullName);
                    RefreshFileInfo(fi);
                    AllFiles.Add(fi);
                }
 
                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();
 
                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    if (blShuttingDown)
                    {
                        _evt.WriteEntry("Shutting Down, about to walk directory: " + dirInfo.FullName);
                        return;
                    }
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo, ref AllFiles, ref blShuttingDown);
                }
            }
        }
	}

		///
		/// Synchronization Folder Class
		/// 
 
	public class SyncFolder
	{
		#region "Variables"
 
		public System.Collections.Generic.List<System.IO.FileInfo> AllFiles = new System.Collections.Generic.List<System.IO.FileInfo>();
 
		///
		/// Event Log Class
		///
 
		//private static Event_Log _evt;
 
		#endregion
	 
		#region "Properties"
 
		private bool _enabled = false;
		public bool Enabled
		{
			get
			{
				return _enabled;
			}
 
			set
			{
				_enabled = value;
			}
		}
 
		public static string _sourceFolder = "";
		public string SourceFolder
		{
			get
			{
				return _sourceFolder;
			}
			set
			{
				_sourceFolder = value;
			}
		}
 
		public static string _destinationFolder = "";
		public string DestinationFolder
		{
			get
			{
				return _destinationFolder;
			}
			set
			{
				_destinationFolder = value;
			}
		}
 
		public FileSyncReplicaOptions _fileSyncReplicaOption = FileSyncReplicaOptions.OneWay;
		public FileSyncReplicaOptions FileSyncReplicaOption
		{
			get
			{
				return _fileSyncReplicaOption;
			}
			set
			{
				_fileSyncReplicaOption = value;
			}
		}
 
		private bool _fileSyncReset = false;
		public bool FileSyncReset
		{
			get
			{
				return _fileSyncReset;
			}
 
			set
			{
				_fileSyncReset = value;
			}
		}
 
		public SyncDirectionOrder _folderSyncDirectionOrder = SyncDirectionOrder.Upload;
		public SyncDirectionOrder FolderSyncDirectionOrder
		{
			get
			{
				return _folderSyncDirectionOrder;
			}
			set
			{
				_folderSyncDirectionOrder = value;
			}
		}
	 
		public ConflictResolutionPolicy _defaultConflictResolutionPolicy = ConflictResolutionPolicy.SourceWins;
		public ConflictResolutionPolicy DefaultConflictResolutionPolicy
		{
			get
			{
				return _defaultConflictResolutionPolicy;
			}
			set
			{
				_defaultConflictResolutionPolicy = value;
			}
		}
		#endregion
 
		#region "Methods"
 
		///
		/// Sychronization Folder Contructor for reuse
		///
 
		private void init_SyncFolder()
		{
			AllFiles = new System.Collections.Generic.List(); 
		}	
 
		///
		/// Sychronization Folder Contructor
		///
 
		public SyncFolder()
		{
			init_SyncFolder();
		}
 
		///
		/// Sychronization Folder Contructor
		///
 
		public SyncFolder(DataRow row)
		{
			init_SyncFolder();
 
			Enabled = Common.FixNullbool(row["Enabled"]);
			SourceFolder = Common.FixNullstring(row["SourceFolder"]);
			DestinationFolder = Common.FixNullstring(row["DestinationFolder"]);
			try
			{
				FileSyncReplicaOption = (FileSyncReplicaOptions)System.Enum.Parse(typeof(FileSyncReplicaOptions), Common.FixNullstring(row["FileSyncReplicaOption"]));
			}
			catch (Exception)
			{
				FileSyncReplicaOption = FileSyncReplicaOptions.OneWay;
			}
			FileSyncReset = Common.FixNullbool(row["FileSyncReset"]);
			try
			{	
				FolderSyncDirectionOrder = (SyncDirectionOrder)System.Enum.Parse(typeof(SyncDirectionOrder), Common.FixNullstring(row["FolderSyncDirectionOrder"]));
			}
			catch (Exception)
			{
				FolderSyncDirectionOrder = SyncDirectionOrder.Upload;
			}
			try
			{
				DefaultConflictResolutionPolicy = (ConflictResolutionPolicy)System.Enum.Parse(typeof(ConflictResolutionPolicy), Common.FixNullstring(row["DefaultConflictResolutionPolicy"]));
			}
			catch (Exception)
			{ 
				DefaultConflictResolutionPolicy = ConflictResolutionPolicy.SourceWins;
			}
		}
 
		///
		/// Synchronization Folder Class Destructor
		///
 
		~SyncFolder()
		{
			AllFiles.Clear();
			AllFiles = null;
			//_evt = null;
		}
 
		///
		/// Initializes the synchronization config table
		///
 
		public static DataTable init_dtSyncConfig()
		{
			DataTable dtSyncConfig;
			dtSyncConfig = new DataTable("SyncConfig");
 
			//Create Primary Key Column
			DataColumn dcID = new DataColumn("ID", typeof(Int32));
			dcID.AllowDBNull = false;
			dcID.Unique = true;
			dcID.AutoIncrement = true;
			dcID.AutoIncrementSeed = 1;
			dcID.AutoIncrementStep = 1;
 
			//Assign Primary Key
			DataColumn[] columns = new DataColumn[1];
			dtSyncConfig.Columns.Add(dcID);
			columns[0] = dtSyncConfig.Columns["ID"];
			dtSyncConfig.PrimaryKey = columns;
 
			//Create Columns
			dtSyncConfig.Columns.Add(new DataColumn("Enabled", typeof(String)));
			dtSyncConfig.Columns.Add(new DataColumn("SourceFolder", typeof(String)));
			dtSyncConfig.Columns.Add(new DataColumn("DestinationFolder", typeof(String)));
			dtSyncConfig.Columns.Add(new DataColumn("FileSyncReplicaOption", typeof(String)));
			dtSyncConfig.Columns.Add(new DataColumn("FileSyncReset", typeof(String)));
			dtSyncConfig.Columns.Add(new DataColumn("FolderSyncDirectionOrder", typeof(String)));
			dtSyncConfig.Columns.Add(new DataColumn("DefaultConflictResolutionPolicy", typeof(String)));
 
			dtSyncConfig.Columns["Enabled"].DefaultValue = "true";
			dtSyncConfig.Columns["FileSyncReplicaOption"].DefaultValue = "OneWay";
			dtSyncConfig.Columns["FileSyncReset"].DefaultValue = "false";
			dtSyncConfig.Columns["FolderSyncDirectionOrder"].DefaultValue = "Upload";
			dtSyncConfig.Columns["DefaultConflictResolutionPolicy"].DefaultValue = "SourceWins";
			return dtSyncConfig;
		}
 
		///
		/// Synchronizes two folders with the specified options of this class
		///
 
		public void ExecuteSyncFolder(ref bool blShuttingDown)
		{ 
			try
			{
				if (Enabled)
				{
					if (string.IsNullOrEmpty(SourceFolder) || string.IsNullOrEmpty(DestinationFolder) || !Directory.Exists(SourceFolder) || !Directory.Exists(DestinationFolder))
					{
						//_evt.PublishInfo(new Exception("invalid source directory path 1 o//r invalid destination directory path 2"));
						return;
					}
	 
					// Set options for the synchronization session. In this case, options specify
					// that the application will explicitly call FileSyncProvider.DetectChanges, and
					// that items should be moved to the Recycle Bin instead of being permanently deleted.
					FileSyncOptions options = FileSyncOptions.ExplicitDetectChanges |
					FileSyncOptions.RecycleDeletedFiles | FileSyncOptions.RecyclePreviousFileOnUpdates |
					FileSyncOptions.RecycleConflictLoserFiles;
 
					// Create a filter that excludes all *.lnk files. The same filter should be used
					// by both providers.
					FileSyncScopeFilter filter = new FileSyncScopeFilter();
						filter.FileNameExcludes.Add("*.lnk");
						filter.FileNameExcludes.Add("File.ID");
					//filter.FileNameExcludes.Add("*.7z");
 
					AllFiles.Clear();
					AllFiles = Common.WalkDirectory(DestinationFolder, ref blShuttingDown);
					//Exclude the compressed files on the destination without .7z extension on the source!!
					foreach (System.IO.FileInfo file1 in AllFiles)
					{
						//Skip over already compressed files
						if (file1.Extension.ToLower() == ".7z")
						{
							string strDFile = file1.Name.Substring(0, file1.Name.Length - 3);
							filter.FileNameExcludes.Add(strDFile);
						}	
					}
 
					//Reset Synchronization so that files deleted in the destination are resynchronized
					if (FileSyncReset)
					{
						if (File.Exists(SourceFolder + "\\filesync.metadata"))
						{
							File.Delete(SourceFolder + "\\filesync.metadata");
						}
						if (File.Exists(DestinationFolder + "\\filesync.metadata"))
						{
							File.Delete(DestinationFolder + "\\filesync.metadata");
						}
					}
 
					// Explicitly detect changes on both replicas before syncyhronization occurs.
					// This avoids two change detection passes for the bidirectional synchronization
					// that we will perform.
					DetectChangesOnFileSystemReplica(
					SourceFolder, filter, options);
					DetectChangesOnFileSystemReplica(
					DestinationFolder, filter, options);
 
					// Synchronize the replicas in one directions. In the first session replica 1 is
					// the source. The third parameter
					// (the filter value) is null because the filter is specified in DetectChangesOnFileSystemReplica().
					SyncFileSystemReplicasOneWay(SourceFolder, DestinationFolder, null, options, FolderSyncDirectionOrder, DefaultConflictResolutionPolicy);
	 
					//If two way sync then reverse the source and destination
					if (FileSyncReplicaOption == FileSyncReplicaOptions.TwoWay)
					{
						SyncFileSystemReplicasOneWay(DestinationFolder, SourceFolder, null, options, FolderSyncDirectionOrder, DefaultConflictResolutionPolicy);
					}
				}
			}
			catch (Exception e)
			{
				//_evt.Publish(new Exception("Exception from File Sync Provider:\n"// + e.ToString()));
			}
		}
 
		///
		/// Create a provider, and detect changes on the replica that the provider
		/// represents.
		///
 
		public static void DetectChangesOnFileSystemReplica(
			string replicaRootPath,
			FileSyncScopeFilter filter, FileSyncOptions options)
		{
			FileSyncProvider provider = null;
 
			try
			{
				//replicaRootPath
				SyncId ssyncId = GetSyncID(replicaRootPath + "\\File.ID");
				provider = new FileSyncProvider(ssyncId.GetGuidId(), replicaRootPath, filter, options);
				provider.DetectChanges();
			}
			finally
			{
				// Release resources.
				if (provider != null)
				provider.Dispose();
			}
		}
 
		///
		/// Creates a file in the path passed that stores a GUID the replicaid for synchronization if the file does not exist otherwise reads the guid from the file and returns the replicaid
		///
 
		private static SyncId GetSyncID(string syncFilePath)
		{
			Guid guid;
			SyncId replicaID = null;
			if (!File.Exists(syncFilePath)) //The ID file doesn't exist.
			//Create the file and store the guid which is used to
			//instantiate the instance of the SyncId.
			{
				guid = Guid.NewGuid();
				replicaID = new SyncId(guid);
				FileStream fs = File.Open(syncFilePath, FileMode.Create);
				StreamWriter sw = new StreamWriter(fs);
				sw.WriteLine(guid.ToString());
				sw.Close();
				fs.Close();
			}
			else
			{
				FileStream fs = File.Open(syncFilePath, FileMode.Open);
				StreamReader sr = new StreamReader(fs);
				string guidString = sr.ReadLine();
				guid = new Guid(guidString);
				replicaID = new SyncId(guid);
				sr.Close();
				fs.Close();
			}
			return (replicaID);
		}
 
		///
		/// One Way Folder Synchronization Method
		///
 	
		public static void SyncFileSystemReplicasOneWay(string sourceReplicaRootPath, string destinationReplicaRootPath
			, FileSyncScopeFilter filter, FileSyncOptions options, SyncDirectionOrder FolderSyncDirectionOrder, ConflictResolutionPolicy DefaultConflictResolutionPolicy)
		{
			FileSyncProvider sourceProvider = null;
			FileSyncProvider destinationProvider = null;
	 
			try
			{
				SyncId sourceId = GetSyncID(sourceReplicaRootPath + "\\File.ID");
				SyncId destId = GetSyncID(destinationReplicaRootPath + "\\File.ID");
 
				// Instantiate source and destination providers, with a null filter (the filter
				// was specified in DetectChangesOnFileSystemReplica()), and options for both.
				sourceProvider = new FileSyncProvider(sourceId.GetGuidId(), sourceReplicaRootPath, filter, options);
				destinationProvider = new FileSyncProvider(destId.GetGuidId(), destinationReplicaRootPath, filter, options);
 
				// Register event handlers so that we can write information
				// to the console.
 
				/*//Additional Events
				provider.DestinationCallbacks.FullEnumerationNeeded += this.FullEnumerationNeededCallback;
				destinationProvider.ItemChangeSkipped += this.ItemChangeSkippedCallback;
				destinationProvider.ItemChanging += this.ItemChangingCallback;
				destinationProvider.ItemConstraint += this.ItemConstraintCallback;
				destinationProvider.ProgressChanged += this.ProgressChangedCallback;
				*/
	 
				//Document Changes if needed
				//destinationProvider.AppliedChange +=
				// new EventHandler(OnAppliedChange);
	 
				destinationProvider.SkippedChange +=
					new EventHandler(OnSkippedChange);
				destinationProvider.Configuration.ConflictResolutionPolicy = DefaultConflictResolutionPolicy;
				// Use SyncCallbacks for conflicting items.
				SyncCallbacks destinationCallbacks = destinationProvider.DestinationCallbacks;
					destinationCallbacks.ItemConflicting += new EventHandler(OnItemConflicting);
	 
				SyncOrchestrator agent = new SyncOrchestrator();
				agent.LocalProvider = sourceProvider;
				agent.RemoteProvider = destinationProvider;
	 
				agent.Direction = FolderSyncDirectionOrder; // Upload changes from the source to the destination.
	 
				//_evt.PublishInfo(new Exception("Synchronizing changes to replica //from: " + sourceReplicaRootPath + " to: " + destinationProvider.R//ootDirectoryPath));
				agent.Synchronize();
			}
			finally
			{
				// Release resources.
				if (sourceProvider != null) sourceProvider.Dispose();
				if (destinationProvider != null) destinationProvider.Dispose();
			}
		}
 
		///
		/// Provide information about files that were affected by the synchronization session.
		///
 
		public static void OnAppliedChange(object sender, AppliedChangeEventArgs args)
		{
			switch (args.ChangeType)
			{
				case ChangeType.Create:
					//_evt.PublishInfo(new Exception("-- Applied CREATE for file " + ar//gs.NewFilePath));
				break;
				case ChangeType.Delete:
					//_evt.PublishInfo(new Exception("-- Applied DELETE for file " + ar//gs.OldFilePath));
				break;
				case ChangeType.Update:
					//_evt.PublishInfo(new Exception("-- Applied OVERWRITE for file " +// args.OldFilePath));
				break;
				case ChangeType.Rename:
					//_evt.PublishInfo(new Exception("-- Applied RENAME for file " + ar//gs.OldFilePath + " as " + args.NewFilePath));
				break;
			}
		}
 
		///
		/// Provide error information for any changes that were skipped.
		///
 
		public static void OnSkippedChange(object sender, SkippedChangeEventArgs args)
		{
			string strMessage = "";
			strMessage = "-- Skipped applying " + args.ChangeType.ToString().ToUpper()
				+ " for " + (!string.IsNullOrEmpty(args.CurrentFilePath) ?
			args.CurrentFilePath : args.NewFilePath) + " due to error";
	 
			if (args.Exception != null)
			strMessage += " [" + args.Exception.Message + "]";
			//_evt.PublishInfo(new Exception(strMessage));
		}
 
		/// By default, conflicts are resolved in favor of the last writer. In this example,
		/// the change from the source in the first session (replica 1), will always
		/// win the conflict.

		public static void OnItemConflicting(object sender, ItemConflictingEventArgs args)
		{
			string strMessage = "";
			args.SetResolutionAction(ConflictResolutionAction.SourceWins);
	 
			strMessage = "-- Concurrency conflict detected for item " + args.DestinationChange.ItemId.ToString();
			//_evt.PublishInfo(new Exception(strMessage));
		}
		#endregion
	}
}