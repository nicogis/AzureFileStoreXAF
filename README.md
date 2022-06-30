# How to: Store file attachments in the azure file instead of the database (XPO)

- Add in agostic module AzureFileDataModule

```csharp
/// <summary> 
/// Required method for Designer support - do not modify 
/// the contents of this method with the code editor.
/// </summary>
private void InitializeComponent() {
    // 
    // AzureFileStoreXAFModule
    // 
    this.AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.BaseObject));
    this.AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.FileData));
    this.AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.FileAttachmentBase));
    this.RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
    this.RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule));
    this.RequiredModuleTypes.Add(typeof(AzureFileData.AzureFileDataModule));

}
```



- Create a property in your BO

```csharp
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
```


- Set in web.config the name of share folder in azure and [connectionstring azure](https://docs.microsoft.com/it-it/azure/storage/files/storage-how-to-create-file-share?tabs=azure-portal) 
 
![Azurefile](azurefilestorexaf.module/images/azurefile.png)

```xml
<!--set share folder-->
<add key="ShareName" value="" />

<!--azure connection string -->
<add key="ConnectionString" value="" />
```

- Set parameter in start code (event login ect)
```xml
AzureFileData.AzureFileDataModule.AzureFileConnectionString = ConfigurationManager.AppSettings["ConnectionString"];
AzureFileData.AzureFileDataModule.AzureFileShareLocation = ConfigurationManager.AppSettings["ShareName"];
```

You can upload file > 4Mb but you must set in web.config
```xml

<system.web>
    <!--60mb-->
	<httpRuntime requestValidationMode="2.0" maxRequestLength="61440" />
		
.....

...
        <security>
		<requestFiltering>
			<!--The default size is 30000000 bytes (28.6 MB). MaxValue is 4294967295 bytes (4 GB)-->
			<!-- Eaxmple 60 MB in bytes -->
			<requestLimits maxAllowedContentLength="61440000" />
		</requestFiltering>
	</security>
</system.webServer>
```



![Azurefilefiledataxaf](azurefilestorexaf.module/images/azurefilefiledataxaf.png)



**Developed using only XAF (web) .NET Framework.**



