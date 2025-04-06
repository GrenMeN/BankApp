using SqlKata;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.Models
{
    public class DbContext
    {
        public DbContext(QueryFactory queryFactory)
        {
            QueryFactory = queryFactory;
        }

        public QueryFactory QueryFactory { get; set; }
        public Query Account { get => QueryFactory.Query("Account"); }
    }
}
