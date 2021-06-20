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

namespace DupFinder42Folders
{
    class MainViewModel : INotifyPropertyChanged
    {
        internal enum EnumFileType { Image, Audio, Video, PDF, Word, Excel, PowerPoint, Compressed, DiskImage, Code, Unknown };

        #region private fields
        readonly System.Timers.Timer timer = null;
        string _sourceFolder1, _sourceFolder2, _lastErrorMessage, _lastErrorPropertyName, _progressMessage;
        CancellationTokenSource scannerCancelTokenSource;
        FolderScanner folderScanner1, folderScanner2;
        #endregion

        #region public properties
        public string SourceFolder1 { get => _sourceFolder1; set => SetField(ref _sourceFolder1, value); }
        public string SourceFolder2 { get => _sourceFolder2; set => SetField(ref _sourceFolder2, value); }
        public string LastErrorMessage { get => _lastErrorMessage; set => SetField(ref _lastErrorMessage, value); }
        public string LastErrorPropertyName { get => _lastErrorPropertyName; set => SetField(ref _lastErrorPropertyName, value); }
        public string ProgressMessage { get => _progressMessage; set => SetField(ref _progressMessage, value); }
        public ObservableCollection<DuplicatedFileRecord> DuplicatedFileRecords { get; } = new ObservableCollection<DuplicatedFileRecord>();
        public ObservableCollection<string> UnaccessibleFolders { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> UnaccessibleFiles { get; } = new ObservableCollection<string>();

        #endregion

        #region constructor
        public MainViewModel()
        {
            // start timer in a new thread
            timer = new System.Timers.Timer(200);
            timer.Stop();
            timer.Elapsed += Timer_Elapsed;
        }
        #endregion 

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
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
        public async Task ScanFolders()
        {
            scannerCancelTokenSource = new CancellationTokenSource();
            var cancelToken = scannerCancelTokenSource.Token;

            try
            {
                timer.Start();
                this.folderScanner1 = new FolderScanner(SourceFolder1);
                this.folderScanner2 = new FolderScanner(SourceFolder2);
                await folderScanner1.Scan(cancelToken);
                await folderScanner2.Scan(cancelToken);
                timer.Stop();

                cancelToken.ThrowIfCancellationRequested();

                ProgressMessage = "Finding dublicated files...";
                FindDublicatedFiles();
                CollectUnaccessibleEntries();

                ProgressMessage = "Scanning finished";
                /*
                ProgressMessage =
                    "Sanning finished, " +
                    (sourceScanner.ScannedFileCount + targetScanner.ScannedFileCount) +
                    " files scanned, " +
                    DuplicatedFileRecords.Count +
                    " dublicated file group found";
                */
            }
            catch (OperationCanceledException)
            {
                ProgressMessage = "Scanning cancalled";
            }
            finally
            {
                scannerCancelTokenSource = null;
            }
        }
        public void CancelScanningFolders()
        {
            scannerCancelTokenSource?.Cancel();
        }
        #endregion

        #region private methods
        private void FindDublicatedFiles()
        {
            DuplicatedFileRecords.Clear();

            foreach (var pair1 in folderScanner1.FileRecords)
            {
                if (folderScanner2.FileRecords.ContainsKey(pair1.Key))
                {
                    string md5 = pair1.Key;
                    List<string> fileList1 = pair1.Value;
                    List<string> fileList2 = folderScanner2.FileRecords[md5];
                    DuplicatedFileRecord record = new DuplicatedFileRecord(md5, fileList1, fileList2);
                    DuplicatedFileRecords.Add(record);
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
