namespace CloudProperty.Models
{
    public class AppModel
    {
        protected string sometext = string.Empty;
        public string Sometextother = string.Empty;

        public int GenerateOtp(int maxRange = 10, int maxDigits = 5)
        {
            string randomNo = String.Empty;
            Random rnd = new Random();
            for (int j = 0; j < 5; j++)
            {
                randomNo = randomNo + rnd.Next(0, 10).ToString();
            }
            return Convert.ToInt32(randomNo);
        }
    }
}
