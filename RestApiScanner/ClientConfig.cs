using System.Net;

namespace Demo.RestApiScanner
{
    public class ClientConfig
    {
        public required string Host { get; set; }
        public required Credentials Credentials { get; set; }
        public string? PinKey { get; internal set; }
        public bool UsePinning { get; internal set; }
        public int HttpRequestTimeout { get; internal set; }
        public string[]? Path { get; internal set; }
    }

    public class Credentials
    {
        public string? Domain { get; internal set; }
        public required string User { get; set; }
        public required string Pass { get; set; }
    }
}