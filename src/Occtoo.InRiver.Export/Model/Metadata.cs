namespace Occtoo.Generic.Inriver.Model
{
    public class Metadata
    {
        public string Id { get; set; }

        public string DataSource { get; set; }

        public string EnvironmentId { get; set; }

        public string Filename { get; set; }

        public string Checksum { get; set; }

        public string Path { get; set; }
        public string Url { get; set; }
        public string CdnUrl { get; set; }

        public string SourceUrl { get; set; }

        public long Size { get; set; }
        public Tag[] Tags { get; set; }

        public string Format { get; set; }

        public string ParentId { get; set; }

        public UploadStatus UploadStatus { get; set; }

        public string UploadStatusMessage { get; set; }

        public MediaType MediaType { get; set; }
        public string MimeType { get; set; }

        public bool IsCdn { get; set; }
    }

    public class Tag
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string Type { get; set; }
    }

    public enum UploadStatus
    {
        Queued = 0,

        Pending = 1,

        Uploaded = 2,

        Imported = 4,

        Processed = 8,

        Failed = 128
    }

    public enum MediaType
    {
        Unknown = 0,
        Image,
        Video,
        Text,
        Zip,
        Office,
    }
}