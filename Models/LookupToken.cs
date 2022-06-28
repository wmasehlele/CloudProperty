using System.Security.Cryptography;
using System.Text;

namespace CloudProperty.Models
{
    public class LookupToken : AppModel
    {
        public int Id { get; set; } 
        public string Token { get; set; }
        public string Action { get; set; }
        public string? ModelName { get; set; } = String.Empty;
        public string? ModelData { get; set; } = String.Empty;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
    }
}
