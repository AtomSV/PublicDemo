namespace Demo.RestApiScanner
{
    public abstract class Client
    {
        public abstract ClientErrorInfo Check();
        public abstract byte[] Read(string path, long size);
        public abstract Task<NodeInfo> GetTreeAsync(CancellationToken token, string? savePoint = null);
    }

    public class ClientErrorInfo
    {
        private object value;

        public ClientErrorInfo(object value)
        {
            this.value = value;
        }
    }
}