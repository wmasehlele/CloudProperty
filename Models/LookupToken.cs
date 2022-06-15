using System.Security.Cryptography;
using System.Text;

namespace CloudProperty.Models
{
    public class LookupToken
    {
        public int Id { get; set; } 
        public string Token { get; set; }
        public string Action { get; set; }
        public string? ModelName { get; set; } = String.Empty;
        public string? ModelData { get; set; } = String.Empty;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        private readonly DataContext context;

        internal static readonly char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
        const int KEY_SIZE = 32;

        public LookupToken() { }
        public LookupToken (DataContext context)
        {
            this.context = context;
        }

        public async Task<LookupToken> CreateLookupToken(LookupToken lookupToken) 
        {
            GenerateToken(KEY_SIZE, out string token);
            lookupToken.Token = token;
            lookupToken.ExpiresAt = DateTime.UtcNow.AddMinutes(30);
            lookupToken.CreatedAt = DateTime.UtcNow;
            lookupToken.UpdatedAt = DateTime.UtcNow;

            await this.context.AddAsync(lookupToken);
            await this.context.SaveChangesAsync();
            
            return lookupToken;
        }

        public async Task<LookupToken> VerifyLookupToken(string token) 
        {
            if (string.IsNullOrEmpty(token)) { return null; }

            var lookupToken = await this.context.LookupTokens.Where(t => t.Token == token).FirstOrDefaultAsync();

            if (lookupToken == null) { return null; }

            if (lookupToken.ExpiresAt < DateTime.UtcNow) { return null; }

            return lookupToken;
        }

        private void GenerateToken(int size, out string lookupToken)
        {
            byte[] data = new byte[4 * size];
            using (var crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            lookupToken = result.ToString();
        }

    }
}
