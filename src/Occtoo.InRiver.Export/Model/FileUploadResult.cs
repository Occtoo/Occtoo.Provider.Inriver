using System.Collections.Generic;

namespace Occtoo.Generic.Inriver.Model
{
    public class FileUploadResult
    {
        public FileUploadResult()
        {
            Files = new List<FileItemResult>();
        }

        public string EnvironmentId { get; set; }

        public List<FileItemResult> Files { get; set; }
    }

    public class FileItemResult
    {
        public string EnvironmentId { get; set; }
        public string Filename { get; set; }
        public string TrustedFilename { get; set; }
        public string Id { get; set; }
        public string UniqueValue { get; set; }
        public string Url { get; set; }
        public string CdnUrl { get; set; }

        public string StatusCode { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}