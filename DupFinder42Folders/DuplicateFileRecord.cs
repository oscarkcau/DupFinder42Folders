using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileType = DupFinder42Folders.EnumFileType;

namespace DupFinder42Folders
{
    class DuplicateFileRecord
    {
        #region public properties
        public string Key { get; private set; }
        public string Filename { get; private set; }
        public List<string> FileList1 { get; private set; } = null;
        public List<string> FileList2{ get; private set; } = null;
        public IEnumerable<PathTreeViewItem> AllFileList { get; private set; } = null;
        public FileType FileType { get; private set; } = FileType.Unknown;
        #endregion

        #region constructor
        public DuplicateFileRecord(string key, List<string> list1, List<string> list2)
        {
            // initialize basic fields
            Key = key;
            Filename = Path.GetFileName(list1[0]);
            FileList1 = list1;
            FileList2 = list2;

            // initialize list of dublicate file paths
            var tempList1 = list1.Select(f => new PathTreeViewItem(0, f));
            var tempList2 = list2.Select(f => new PathTreeViewItem(1, f));
            AllFileList = tempList1.Concat(tempList2);

            // initialize file type for displaying corresponding icons
            string extension = Path.GetExtension(FileList1[0]).ToLower();
            FileType = FileExtensionHelper.GetFileType(extension);
        }
        #endregion
    }

    class PathTreeViewItem
    {
        #region public properties
        public int ListIndex { get; private set; }
        public string Path { get; private set; }
        #endregion

        #region constructor
        public PathTreeViewItem(int listIndex, string path)
        {
            ListIndex = listIndex;
            Path = path;
        }
        #endregion
    }
}
