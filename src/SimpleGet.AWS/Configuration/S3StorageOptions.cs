using System.ComponentModel.DataAnnotations;
using SimpleGet.Core.Validation;

namespace SimpleGet.AWS.Configuration
{
    public class S3StorageOptions
    {
        [Required]
        public string Bucket { get; set; }

        public string Prefix { get; set; }
    }
}
