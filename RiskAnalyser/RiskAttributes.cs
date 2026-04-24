namespace Demo
{
    internal class RiskAttributes
    {
        public string Name { get; internal set; } = string.Empty;
        public string Id { get; internal set; } = string.Empty;

        public string Description { get; internal set; } = string.Empty;
        public Dictionary<string, string> Attributes { get; internal set; } = [];

        internal string GetAttribute(string nsaccountLock)
        {
            return Attributes.ContainsKey(nsaccountLock) ? Attributes[nsaccountLock] : string.Empty;
        }
    }
}