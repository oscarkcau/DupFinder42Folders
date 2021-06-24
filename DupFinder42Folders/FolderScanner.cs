using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DupFinder42Folders
{
    class FolderScanner
    {
		#region private fields
		MD5 md5;
		readonly EnumSearchCriteria criteria;
		readonly long? lowerBound, upperBound;
		#endregion

		#region public properties
		public string SourcePath { get; private set; }
		public bool IsScanning { get; private set; } = false;
		public bool IsCancelled { get; private set; } = false;
		public string CurrentScanningFolder { get; private set; } = "";
		public long ScannedFileCount { get; private set; } = 0;
		public List<string> UnaccessibleFolders { get; private set; } = new List<string>();
		public List<string> UnaccessibleFiles { get; private set; } = new List<string>();
		public Dictionary<string, List<string>> FileRecords { get; private set; } = new Dictionary<string, List<string>>();
        #endregion

        #region constructor
        public FolderScanner(string folderPath, EnumSearchCriteria criteria, long? lowerBound = null, long? upperBound = null)
		{
			SourcePath = folderPath;
			this.criteria = criteria;
			this.lowerBound = lowerBound;
			this.upperBound = upperBound;
		}
		#endregion

		#region public methods
		public async Task Scan(CancellationToken cancelToken)
		{
			// ensure folder exists
			if (Directory.Exists(SourcePath) == false)
			{
				throw new DirectoryNotFoundException();
			}

			// set scanning flag
			IsScanning = true;

			// clear fields
			UnaccessibleFolders.Clear();
			UnaccessibleFiles.Clear();
			FileRecords.Clear();
			ScannedFileCount = 0;
			CurrentScanningFolder = "";

			// initialize md5 object
			md5 = MD5.Create();

			// scan folder in new thread
			await Task.Run(() => ScanFolder(SourcePath, cancelToken), cancelToken);
			
			// reset flag and fields
			IsScanning = false;
			CurrentScanningFolder = "";
			md5.Dispose();
		}
		#endregion

		#region private methods
		private void ScanFolder(string rootFolder, CancellationToken cancelToken)
		{
			// initialize the stack of folders to process
			Stack<string> foldersToProcess = new Stack<string>();
			foldersToProcess.Push(rootFolder);

			// process all folders in stack
			while (foldersToProcess.Count > 0)
			{
				// get folder name at the top of stack
				string folderName = foldersToProcess.Pop();
				CurrentScanningFolder = folderName;
				Debug.Print(folderName);

				try
				{
					// scan all files in current folder
					foreach (var filename in Directory.EnumerateFiles(folderName))
					{
						cancelToken.ThrowIfCancellationRequested();
						ScanFile(filename);
					}

					// add subfolders to stack
					foreach (var subFolderName in Directory.EnumerateDirectories(folderName))
					{
						cancelToken.ThrowIfCancellationRequested();
						foldersToProcess.Push(subFolderName);
					}
				}
				// terminate scanning when cancelation is requested
				catch (OperationCanceledException)
				{
					IsCancelled = true;
					return;
				}
				// record names of any unaccessable folder
				catch (Exception)
                {
					UnaccessibleFolders.Add(folderName);
					Debug.Print("cannot access folder: " + folderName);
				}
			}
		}
		private void ScanFile(string path)
		{
			try
			{
				// initial helper objects
				StringBuilder sb = new StringBuilder();
				FileInfo info = null;
				if (criteria.HasFlag(EnumSearchCriteria.Size) ||
					criteria.HasFlag(EnumSearchCriteria.LastModifiedDate) ||
					lowerBound.HasValue || upperBound.HasValue)
				{
					info = new FileInfo(path);
				}

				// skip if file size does not within given range
				if (lowerBound.HasValue && info.Length < lowerBound) return;
				if (upperBound.HasValue && info.Length > upperBound) return;

				// add filename to key if it is used as search criteria
				if (criteria.HasFlag(EnumSearchCriteria.Name))
				{
					sb.Append(Path.GetFileName(path));
				}

				// add file size to key if it is used as search criteria
				if (criteria.HasFlag(EnumSearchCriteria.Size))
				{
					sb.Append(info.Length);
				}

				// add file last modifity date to key if it is used as search criteria
				if (criteria.HasFlag(EnumSearchCriteria.LastModifiedDate))
				{
					sb.Append(info.LastWriteTimeUtc);
				}

				// add file MD5 encoding to key if it is used as search criteria
				if (criteria.HasFlag(EnumSearchCriteria.Content))
                {
					// open file
					using (var stream = File.OpenRead(path))
					{
						// compute md5 and convert it to string
						byte[] hash = md5.ComputeHash(stream);
						string asciiHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
						sb.Append(asciiHash);
					}
				}

				// add filename to Dictionary
				string key = sb.ToString();
				if (FileRecords.ContainsKey(key))
				{
					FileRecords[key].Add(path);
				}
				else
				{
					FileRecords.Add(key, new List<string> { path });
				}

				// increace count
				ScannedFileCount++;
			}
			// record names of any unaccessable folder
			catch (Exception)
			{
				UnaccessibleFiles.Add(path);
				Debug.Print("cannot access file: " + path);
			}
		}
		#endregion
	}
}
