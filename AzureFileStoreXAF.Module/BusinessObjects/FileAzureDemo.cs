
using AzureFileData.BusinessObjects;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace AzureFileStoreXAF.Module.BusinessObjects
{
    [DefaultClassOptions]
    [FileAttachment("File")]
    public class FileSystemStoreObjectDemo : BaseObject
    {
        public FileSystemStoreObjectDemo(Session session) : base(session) { }
        [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never), ImmediatePostData]
        public AzureFileStoreObject File
        {
            get { return GetPropertyValue<AzureFileStoreObject>("File"); }
            set { SetPropertyValue<AzureFileStoreObject>("File", value); }
        }
    }
}