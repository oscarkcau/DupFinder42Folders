using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Timers;
using System.Collections.ObjectModel;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;

namespace DupFinder42Folders
{
    #region Enum types
    internal enum EnumFileType { Image, Audio, Video, PDF, Word, Excel, PowerPoint, Compressed, DiskImage, Code, Unknown };
    [Flags] internal enum EnumSearchCriteria { None = 0, Name = 1, Size = 2, Content = 4, LastModifiedDate = 8 };
    internal enum EnumFileSizeUnit { Bytes = 1, KBs = 1024, MBs = 1048576, GBs = 1073741824 };
    internal enum EnumActionSourceFolder { One, Two };
    internal enum EnumActionType { MoveToRecycleBin, Delete, Copy, Move };
    #endregion

    class MainViewModel : INotifyPropertyChanged
    {
        #region private fields
        readonly System.Timers.Timer timer = null;
        string _sourceFolder1, _sourceFolder2, _lastErrorMessage, _lastErrorPropertyName, _progressMessage, _targetFolder;
        CancellationTokenSource scannerCancelTokenSource;
        FolderScanner folderScanner1, folderScanner2;
        EnumSearchCriteria _searchCriteria = EnumSearchCriteria.Content;
        EnumActionSourceFolder _actionSourceFolder = EnumActionSourceFolder.One;
        EnumActionType _actionType = EnumActionType.MoveToRecycleBin;
        bool _ignoreZeroByteFiles = true, _excludeFilesSamllerThan = false, _excludeFilesLargerThan = false;
        int _excludeFileSizeLowerBound = 0, _excludeFileSizeUpperBound = 0;
        EnumFileSizeUnit _excludeFileSizeLowerBoundUnit = EnumFileSizeUnit.MBs;
        EnumFileSizeUnit _excludeFileSizeUpperBoundUnit = EnumFileSizeUnit.MBs;
        bool _deleteEmptySubfolders = false, _keepFolderStructure = false, _overwriteExistingFiles = false;
        #endregion

        #region public properties
        public string LastErrorMessage { get => _lastErrorMessage; set => SetField(ref _lastErrorMessage, value); }
        public string LastErrorPropertyName { get => _lastErrorPropertyName; set => SetField(ref _lastErrorPropertyName, value); }
        public string SourceFolder1 { get => _sourceFolder1; set => SetField(ref _sourceFolder1, value); }
        public string SourceFolder2 { get => _sourceFolder2; set => SetField(ref _sourceFolder2, value); }
        public EnumSearchCriteria SearchCriteria { get => _searchCriteria; set => SetField(ref _searchCriteria, value); }
        public bool ShouldIgnoreZeroByteFiles { get => _ignoreZeroByteFiles; set => SetField(ref _ignoreZeroByteFiles, value); }
        public bool ShouldExcludeFilesSamllerThan { get => _excludeFilesSamllerThan; set => SetField(ref _excludeFilesSamllerThan, value); }
        public bool ShouldExcludeFilesLargerThan { get => _excludeFilesLargerThan; set => SetField(ref _excludeFilesLargerThan, value); }
        public int ExcludeFileSizeLowerBound { get => _excludeFileSizeLowerBound; set => SetField(ref _excludeFileSizeLowerBound, value); }
        public int ExcludeFileSizeUpperBound { get => _excludeFileSizeUpperBound; set => SetField(ref _excludeFileSizeUpperBound, value); }
        public EnumFileSizeUnit ExcludeFileSizeLowerBoundUnit { get => _excludeFileSizeLowerBoundUnit; set => SetField(ref _excludeFileSizeLowerBoundUnit, value); }
        public EnumFileSizeUnit ExcludeFileSizeUpperBoundUnit { get => _excludeFileSizeUpperBoundUnit; set => SetField(ref _excludeFileSizeUpperBoundUnit, value); }
        public string ProgressMessage { get => _progressMessage; set => SetField(ref _progressMessage, value); }
        public ObservableCollection<DuplicateFileRecord> DuplicateFileRecords { get; } = new ObservableCollection<DuplicateFileRecord>();
        public ObservableCollection<string> UnaccessibleFolders { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> UnaccessibleFiles { get; } = new ObservableCollection<string>();
        public EnumActionSourceFolder ActionSourceFolder { get => _actionSourceFolder; set => SetField(ref _actionSourceFolder, value); }
        public EnumActionType ActionType { get => _actionType; set => SetField(ref _actionType, value); }
        public string TargetFolder { get => _targetFolder; set => SetField(ref _targetFolder, value); }
        public bool ShouldDeleteEmptySubfolders { get => _deleteEmptySubfolders; set => SetField(ref _deleteEmptySubfolders, value); }
        public bool ShouldKeepFolderStructure { get => _keepFolderStructure; set => SetField(ref _keepFolderStructure, value); }
        public bool ShouldOverwriteExistingFiles { get => _overwriteExistingFiles; set => SetField(ref _overwriteExistingFiles, value); }
        public ObservableCollection<string> ActionFailedEntities { get; } = new ObservableCollection<string>();

        #endregion

        #region constructor
        public MainViewModel()
        {
            // initialize timer
            timer = new System.Timers.Timer(200);
            timer.Stop();
            timer.Elapsed += Timer_Elapsed;
        }
        #endregion

        #region event handlers
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // update progress message based on which folders is being scanning
            if (folderScanner1 != null && folderScanner1.IsScanning)
            {
                ProgressMessage =
                    folderScanner1.ScannedFileCount +
                    " files scanned, now scanning: " +
                    folderScanner1.CurrentScanningFolder;
            }
            else if (folderScanner2 != null && folderScanner2.IsScanning)
            {
                ProgressMessage =
                    folderScanner1.ScannedFileCount + folderScanner2.ScannedFileCount +
                    " files scanned, now scanning: " +
                    folderScanner2.CurrentScanningFolder;
            }
        }
        #endregion

        #region public methods
        public bool CheckSourceFolders()
        {
            // check if fields is not empty
            if (string.IsNullOrWhiteSpace(SourceFolder1))
            {
                LastErrorMessage = Properties.ResourceErrorMessages.FieldCannotBeEmpty;
                LastErrorPropertyName = nameof(SourceFolder1);
                return false;
            }
            if (string.IsNullOrWhiteSpace(SourceFolder2))
            {
                LastErrorMessage = Properties.ResourceErrorMessages.FieldCannotBeEmpty;
                LastErrorPropertyName = nameof(SourceFolder2);
                return false;
            }

            // check if folders exist
            if (Directory.Exists(SourceFolder1) == false)
            {
                LastErrorMessage = Properties.ResourceErrorMessages.FolderNotFound;
                LastErrorPropertyName = nameof(SourceFolder1);
                return false;
            }
            if (Directory.Exists(SourceFolder2) == false)
            {
                LastErrorMessage = Properties.ResourceErrorMessages.FolderNotFound;
                LastErrorPropertyName = nameof(SourceFolder2);
                return false;
            }

            // check if one folder is NOT a subfolder of another folder
            string f1 = SourceFolder1.EndsWith("\\") || SourceFolder1.EndsWith("/") ? SourceFolder1 : SourceFolder1 + "\\";
            string f2 = SourceFolder2.EndsWith("\\") || SourceFolder2.EndsWith("/") ? SourceFolder2 : SourceFolder2 + "\\";
            Uri u1 = new Uri(f1);
            Uri u2 = new Uri(f2);
            if (u1 == u2)
            {
                LastErrorMessage = Properties.ResourceErrorMessages.BothFoldersCannotBeSame;
                LastErrorPropertyName = nameof(SourceFolder1);
                return false;
            }
            if (u2.IsBaseOf(u1))
            {
                LastErrorMessage = Properties.ResourceErrorMessages.FolderCannotBeSubfolder;
                LastErrorPropertyName = nameof(SourceFolder1);
                return false;
            }
            if (u1.IsBaseOf(u2))
            {
                LastErrorMessage = Properties.ResourceErrorMessages.FolderCannotBeSubfolder;
                LastErrorPropertyName = nameof(SourceFolder2);
                return false;
            }
            return true;
        }
        public bool CheckSearchOptions()
        {
            // check if at least one search criteria is selected
            if (SearchCriteria.Equals(EnumSearchCriteria.None))
            {
                LastErrorMessage = Properties.ResourceErrorMessages.SearchCriteriaNotSpecified;
                LastErrorPropertyName = nameof(SearchCriteria);
                return false;
            }

            // check if file size thresholds of excluded files are well defined
            if (ShouldExcludeFilesSamllerThan && ShouldExcludeFilesLargerThan)
            {
                long lowerBound = ExcludeFileSizeLowerBound * (long)ExcludeFileSizeLowerBoundUnit;
                long upperBound = ExcludeFileSizeUpperBound * (long)ExcludeFileSizeUpperBoundUnit;

                if(lowerBound >= upperBound)
                {
                    LastErrorMessage = Properties.ResourceErrorMessages.ExcludeFileSizeError;
                    LastErrorPropertyName = nameof(ExcludeFileSizeLowerBound);
                    return false;
                }
            }

            return true;
        }
        public bool CheckTargetFolder()
        {
            // target folder is not used in delete or move to recycle bin actions
            if (ActionType == EnumActionType.Delete ||
                ActionType == EnumActionType.MoveToRecycleBin)
                return true;

            // check if fields is not empty
            if (string.IsNullOrWhiteSpace(TargetFolder))
            {
                LastErrorMessage = Properties.ResourceErrorMessages.FieldCannotBeEmpty;
                LastErrorPropertyName = nameof(TargetFolder);
                return false;
            }

            // check if folders exist
            if (Directory.Exists(TargetFolder) == false)
            {
                LastErrorMessage = Properties.ResourceErrorMessages.FolderNotFound;
                LastErrorPropertyName = nameof(TargetFolder);
                return false;
            }

            // check if target folder is NOT a subfolder of any source folder
            string f1 = SourceFolder1.EndsWith("\\") || SourceFolder1.EndsWith("/") ? SourceFolder1 : SourceFolder1 + Path.DirectorySeparatorChar;
            string f2 = SourceFolder2.EndsWith("\\") || SourceFolder2.EndsWith("/") ? SourceFolder2 : SourceFolder2 + Path.DirectorySeparatorChar;
            string f = TargetFolder.EndsWith("\\") || TargetFolder.EndsWith("/") ? TargetFolder : TargetFolder + Path.DirectorySeparatorChar;
            Uri u1 = new Uri(f1);
            Uri u2 = new Uri(f2);
            Uri u = new Uri(f);
            if (u == u1 || u == u2)
            {
                LastErrorMessage = Properties.ResourceErrorMessages.TargetFolderCannotBeSame;
                LastErrorPropertyName = nameof(TargetFolder);
                return false;
            }
            if (u1.IsBaseOf(u))
            {
                LastErrorMessage = Properties.ResourceErrorMessages.TargetFolderCannotBeSubfolder;
                LastErrorPropertyName = nameof(TargetFolder);
                return false;
            }
            if (u2.IsBaseOf(u))
            {
                LastErrorMessage = Properties.ResourceErrorMessages.TargetFolderCannotBeSubfolder;
                LastErrorPropertyName = nameof(TargetFolder);
                return false;
            }

            return true;
        }
        public async Task ScanFolders()
        {
            // initialize cancellation token
            scannerCancelTokenSource = new CancellationTokenSource();
            var cancelToken = scannerCancelTokenSource.Token;

            // compute file size threshold if file size constraint is used
            long? lowerBound = null, upperBound = null;
            if (ShouldIgnoreZeroByteFiles) lowerBound = 1;
            if (ShouldExcludeFilesSamllerThan) lowerBound = ExcludeFileSizeLowerBound * (long)ExcludeFileSizeLowerBoundUnit;
            if (ShouldExcludeFilesLargerThan) upperBound = ExcludeFileSizeUpperBound * (long)ExcludeFileSizeUpperBoundUnit;

            try
            {
                // scan both source folders
                timer.Start();
                this.folderScanner1 = new FolderScanner(SourceFolder1, SearchCriteria, lowerBound, upperBound);
                this.folderScanner2 = new FolderScanner(SourceFolder2, SearchCriteria, lowerBound, upperBound);
                await folderScanner1.Scan(cancelToken);
                await folderScanner2.Scan(cancelToken);
                timer.Stop();

                cancelToken.ThrowIfCancellationRequested();

                // find duglicate files
                ProgressMessage = "Finding dublicate files...";
                FindDublicateFiles();
                CollectUnaccessibleEntries();

                ProgressMessage = "Scanning finished";
                
                /*
                ProgressMessage =
                    "Sanning finished, " +
                    (sourceScanner.ScannedFileCount + targetScanner.ScannedFileCount) +
                    " files scanned, " +
                    DuplicateFileRecords.Count +
                    " dublicated file group found";
                */
            }
            catch (OperationCanceledException)
            {
                ProgressMessage = "Scanning cancalled";
            }
            finally
            {
                // remember to clear calcellation token
                scannerCancelTokenSource = null;
            }
        }
        public void CancelScanningFolders()
        {
            // if token available, cancel associated task
            scannerCancelTokenSource?.Cancel();
        }
        public void PreformAction()
        {
            ActionFailedEntities.Clear();

            // call corresponding action function
            switch (ActionType)
            {
                case EnumActionType.Delete:
                    DeleteDuplicateFiles();
                    break;
                case EnumActionType.MoveToRecycleBin:
                    MoveDuplicateFilesToRecycleBin();
                    break;
                case EnumActionType.Copy:
                    CopyeDuplicateFiles();
                    break;
                case EnumActionType.Move:
                    MoveeDuplicateFiles();
                    break;
            }

            // delete empty subfolders under source folder(s) if required
            if (ShouldDeleteEmptySubfolders)
            {
                DeleteEmptySubfolders();
            }
        }

        #endregion

        #region private methods
        private void FindDublicateFiles()
        {
            // clear previous search result
            DuplicateFileRecords.Clear();

            // match files in two source folders based on their keys
            foreach (var pair1 in folderScanner1.FileRecords)
            {
                if (folderScanner2.FileRecords.ContainsKey(pair1.Key))
                {
                    string key = pair1.Key;
                    List<string> fileList1 = pair1.Value;
                    List<string> fileList2 = folderScanner2.FileRecords[key];
                    DuplicateFileRecord record = new DuplicateFileRecord(key, fileList1, fileList2);
                    DuplicateFileRecords.Add(record);
                }
            }
        }
        private void CollectUnaccessibleEntries()
        {
            UnaccessibleFolders.Clear();
            UnaccessibleFiles.Clear();
            foreach (var f in folderScanner1.UnaccessibleFolders) UnaccessibleFolders.Add(f);
            foreach (var f in folderScanner2.UnaccessibleFolders) UnaccessibleFolders.Add(f);
            foreach (var f in folderScanner1.UnaccessibleFiles) UnaccessibleFiles.Add(f);
            foreach (var f in folderScanner2.UnaccessibleFiles) UnaccessibleFiles.Add(f);
        }
        private IEnumerable<string> GetActionSourceFiles()
        {
            switch (ActionSourceFolder)
            {
                case EnumActionSourceFolder.One:
                    return DuplicateFileRecords.SelectMany(r => r.FileList1);

                case EnumActionSourceFolder.Two:
                    return DuplicateFileRecords.SelectMany(r => r.FileList2);
            }
            return null;
        }
        private string GetActionSourceFolder()
        {
            switch (ActionSourceFolder)
            {
                case EnumActionSourceFolder.One:
                    return SourceFolder1;

                case EnumActionSourceFolder.Two:
                    return SourceFolder2;
            }
            return null;
        }
        private void DeleteDuplicateFiles()
        {
            // delete files permanently from selected source folder
            foreach (var f in GetActionSourceFiles())
            {
                try
                {
                    FileSystem.DeleteFile(
                        f,
                        UIOption.OnlyErrorDialogs,
                        RecycleOption.DeletePermanently
                        );
                }
                catch (Exception)
                {
                    ActionFailedEntities.Add("delete " + f);
                }
            }
        }
        private void MoveDuplicateFilesToRecycleBin()
        {
            // move files to recycle bin from selected source folder
            foreach (var f in GetActionSourceFiles())
            {
                try
                {
                    FileSystem.DeleteFile(
                        f,
                        UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin
                        );
                }
                catch (Exception)
                {
                    ActionFailedEntities.Add("move_to_recycle_bin " + f);
                }
            }
        }
        private void CopyeDuplicateFiles()
        {
            string sourceFolder = GetActionSourceFolder();
            string targetFolder = TargetFolder.EndsWith("\\") || TargetFolder.EndsWith("/") ? TargetFolder : TargetFolder + Path.DirectorySeparatorChar;

            // copy files to new location from selected source folder
            foreach (var f in GetActionSourceFiles())
            {
                string filename = Path.GetFileName(f);
                //string relativePath = Path.GetRelativePath()
                string targetFilename = targetFolder + filename;
                
                try
                {
                    FileSystem.CopyFile(f, targetFilename, ShouldOverwriteExistingFiles);
                }
                catch (IOException)
                {
                    ActionFailedEntities.Add("target file already exists " + f);
                }
                catch (Exception)
                {
                    ActionFailedEntities.Add("copy " + f);
                }
            }
        }
        private void MoveeDuplicateFiles()
        {
            throw new NotImplementedException();
        }
        private void DeleteEmptySubfolders()
        {
            switch (ActionSourceFolder)
            {
                case EnumActionSourceFolder.One:
                    DeleteEmptySubfolders(SourceFolder1);
                    break;

                case EnumActionSourceFolder.Two:
                    DeleteEmptySubfolders(SourceFolder2);
                    break;
            }
        }
        private void DeleteEmptySubfolders(string rootFolder)
        {
            // initialize the stack of folders to process
            Stack<string> orderedFolders = new Stack<string>();
            Stack<string> foldersToProcess = new Stack<string>();
            foldersToProcess.Push(rootFolder);

            // process all folders in stack
            while (foldersToProcess.Count > 0)
            {
                // get folder name at the top of stack
                string folderName = foldersToProcess.Pop();
                orderedFolders.Push(folderName);
                Debug.Print("scanning " + folderName);

                try
                {
                    // add subfolders to stack
                    foreach (var subFolderName in Directory.EnumerateDirectories(folderName))
                    {
                        //cancelToken.ThrowIfCancellationRequested();
                        foldersToProcess.Push(subFolderName);
                    }
                }
                // terminate scanning when cancelation is requested
                catch (OperationCanceledException)
                {
                    //IsCancelled = true;
                    return;
                }
                // record names of any unaccessable folder
                catch (Exception)
                {
                    ActionFailedEntities.Add("delete_folder " + folderName);
                    Debug.Print("cannot access folder: " + folderName);
                }
            }

            while (orderedFolders.Count > 0)
            {
                string folderName = orderedFolders.Pop();
                
                try
                {
                    // delete current folder if it is empty
                    if (!Directory.EnumerateFileSystemEntries(folderName).Any())
                    {
                        FileSystem.DeleteDirectory(
                            folderName,
                            UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin
                            );
                    }
                }
                catch (Exception)
                {
                    ActionFailedEntities.Add("delete_folder " + folderName);
                    Debug.Print("cannot access folder: " + folderName);
                }
            }
        }
        #endregion

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }


}
