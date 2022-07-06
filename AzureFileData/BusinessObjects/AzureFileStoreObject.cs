using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace AzureFileData.BusinessObjects
{
    /// <summary>
    /// This class enables you to store uploaded files in a centralized azure file system location instead of the database. You can configure the file system store location via the static FileSystemDataModule.FileSystemStoreLocation property.
    /// </summary>
    [DefaultProperty("FileName")]
    public class AzureFileStoreObject : BaseObject, IFileData, IEmptyCheckable {
        private Stream tempSourceStream;
        private string tempFileName = string.Empty;

        
        public AzureFileStoreObject(Session session) : base(session) {

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        }


        private ShareDirectoryClient shareDirectoryClient = null;
        public ShareDirectoryClient ShareDirectoryClient
        {
            get
            {
                if (shareDirectoryClient == null)
                {
                    var shareClient = new ShareClient(AzureFileDataModule.AzureFileConnectionString, AzureFileDataModule.AzureFileShareLocation);
                    shareClient.CreateIfNotExists();
                    shareDirectoryClient = shareClient.GetRootDirectoryClient();
                }
               
                return shareDirectoryClient;
                
            }
        }


        public string RealFileName {
            get {
                if (!string.IsNullOrEmpty(FileName) && Oid != Guid.Empty)
                    return $"{Oid}-{FileName}";
                return null;
            }
        }

        protected virtual void SaveFileToStore()
        {
            if (!string.IsNullOrEmpty(RealFileName) && TempSourceStream != null)
            {
                try
                {
                    
                    WriteFile(RealFileName, TempSourceStream);
                    Size = (int)TempSourceStream.Length;
                }
                catch (Exception exc)
                {
                    throw new UserFriendlyException(exc);
                }
            }
        }

        #region Azure share file
        private void WriteFile(string filename, Stream stream)
        {
            

            //  Azure allows for 4MB max uploads  (4 x 1024 x 1024 = 4194304)
            const int uploadLimit = 4194304;

            stream.Seek(0, SeekOrigin.Begin);   // ensure stream is at the beginning
            var fileClient = ShareDirectoryClient.CreateFile(filename, stream.Length);

            // If stream is below the limit upload directly
            if (stream.Length <= uploadLimit)
            {
                fileClient.Value.UploadRange(new HttpRange(0, stream.Length), stream);
                return;
            }

            int bytesRead;
            long index = 0;
            byte[] buffer = new byte[uploadLimit];

            // Stream is larger than the limit so we need to upload in chunks
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                // Create a memory stream for the buffer to upload
                using (MemoryStream ms = new MemoryStream(buffer, 0, bytesRead))
                {
                    fileClient.Value.UploadRange(ShareFileRangeWriteType.Update, new HttpRange(index, ms.Length), ms);
                    index += ms.Length; // increment the index to the account for bytes already written
                }
            }

        }
        #endregion


        private void RemoveOldFileFromStore()
        {
            if (!string.IsNullOrEmpty(tempFileName) && tempFileName != RealFileName)
            {   
                try
                {
                    ShareFileClient file = ShareDirectoryClient.GetFileClient(tempFileName);

                    file.DeleteIfExists();

                    tempFileName = string.Empty;
                }
                catch (Exception exc)
                {
                    throw new UserFriendlyException(exc);
                }
            }
        }

        protected override void OnSaving() {
            base.OnSaving();
            Guard.ArgumentNotNullOrEmpty(AzureFileDataModule.AzureFileShareLocation, "AzureFileShareLocation");
            
            SaveFileToStore();
            RemoveOldFileFromStore();
        }
        protected override void OnDeleting() {
            
            Clear();
            base.OnDeleting();
        }
        protected override void Invalidate(bool disposing) {
            if (disposing && TempSourceStream != null) {
                TempSourceStream.Close();
                TempSourceStream = null;
            }
            base.Invalidate(disposing);
        }
        #region IFileData Members
        public void Clear() {
            
            if (string.IsNullOrEmpty(tempFileName))
                tempFileName = RealFileName;
            FileName = string.Empty;
            Size = 0;
        }
        [Size(260)]
        public string FileName {
            get { return GetPropertyValue<string>("FileName"); }
            set { SetPropertyValue("FileName", value); }
        }
        [Browsable(false)]
        public Stream TempSourceStream {
            get { return tempSourceStream; }
            set {
                
                if (value == null) {
                    tempSourceStream = null;
                } else {
                    if (value.Length > (long)int.MaxValue) throw new UserFriendlyException("File is too long");
                    tempSourceStream = new MemoryStream((int)value.Length);
                    AzureFileDataModule.CopyStream(value, tempSourceStream);
                    tempSourceStream.Position = 0;
                }
            }
        }
        
        void IFileData.LoadFromStream(string fileName,Stream source) {
            
            if(fileName != FileName) 
            {
                tempFileName = RealFileName;
            }
            FileName = fileName;
            TempSourceStream = source;
            Size = (int)TempSourceStream.Length;
            OnChanged();
        }

        /// <summary>
        /// Fires when saving or opening a file.
        /// </summary>
        /// <param name="destination"></param>
        /// <exception cref="UserFriendlyException"></exception>
        void IFileData.SaveToStream(Stream destination)
        {
            try
            {
                if (!string.IsNullOrEmpty(RealFileName))
                {
                    ShareFileClient file = ShareDirectoryClient.GetFileClient(RealFileName);
                    if (file.Exists())
                    {
                        using (ShareFileDownloadInfo download = file.Download())
                        {
                            
                            AzureFileDataModule.CopyStream(download.Content, destination);
                            
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException();
                    }

                }
                else if (TempSourceStream != null)
                    AzureFileDataModule.CopyStream(TempSourceStream, destination);
            }
            catch (FileNotFoundException exc)
            {
                throw new UserFriendlyException(exc);
            }
            catch (Exception exc)
            {
                throw new UserFriendlyException(exc);
            }

        }

        
        [Persistent]
        public int Size {
            get { return GetPropertyValue<int>("Size"); }
            private set { SetPropertyValue<int>("Size", value); }
        }
        #endregion
        #region IEmptyCheckable Members
        public bool IsEmpty {
            
            get { return FileDataHelper.IsFileDataEmpty(this) || !(this.TempSourceStream!= null || (ShareDirectoryClient.GetFileClient(RealFileName).Exists())); }
        }
        #endregion
    }
}
