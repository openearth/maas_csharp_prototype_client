namespace DeltaSphereTestApp.Entities
{
    internal class S3File
    {
        public string Key { get; set; }

        public string LastModified { get; set; }

        public string ETag { get; set; }

        public string StorageClass { get; set; }
    }
}