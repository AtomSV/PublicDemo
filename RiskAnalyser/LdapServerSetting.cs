using Demo;

namespace RiskAnalyzer
{
    public class LdapServerSetting
    {
        public string? ExcludeRiskUsers { get; internal set; }
        internal LdapServerType Type { get; set; }
    }
}