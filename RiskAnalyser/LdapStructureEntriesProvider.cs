namespace RiskAnalyzer
{
    internal class LdapStructureEntriesProvider
    {
        private string clickhouseCon;

        public LdapStructureEntriesProvider(string clickhouseCon)
        {
            this.clickhouseCon = clickhouseCon;
        }

        internal IEnumerable<T> Get<T>(int storeId, string v, List<string> fields)
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<T> Get<T>(int storeId, string v)
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<T> Get<T>(int storeId)
        {
            throw new NotImplementedException();
        }
    }
}