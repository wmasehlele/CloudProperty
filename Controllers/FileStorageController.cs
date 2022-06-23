using CloudProperty.Models;
using CloudProperty.Sevices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudProperty.Controllers
{
    [Route("api/storage")]
    [ApiController, Authorize]
    public class FileStorageController : AppController
    {
        private readonly BlobStorage blobStorage;
        public FileStorageController(BlobStorage blobStorage) {
            this.blobStorage = blobStorage;
        }

        [HttpPost("uploadfiles")]
        public async Task<ActionResult<List<FileStorage>>> UploadFiles(List<IFormFile> files, IFormCollection fileForm) {

            if (files.Count == 0) { return BadRequest("No files uploaded"); }

            if (String.IsNullOrEmpty(fileForm["Type"]) || String.IsNullOrEmpty(fileForm["ModelName"]) || String.IsNullOrEmpty(fileForm["ModelId"])) {
                return BadRequest("Missing file upload details");
            }
            
            List<FileStorage> uploadedFiles = new List<FileStorage>();
            var bolbUrl = string.Empty;
            foreach (var file in files) {

                if (!this.blobStorage.VerifyFileExtension(file.FileName)) { return BadRequest("Invalid file uploaded"); }
                
                FileStorage fileStorage = new FileStorage();
                fileStorage.FileName = DateTime.UtcNow.ToString("yyyymmddHHss") + AuthUserID.ToString() + Path.GetExtension(file.FileName);
                fileStorage.Description = fileForm["Description"];
                fileStorage.Type = fileForm["Type"];
                fileStorage.ModelName = fileForm["ModelName"];
                fileStorage.ModelId = fileForm["ModelId"];

                fileStorage = await this.blobStorage.UploadFile(file, fileStorage);
                //bolbUrl = this.blobStorage.GetBlobUrl(fileStorage.FileName);
                uploadedFiles.Add(fileStorage);
            }
            return Ok(uploadedFiles);
        }
    }
}
