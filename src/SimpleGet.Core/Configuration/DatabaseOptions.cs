using System.ComponentModel.DataAnnotations;

namespace SimpleGet.Core.Configuration
{
    public class DatabaseOptions
    {
        public DatabaseType Type { get; set; }

        [Required]
        public string ConnectionString { get; set; }
    }

    public enum DatabaseType
    {
        Mongo,
    }
}
