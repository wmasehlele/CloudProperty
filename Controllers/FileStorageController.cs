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
        private readonly FileStorageService _fileStorageService;
        public FileStorageController(FileStorageService fileStorageService) {
            _fileStorageService = fileStorageService;
        }

        [HttpPost("uploadfiles")]
        public async Task<ActionResult<List<FileStorageDTO>>> UploadFiles(List<IFormFile> files, IFormCollection fileForm) {

            if (files.Count == 0) { return BadRequest("No files uploaded"); }

            if (String.IsNullOrEmpty(fileForm["Type"]) || String.IsNullOrEmpty(fileForm["ModelName"]) || String.IsNullOrEmpty(fileForm["ModelId"])) {
                return BadRequest("Missing file upload details");
            }
            
            List<FileStorageDTO> uploadedFiles = new List<FileStorageDTO>();
            var bolbUrl = string.Empty;
            foreach (var file in files) {

                if (!_fileStorageService.VerifyFileExtension(file.FileName)) { return BadRequest("Invalid file uploaded"); }
                
                FileStorage fileStorage = new FileStorage();
                fileStorage.FileName = DateTime.UtcNow.ToString("yyyymmddHHss") + AuthUserID.ToString() + Path.GetExtension(file.FileName);
                fileStorage.Description = fileForm["Description"];
                fileStorage.Type = fileForm["Type"];
                fileStorage.ModelName = fileForm["ModelName"];
                fileStorage.ModelId = Convert.ToInt32(fileForm["ModelId"]);

                var fileStorageDto = await _fileStorageService.UploadFile(file, fileStorage);
                //bolbUrl = this.fileStorageService.GetBlobUrl(fileStorage.FileName);
                uploadedFiles.Add(fileStorageDto);
            }
            return Ok(uploadedFiles);
        }
    }
}
