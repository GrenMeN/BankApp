using BankApp;
using BankApp.Entities;
using BankApp.Models;
using BankApp.Services;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using SqlKata.Execution;

internal class Program
{
    private static IConfigurationRoot _configuration;
    private static DbContext _db;
    const string ROOT_LAYOUT = "Root";
    const string CREATE_NEW_ACCOUNT = "Create new account";
    const string TRANSFER_MONEY = "Transfer money";
    const string SHOW_BALANCE = "Show balance";
    const string EXIT = "Exit";
    const string LOG_OUT = "Log out";
    const string SHOW_ALL = "[[Show all]]";

    private static void Main(string[] args)
    {
        AnsiConsole.Status()
            .Start("Starting the app...", ctx =>
            {
                AnsiConsole.MarkupLine("Building configuration...");
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();
                AnsiConsole.MarkupLine("Running migrations...");

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                MigrationService.RunMigrations(connectionString);

                AnsiConsole.MarkupLine("Initialize database connection...");
                _db = QueryFactoryService.CreateQueryFactory(connectionString);
            });
        AnsiConsole.Clear();

        bool isRunning = true;
        while (isRunning)
        {
            var accountNumber = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter your account number:")
            );
            var account = _db.Account.Where("Number", accountNumber).FirstOrDefault<Account>();

            if (account == null)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[red]Account not found![/]");
                continue;
            }

            var (userPrompt, typePrompt, showBalancePrompt) = LoadSelectPrompts(account);

            bool userLoggedIn = true;
            while (userLoggedIn)
            {
                AnsiConsole.Clear();
                var selectedOptions = AnsiConsole.Prompt(userPrompt);
                switch (selectedOptions)
                {
                    case SHOW_BALANCE:
                        ShowBalance(account, showBalancePrompt);
                        break;
                    case CREATE_NEW_ACCOUNT:
                        CreateAccount(typePrompt);
                        break;
                    case TRANSFER_MONEY:
                        TransferMoneyBetweenUsers(account);
                        break;
                    case LOG_OUT:
                        userLoggedIn = false;
                        break;
                    case EXIT:
                        userLoggedIn = false;
                        isRunning = false;
                        break;
                }
            }
        }
    }

    private static void TransferMoneyBetweenUsers(Account account)
    {
        if (account.Type == AccountType.Manager)
        {
            AnsiConsole.MarkupLine("[red]You are not allowed to transfer money![/]");
            return;
        }

        if (account.Type == AccountType.User)
        {
            var userToTransferOptions = _db.Account
                .Where("Type", AccountType.User)
                .Where("Number", "!=", account.Number)
                .Get<string>()
                .ToList();
            var userTransferPrompt = new SelectionPrompt<string>()
                .Title("Select user to transfer money:")
                .AddChoices(userToTransferOptions);
            var selectedUser = _db.Account.Where("Number", AnsiConsole.Prompt(userTransferPrompt))
                .FirstOrDefault<Account>();
            var amount = AnsiConsole.Ask<decimal>("How many you want to transfer:");
            if (account.Balance < amount)
            {
                AnsiConsole.MarkupLine("[red]Not enough money![/]");
            }
            else
            {
                TransferMoney(account, selectedUser, amount);
            }
        }
        else if (account.Type == AccountType.Administrator)
        {
            var allUsers = _db.Account
                .Where("Type", AccountType.User)
                .Get<string>()
                .ToList();
            var userFromTransferNumber = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select user to transfer money from:")
                .AddChoices(allUsers));
            var usersToTransferOptions = allUsers.Where(x => x != userFromTransferNumber).ToList();
            if (!usersToTransferOptions.Any())
            {
                AnsiConsole.MarkupLine("[red]No users to transfer money to![/]");
                Console.ReadLine();
                return;
            }
            var userToTransferNumber = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select user to transfer money to:")
                .AddChoices(usersToTransferOptions));
            var amount = AnsiConsole.Ask<decimal>("How many you want to transfer:");
            var userFromTransfer = _db.Account
                .Where("Number", userFromTransferNumber)
                .FirstOrDefault<Account>();
            var userToTransfer = _db.Account
                .Where("Number", userToTransferNumber)
                .FirstOrDefault<Account>();
            if (userFromTransfer != null && userToTransfer != null && userFromTransfer.Balance < amount)
            {
                AnsiConsole.MarkupLine("[red]Not enough money![/]");
            }
            else
            {
                TransferMoney(userFromTransfer, userToTransfer, amount);
            }
        }

        Console.ReadLine();
    }

    private static void TransferMoney(Account accountFrom, Account accountTo, decimal amount)
    {
        //Exta carefully with manipulation with money
        var connection = _db.QueryFactory.Connection;
        if (connection.State == System.Data.ConnectionState.Closed)
        {
            connection.Open();
        }

        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                accountFrom.Balance -= amount;
                _db.Account.Where("Number", accountFrom.Number)
                    .Update(accountFrom, transaction);

                accountTo.Balance += amount;
                _db.Account.Where("Number", accountTo.Number)
                    .Update(accountTo, transaction);

                transaction.Commit();
                AnsiConsole.MarkupLine("[green]Transfer successful![/]");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                AnsiConsole.MarkupLine($"[red]Transfer failed: {ex.Message}[/]");
            }
        }
    }

    private static void ShowBalance(Account account, SelectionPrompt<string> showBalancePrompt)
    {
        if (account.Type == AccountType.User)
        {
            AnsiConsole.MarkupLine($"Your balance: [green]{account.Balance}$[/]");
            Console.ReadLine();
        }
        else
        {
            var selectedAccount = AnsiConsole.Prompt(showBalancePrompt);
            if (selectedAccount == SHOW_ALL)
            {
                AnsiConsole.Clear();
                var allAccounts = _db.Account.Where("Type", AccountType.User).Get<Account>();
                var table = new Table();
                table.Title("All accounts balance");
                table.AddColumn("Number");
                table.AddColumn("Balance, $");
                foreach (var acc in allAccounts)
                {
                    table.AddRow(acc.Number, $"[green]{acc.Balance.ToString()}[/]");
                }
                AnsiConsole.Write(table);
                Console.ReadLine();
            }
            else
            {
                var selectedAccountData = _db.Account.Where("Number", selectedAccount).FirstOrDefault<Account>();
                if (selectedAccountData == null)
                {
                    AnsiConsole.MarkupLine("[red]Account not found![/]");
                    Console.ReadLine();
                }
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine($"Balance of {selectedAccountData?.Number}: [green]{selectedAccountData?.Balance}$[/]");
                Console.ReadLine();
            }
        }
    }

    private static (SelectionPrompt<string> userPrompt,
        SelectionPrompt<string> typePrompt,
        SelectionPrompt<string> showBalancePrompt) LoadSelectPrompts(Account account)
    {
        var userOptions = new List<string> { SHOW_BALANCE };
        List<string> showBalanceOptions = new List<string>();
        if (account.Type == AccountType.Administrator || account.Type == AccountType.Manager)
        {
            userOptions.Add(CREATE_NEW_ACCOUNT);
            showBalanceOptions = new List<string>
            {
                "[[Show all]]"
            };
            _db.Account.Where("Type", AccountType.User)
                .Get<string>()
                .ToList()
                .ForEach(x => showBalanceOptions.Add(x));
        }
        if (account.Type != AccountType.Manager)
        {
            userOptions.Add(TRANSFER_MONEY);
        }
        userOptions.Add(LOG_OUT);
        userOptions.Add(EXIT);

        var userPrompt = new SelectionPrompt<string>().AddChoices(userOptions);
        var typePrompt = new SelectionPrompt<string>()
            .Title("Select account type:")
            .AddChoices(new List<string>
            {
                AccountType.User.ToString(),
                AccountType.Manager.ToString(),
                AccountType.Administrator.ToString(),
            });
        var showBalancePrompt = new SelectionPrompt<string>()
            .Title("Select account number:")
            .AddChoices(showBalanceOptions);

        return (userPrompt, typePrompt, showBalancePrompt);
    }

    private static void CreateAccount(SelectionPrompt<string> typePrompt)
    {
        var forbiddenNumbers = _db.Account.Select("Number").Get<string>();
        AnsiConsole.Clear();
        var accountToCreate = new Account();
        while (true)
        {
            accountToCreate.Number = AnsiConsole.Ask<string>("Enter account number:");
            if (forbiddenNumbers.Contains(accountToCreate.Number))
            {
                AnsiConsole.MarkupLine("[red]This account number is already taken![/]");
                Console.ReadLine();
                AnsiConsole.Clear();
                continue;
            }
            break;
        }
        accountToCreate.Type = Enum.Parse<AccountType>(AnsiConsole.Prompt(typePrompt));
        AnsiConsole.WriteLine($"Selected user type: {accountToCreate.Type}");
        if (accountToCreate.Type == AccountType.User)
        {
            accountToCreate.Balance = AnsiConsole.Ask<decimal>("Enter start balance:");
            accountToCreate.PhoneNumber = AnsiConsole.Ask<string>("Enter phone number:");
        }
        else
        {
            accountToCreate.Balance = 0;
            accountToCreate.PhoneNumber = "";
        }
        AnsiConsole.Clear();
        AnsiConsole.WriteLine("You are going to create a new account with the following data:");
        var table = new Table();
        table.AddColumn("Number");
        table.AddColumn("Type");
        table.AddColumn("Balance");
        table.AddColumn("Phone number");
        table.AddRow(accountToCreate.Number,
            accountToCreate.Type.ToString(),
            accountToCreate.Balance.ToString(),
            accountToCreate.PhoneNumber.ToString());
        AnsiConsole.Write(table);
        var confirm = AnsiConsole.Confirm("Do you want to create this account?");
        if (confirm)
        {
            _db.Account.Insert(accountToCreate);
            AnsiConsole.MarkupLine("[green]Account created![/]");
        }
        Console.ReadLine();
    }
}
