using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileType = DupFinder42Folders.EnumFileType;

namespace DupFinder42Folders
{
    class FileExtensionHelper
    {
        static private Dictionary<string, FileType> fileExtensionTypes = new Dictionary<string, FileType>();

        static FileExtensionHelper()
        {
            var s = Properties.Settings.Default;

            SetFileTypes(s.ImageFileExtensions, FileType.Image);
            SetFileTypes(s.AudioFileExtensions, FileType.Audio);
            SetFileTypes(s.VideoFileExtensions, FileType.Video);
            SetFileTypes(s.PdfFileExtensions, FileType.PDF);
            SetFileTypes(s.WordFileExtensions, FileType.Word);
            SetFileTypes(s.ExcelFileExtensions, FileType.Excel);
            SetFileTypes(s.PowerPointFileExtensions, FileType.PowerPoint);
            SetFileTypes(s.DiskImageFileExtensions, FileType.DiskImage);
            SetFileTypes(s.CompressedFileExtensions, FileType.Compressed);
            SetFileTypes(s.CodeFileExtensions, FileType.Code);
        }

        static public FileType GetFileType(string extension)
        {
            string s = extension.Trim().Trim('.');

            if (fileExtensionTypes.ContainsKey(s))
            {
                return fileExtensionTypes[s];
            }

            return FileType.Unknown;
        }

        static private void SetFileTypes(string settingString, FileType type)
        {
            foreach (string ext in settingString.Split(' '))
            {
                if (String.IsNullOrWhiteSpace(ext)) continue;
                if (fileExtensionTypes.ContainsKey(ext)) continue;
                fileExtensionTypes.Add(ext, type);
            }
        }
    }
}
