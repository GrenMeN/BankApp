namespace BankApp.Entities
{
    public class Account
    {
        public string Number { get; set; }
        public AccountType Type { get; set; }
        public decimal Balance { get; set; }
        public string PhoneNumber { get; set; }
    }

    public enum AccountType 
    { 
        User,
        Manager,
        Administrator
    }
}