namespace Demo.RestApiScanner
{
    public class NodeInfo
    {
        public long Level { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        public string Path { get; internal set; } = string.Empty;
        public StorageEntryType Type { get; internal set; } = StorageEntryType.Directory;
        public string Id { get; internal set; } = string.Empty;
        public string? Owner { get; internal set; }
        public DateTime Create { get; internal set; }
        public DateTime Update { get; internal set; }
        public long Size { get; internal set; }
        public string[] Acls { get; internal set; } = Array.Empty<string>();
        public Dictionary<string, string> Attrs { get; internal set; } = new Dictionary<string, string>();
    }
}