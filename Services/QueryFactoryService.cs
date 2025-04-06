using BankApp.Models;
using SqlKata.Execution;
using Microsoft.Data.SqlClient;

namespace BankApp.Services
{
    public static class QueryFactoryService
    {
        public static DbContext CreateQueryFactory(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            var compiler = new SqlKata.Compilers.SqlServerCompiler();
            var db = new QueryFactory(connection, compiler);
            return new DbContext(db);
        }
    }
}
