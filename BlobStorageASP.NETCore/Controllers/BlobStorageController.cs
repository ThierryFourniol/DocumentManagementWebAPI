using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DocumentManagementWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlobStorageController : ControllerBase
    {
        private readonly IOptions<MyConfig> config;


        public BlobStorageController(IOptions<MyConfig> config)
        {
            this.config = config;
        }

        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile asset)
        {
            try
            {
                // Acceptance criterion: in case the file is non PDF...
                if (!asset.FileName.ToLowerInvariant().EndsWith("pdf"))
                {
                    return Content("Upload failed: File is non-PDF");
                }

                // Acceptance criterion: in case the file size is greater than 5 MB...
                long fileSizeLimit = 5242880; //TODO Retrieve/parse the file size limit from the appsettings.json configuration file
                if (asset.Length > fileSizeLimit)
                {
                    return Content("Upload failed: File size exceeds the max size of 5 MB");
                }

                if (CloudStorageAccount.TryParse(config.Value.StorageConnection, out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                    CloudBlobContainer container = blobClient.GetContainerReference(config.Value.Container);

                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(asset.FileName);

                    await blockBlob.UploadFromStreamAsync(asset.OpenReadStream());

                    return Content("Upload successful: File has been uploaded");
                }
                else
                {
                    return Content("Upload failed: Error opening cloud storage");
                }
            }
            catch
            {
                return Content("Upload failed: File cannot be uploaded as an exception occurred");
            }
        }

        [HttpGet("DownloadFile/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            MemoryStream ms = new MemoryStream();
            if (CloudStorageAccount.TryParse(config.Value.StorageConnection, out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(config.Value.Container);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference(fileName);

                    if (await file.ExistsAsync())
                    {
                        await file.DownloadToStreamAsync(ms);
                        Stream blobStream = file.OpenReadAsync().Result;
                        return File(blobStream, file.Properties.ContentType, file.Name);
                    }
                    else
                    {
                        return Content("Download failed: File does not exist");
                    }
                }
                else
                {
                    return Content("Download failed: Container does not exist");
                }
            }
            else
            {
                return Content("Download failed: Error opening cloud storage");
            }
        }

        [HttpGet("ListFiles")]
        public async Task<List<string>> ListFiles()
        {
            List<string> blobs = new List<string>();
            try
            {
                if (CloudStorageAccount.TryParse(config.Value.StorageConnection, out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                    CloudBlobContainer container = blobClient.GetContainerReference(config.Value.Container);

                    BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(null);
                    foreach (IListBlobItem item in resultSegment.Results)
                    {
                        if (item.GetType() == typeof(CloudBlockBlob))
                        {
                            CloudBlockBlob blob = (CloudBlockBlob)item;
                            blobs.Add(blob.Name);
                        }
                        else if (item.GetType() == typeof(CloudPageBlob))
                        {
                            CloudPageBlob blob = (CloudPageBlob)item;
                            blobs.Add(blob.Name);
                        }
                        else if (item.GetType() == typeof(CloudBlobDirectory))
                        {
                            CloudBlobDirectory dir = (CloudBlobDirectory)item;
                            blobs.Add(dir.Uri.ToString());
                        }
                    }
                }
            }
            catch
            {
                // An exception occurred
            }
            return blobs;
        }

        [HttpGet("ReorderFiles{reorderedFileNames}")]
        public async Task<List<string>> ReorderFiles(List<string> reorderedFileNames)
        {
            List<string> blobs = new List<string>();
            try
            {
                if (CloudStorageAccount.TryParse(config.Value.StorageConnection, out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                    CloudBlobContainer container = blobClient.GetContainerReference(config.Value.Container);

                    BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(null);
                    foreach (IListBlobItem item in resultSegment.Results)
                    {
                        if (item.GetType() == typeof(CloudBlockBlob))
                        {
                            CloudBlockBlob blob = (CloudBlockBlob)item;
                            blobs.Add(blob.Name);
                        }
                        else if (item.GetType() == typeof(CloudPageBlob))
                        {
                            CloudPageBlob blob = (CloudPageBlob)item;
                            blobs.Add(blob.Name);
                        }
                        else if (item.GetType() == typeof(CloudBlobDirectory))
                        {
                            CloudBlobDirectory dir = (CloudBlobDirectory)item;
                            blobs.Add(dir.Uri.ToString());
                        }
                    }
                }
            }
            catch
            {
                // An exception occurred
            }
            return blobs;
        }

        [Route("DeleteFile/{fileName}")]
        [HttpGet]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            try
            {
                if (CloudStorageAccount.TryParse(config.Value.StorageConnection, out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = BlobClient.GetContainerReference(config.Value.Container);

                    if (await container.ExistsAsync())
                    {
                        CloudBlob file = container.GetBlobReference(fileName);

                        if (await file.ExistsAsync())
                        {
                            await file.DeleteAsync();
                        }
                        else
                        {
                            // Acceptance criterion: in case the file does not exist...
                            return Content("Deletion failed: File does not exist");
                        }
                    }
                }
                else
                {
                    return Content("Deletion failed: Error opening cloud storage");
                }
                return Content("Deletion successful: File has been deleted successfully");
            }
            catch
            {
                return Content("Deletion failed: File cannot be deleted as an exception occurred");
            }
        }
    }
}
