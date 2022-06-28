using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CloudProperty.Models;

namespace CloudProperty.Sevices
{
    public class FileStorageService
    {
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly DatabaseContext context;
        private readonly BlobContainerClient blobContainerClient;
        private readonly string blobContainerName = "fileuploads";
        private readonly string localUploadDir = string.Empty;

        private string storageAccountUri = string.Empty;  
        private string storageAccountName = string.Empty;  

        private List<string> acceptableExtensions = new List<string>() { 
            ".csv",
            ".pdf",
            ".xls",
            ".xlsx",
            ".doc",
            ".docx",
            ".png",
            ".jpg",
            ".jpeg"
        };

        public FileStorageService(DatabaseContext context, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
            this.context = context;
            this.localUploadDir = Path.Combine(this.webHostEnvironment.ContentRootPath, "storage");
            this.blobContainerClient = new BlobContainerClient( this.configuration.GetSection("Storage:StorageConnection").Value,  this.blobContainerName);

            this.SetStorageAccountUrl();
        }

        private void SetStorageAccountUrl(string storageAccName = "") {
            this.storageAccountName = this.configuration.GetSection("Storage:StorageAccountName").Value;
            if (!string.IsNullOrEmpty(storageAccName)) {
                this.storageAccountName = storageAccName;
            }            
            this.storageAccountUri = $"https://{storageAccountName}.blob.core.windows.net";
        }

        public bool VerifyFileExtension(string fileName) {
            string extension = Path.GetExtension(fileName);            
            if (acceptableExtensions.Contains(extension.ToLower())) { 
                return true;
            }
            return false;
        }

        public async Task<FileStorageDTO> UploadFile (IFormFile file, FileStorage fileStorage) {
            
            if (file == null) { return null; }

            string filePath = Path.Combine(this.localUploadDir, fileStorage.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) {
                await file.CopyToAsync(stream);
            }

            this.context.Blobs.Add(fileStorage);
            await this.context.SaveChangesAsync();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;
                await blobContainerClient.UploadBlobAsync(fileStorage.FileName, stream);

                File.Delete(filePath);
            }

            return await GetFileById(fileStorage.Id);            
        }

        public string GetBlobUrl(string blobName)
        {
            return this.blobContainerClient.GetBlobClient(blobName).GenerateSasUri(BlobSasPermissions.Read, DateTime.UtcNow.AddDays(1)).AbsoluteUri;
        }

        public async Task<FileStorageDTO> GetFileById(int Id) {
            var fileStorageDto = new FileStorageDTO();
            fileStorageDto = await this.context.Blobs
                    .Where(f => f.Id == Id && f.Active == 1).Select(file => new FileStorageDTO()
                    {
                        Id = file.Id,
                        FileName = file.FileName,
                        Description = file.Description,
                        Type = file.Type,
                        ModelName = file.ModelName,
                        ModelId = file.ModelId,
                        Active = file.Active,
                        CreatedAt = file.CreatedAt,
                        UpdatedAt = file.UpdatedAt
                    }).FirstOrDefaultAsync();

            fileStorageDto.FileUrl = GetBlobUrl(fileStorageDto.FileName);

            return fileStorageDto;
        }
    }
}
