namespace UpRestEye3.Models.DAO
{
    public class UserDAO
    {
        public int? Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int ConsumerId { get; set; }
        public ConsumerDAO Consumer { get; set; }
    }

}
