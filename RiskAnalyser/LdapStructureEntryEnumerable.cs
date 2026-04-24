using Demo;
using System.Collections;

namespace RiskAnalyzer
{
    public class LdapStructureEntryEnumerable(IEnumerable<LdapStructureEntry> entries) : LdapStructureEntry, IEnumerable<LdapStructureEntry>
    {
        private readonly IEnumerable<LdapStructureEntry> _entries = entries;

        public IEnumerator<LdapStructureEntry> GetEnumerator() => _entries.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class LdapStructureEntry
    {
        public int[] ActiveDirectoryFlags { get; internal set; } = Array.Empty<int>();
        public string Delegation { get; internal set; } = string.Empty;
        public string[] Acl { get; internal set; } = Array.Empty<string>();
        public string Id { get; internal set; } = string.Empty;
        public string Name { get; internal set; } = string.Empty;
        public string[] AttrNames { get; internal set; } = Array.Empty<string>();
        internal StorageEntryType Type { get; set; }
    }
}