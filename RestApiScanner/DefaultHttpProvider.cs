namespace Demo.RestApiScanner
{
    public class DefaultHttpProvider
    {
        private string host;
        private string domain;
        private string user;
        private string pass;
        private string pinKey;
        private bool usePinning;
        private int httpTimeout;

        public DefaultHttpProvider(string host, string domain, string user, string pass, string pinKey, bool usePinning, int httpTimeout)
        {
            this.host = host;
            this.domain = domain;
            this.user = user;
            this.pass = pass;
            this.pinKey = pinKey;
            this.usePinning = usePinning;
            this.httpTimeout = httpTimeout;
        }
    }
}