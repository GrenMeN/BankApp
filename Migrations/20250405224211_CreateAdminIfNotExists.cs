using FluentMigrator;

namespace BankApp
{
	[Migration(20250405224211)]
    public class CreateAdminIfNotExists : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
				IF NOT EXISTS (SELECT 1 FROM Account WHERE Number = 'admin')
				BEGIN
					INSERT INTO Account (Number, Type, Balance, PhoneNumber)
					VALUES ('admin', 2, 0.0, '')
				END
			");
        }

        public override void Down()
        {
            Execute.Sql("DELETE FROM Account WHERE Number = 'admin'");
        }
    }
}  