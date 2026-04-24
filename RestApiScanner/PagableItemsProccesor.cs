using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Demo.RestApiScanner.InHouseClient;

namespace Demo.RestApiScanner
{
    public interface IPagableItemsProccesor<out TItem, out TCriteria>
        where TItem : ICodable
        where TCriteria : ICriteriable
    {
        Task<bool> ProccessItemsAsync(string code, string path);
    }

    public class PagableItemsProccesor<TItem, TCriteria> : IPagableItemsProccesor<TItem, TCriteria>
        where TItem : ICodable
        where TCriteria : ICriteriable, new()
    {
        private string _pageSize = "100";
        readonly Func<InHousePagableFindRequest<TCriteria>, FindPagableResponse<TItem>> _findFunc;
        readonly Func<TItem, string, NodeInfo> _createNodeFromItemFunc;
        List<InHouseSpaceRights> _rights;
        private Func<IPagableItemsProccesor<TItem, TCriteria>> _func;
        private readonly CancellationToken _token;
        private readonly InHouseClient _inHouseClient;
        private readonly List<IPagableItemsProccesor<ICodable, TCriteria>> _nextLevel;

        public PagableItemsProccesor(InHouseClient inHouseClient,
                                     Func<InHousePagableFindRequest<TCriteria>, FindPagableResponse<TItem>> findFunc,
                                     Func<TItem, string, NodeInfo> createNodeFromItem,
                                     List<InHouseSpaceRights> rights,
                                     CancellationToken token,
                                     string pageSize,
                                     List<IPagableItemsProccesor<ICodable, TCriteria>>? nextLevel)
        {
            _inHouseClient = inHouseClient;
            _findFunc = findFunc;
            _createNodeFromItemFunc = createNodeFromItem;
            _rights = rights;
            _token = token;
            _pageSize = pageSize;
            _nextLevel = nextLevel;
        }

        public async Task<bool> ProccessItemsAsync(string code, string path)
        {
            Log.Verbose($"Starting processing items in path: {path}, code {code} for {typeof(TItem).Name}");

            bool hasMore = true;
            for (int page = 0; hasMore; page++)
            {
                var itemsReq = new InHousePagableFindRequest<TCriteria>
                {
                    Filter = new TCriteria { TextSearch = code },
                    Page = new PagingInfo { Page = page.ToString(), Size = _pageSize }
                };

                var itemsResp = _findFunc(itemsReq);
                if (itemsResp == null) return true;

                Log.Debug($"Get RESP with: PageNumber: {itemsResp.PageNumber}, PageSize: {itemsResp.PageSize}, HasNext: {itemsResp.HasNext}");
                _pageSize = itemsResp.PageSize.ToString();

                hasMore = itemsResp?.HasNext ?? false;
                foreach (var itemDetails in itemsResp?.Content)
                {
                    var itemPath = $"{path}/{itemDetails.ItemCode}";
                    if (_inHouseClient.SkipPath(itemPath))
                    {
                        Log.Debug("Item path skiped: {0}", itemPath);
                        return false;
                    }

                    if (itemDetails == null) return false;
                    var node = _createNodeFromItemFunc(itemDetails, path);

                    Log.Debug($"Added Node with ID: {node.Id}, \r\n Name: {node.Name},\r\n Type: {node.Type}, \r\n Path: {node.Path}");
                    _inHouseClient.PublishInfo(node);

                    try
                    {
                        Log.Debug("Get Child Items For Item : {0}, path {1}, Type: {2}", itemDetails.ItemCode, itemPath, typeof(TItem).Name);
                        _nextLevel?.ForEach(async next => await next.ProccessItemsAsync(itemDetails.ItemCode, itemPath));
                    }
                    catch (Exception)
                    {
                        Log.Warning($"Error processing next level for path: {itemPath}");
                    }

                    if (_token.IsCancellationRequested)
                    {
                        Log.Debug("Cancelation Requested");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}