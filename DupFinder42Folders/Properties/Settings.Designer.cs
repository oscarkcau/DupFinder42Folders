﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DupFinder42Folders.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.8.1.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("png jpg jpeg tif tiff bmp svg webp avif apng ico cur")]
        public string ImageFileExtensions {
            get {
                return ((string)(this["ImageFileExtensions"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("aac aiff alac flac mp3 ogg wav wma")]
        public string AudioFileExtensions {
            get {
                return ((string)(this["AudioFileExtensions"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("webm mkv flv ogv avi wmv mov rm rmvb amv mp4 mpg mp2 mpeg m4v 3gp")]
        public string VideoFileExtensions {
            get {
                return ((string)(this["VideoFileExtensions"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("pdf")]
        public string PdfFileExtensions {
            get {
                return ((string)(this["PdfFileExtensions"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("doc docx docm dot dotm dotx odt rtf")]
        public string WordFileExtensions {
            get {
                return ((string)(this["WordFileExtensions"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("csv xls xlsx xla xlam xlsb xlsm xlt xltm xltx xlw ods")]
        public string ExcelFileExtensions {
            get {
                return ((string)(this["ExcelFileExtensions"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("pot potm potx ppa ppam pps ppsm ppsx ppt pptm pptx odp")]
        public string PowerPointFileExtensions {
            get {
                return ((string)(this["PowerPointFileExtensions"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("zip tar gz tgz 7z rar")]
        public string CompressedFileExtensions {
            get {
                return ((string)(this["CompressedFileExtensions"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("dmg iso img")]
        public string DiskImageFileExtensions {
            get {
                return ((string)(this["DiskImageFileExtensions"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("c cpp h cgi php java py sh swith vb asp aspx css htm html js xml")]
        public string CodeFileExtensions {
            get {
                return ((string)(this["CodeFileExtensions"]));
            }
        }
    }
}