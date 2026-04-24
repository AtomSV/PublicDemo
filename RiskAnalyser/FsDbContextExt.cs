using Microsoft.EntityFrameworkCore;

namespace RiskAnalyzer
{
    internal class FsDbContextExtDbContext : DbContext
    {
        private string postgressCon;

        public FsDbContextExtDbContext(string postgressCon) : base()
        {
            this.postgressCon = postgressCon;
        }

        public DbSet<LdapServerSetting> LdapServerSettings { get; internal set; }
        public IEnumerable<LdapComuters> LdapComputers { get; internal set; }
    }
}