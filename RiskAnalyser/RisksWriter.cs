namespace RiskAnalyzer
{
    internal class RisksWriter
    {
        private readonly string clickhouseCon;
        private readonly int storeId;

        public RisksWriter(string clickhouseCon, int storeId)
        {
            this.clickhouseCon = clickhouseCon;
            this.storeId = storeId;
        }

        public void Add(LdapStructureEntryEnumerable riskResult)
        {
            throw new NotImplementedException();
        }

        internal void Complete()
        {
            throw new NotImplementedException();
        }
    }
}