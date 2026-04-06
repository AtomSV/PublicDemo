using Amazon.Runtime.Internal;
using FsClients.Seafile.Api;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Demo.RestApiScanner
{
    public interface IPageable
    {
        PagingInfo Page { get; set; }
    }

    public interface IRequest<out TCriteria>
        where TCriteria : ICriteriable, new()
    {
    }

    public class PagingInfo
    {
        [JsonPropertyName("page")]
        public string Page { get; set; }

        [JsonPropertyName("size")]
        public string Size { get; set; }
    }

    public class InHousePagableFindRequest<TFilter> : IPageable, IRequest<TFilter>
        where TFilter : ICriteriable, new()
    {
        [JsonPropertyName("filter")]
        public TFilter Filter { get; set; }

        [JsonPropertyName("page")]
        public PagingInfo Page { get; set; }
    }

    public class UnitCommentsRequest : IPageable
    {
        [JsonPropertyName("unitCode")]
        public string UnitCode { get; set; }

        [JsonPropertyName("page")]
        public PagingInfo Page { get; set; } = new PagingInfo();
    }

    public interface ICriteriable
    {
        string TextSearch { get; set; }
    }

    public class FilterCriteria : ICriteriable
    {
        [JsonPropertyName("textSearch")]
        public string TextSearch { get; set; }
    }

    public class UnitCriteria : ICriteriable
    {
        [JsonPropertyName("spaces")]
        public List<string> Spaces { get; set; }

        public string TextSearch { get => Spaces.FirstOrDefault(); set => Spaces = new List<string> { value }; }
    }

    public class TqlUnitsSearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }

        [JsonPropertyName("page")]
        public PagingInfo Page { get; set; } = new PagingInfo();
    }
}