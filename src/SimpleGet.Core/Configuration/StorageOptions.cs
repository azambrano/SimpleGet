namespace SimpleGet.Core.Configuration
{
    public class StorageOptions
    {
        public StorageType Type { get; set; }
    }

    public enum StorageType
    {
        FileSystem = 0,
        AwsS3 = 2,
        Null = 4,
    }
}
