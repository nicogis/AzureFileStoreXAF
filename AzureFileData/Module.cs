using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using System;
using System.ComponentModel;
using System.IO;

namespace AzureFileData
{
    [Description("This module provides the AzureFileStoreObject class that enable you to store uploaded files in a azure file instead of the database.")]
    public sealed partial class AzureFileDataModule : ModuleBase {
        public static int ReadBytesSize = 0x1000;
        public static string AzureFileShareLocation = null;
        public static string AzureFileConnectionString= null;
        

        public AzureFileDataModule() {
            InitializeComponent();
            BaseObject.OidInitializationMode = OidInitializationMode.AfterConstruction;
        }
        public static void CopyFileToStream(string sourceFileName, Stream destination) {
            if (string.IsNullOrEmpty(sourceFileName) || destination == null) return;
            using (Stream source = File.OpenRead(sourceFileName))
                CopyStream(source, destination);
        }
        
        public static void CopyStream(Stream source, Stream destination) {
            if (source == null || destination == null) return;
            byte[] buffer = new byte[ReadBytesSize];
            int read = 0;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                destination.Write(buffer, 0, read);
        }
    }
}