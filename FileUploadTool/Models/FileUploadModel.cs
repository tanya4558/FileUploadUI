using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FileUploadTool.Models
{
    public class FileUploadModel
    {
        public string SessionId { get; set; }
        public int FileIndex { get; set; }
        public string FilePath { get; set; }

        public MultipartFormDataContent ToFormData()
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(SessionId), "SessionId");
            content.Add(new StringContent(FileIndex.ToString()), "FileIndex");
            var fileStream = System.IO.File.OpenRead(FilePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "File", System.IO.Path.GetFileName(FilePath));
            return content;
        }
    }
}
