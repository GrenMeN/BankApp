using FluentMigrator;

namespace BankApp
{
	[Migration(20250405215124)]
	public class Init : Migration
	{
        public override void Up()
        {
            Create.Table("Account")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Number").AsString().NotNullable()
                .WithColumn("Type").AsInt32().NotNullable()
                .WithColumn("Balance").AsDecimal().NotNullable()
                .WithColumn("PhoneNumber").AsString().NotNullable();
        }

        public override void Down()
        {
            Delete.Table("Account");
        }
    }
}  