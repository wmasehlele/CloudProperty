namespace CloudProperty.Data
{
    public class UserDto
    {
        // Used only for login and registration purposes.
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cellphone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;    
    }
}
