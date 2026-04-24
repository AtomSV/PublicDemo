using System.Text.Json;

namespace Demo.RestApiScanner
{
    public class InHouseScanerProvider : DefaultHttpProvider
    {
        private const string _apiRoot = "rest/api";
        private readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(5);
        private readonly int _requestRetryCount = 5;

        public ClientError LastError { get; internal set; }

        public InHouseScanerProvider(string host,
                                     string domain,
                                     string user,
                                     string pass,
                                     string pinKey,
                                     bool usePinning,
                                     int httpTimeout = 0)
            : base(host, domain, user, pass, pinKey, usePinning, httpTimeout)
        {
        }

        public SpaceUsersGroupsResponse GetSpacePermissions(string spaceCode) =>
            TryApiGet<SpaceUsersGroupsResponse>($"{_apiRoot}/space/v3/{Uri.EscapeDataString(spaceCode)}/permissions", _requestTimeout, _requestRetryCount);

        private T TryApiGet<T>(string v, TimeSpan requestTimeout, int requestRetryCount)
        {
            throw new NotImplementedException();
        }

        public UnitPermissionsResponse GetUnitPermissions(string unitCode) =>
            TryApiGet<UnitPermissionsResponse>($"{_apiRoot}/unit/v1/permissions/{Uri.EscapeDataString(unitCode)}", _requestTimeout, _requestRetryCount);

        public FindPagableResponse<SpaceDto> FindSpaces(InHousePagableFindRequest<FilterCriteria> request)
        {
            var json = JsonSerializer.Serialize(request);
            return ApiPost<FindPagableResponse<SpaceDto>>($"{_apiRoot}/space/v2/find/all", json);
        }

        private T ApiPost<T>(string v, string json)
        {
            throw new NotImplementedException();
        }

        public FindPagableResponse<UnitContent> FindUnits(InHousePagableFindRequest<FilterCriteria> request)
        {
            var json = JsonSerializer.Serialize(request);
            return ApiPost<FindPagableResponse<UnitContent>>($"{_apiRoot}/unit/v2/find", json);
        }

        public FindPagableResponse<FileContentDto> FindFiles(InHousePagableFindRequest<FilterCriteria> request)
        {
            var json = JsonSerializer.Serialize(request);
            return ApiPost<FindPagableResponse<FileContentDto>>($"{_apiRoot}/unit/files/v1/{request.Filter.TextSearch}", json);
        }

        public FindPagableResponse<UnitCommentDto> GetUnitComments(UnitCommentsRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            return ApiPost<FindPagableResponse<UnitCommentDto>>($"{_apiRoot}/unit-comment/v1/find", json);
        }

        public FindPagableResponse<UnitDto> FindUnitsTql(TqlUnitsSearchRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            return ApiPost<FindPagableResponse<UnitDto>>($"{_apiRoot}/unit/v2/find/tql", json);
        }

        public UnitDetailResponse GetUnit(string unitCode) =>
            TryApiGet<UnitDetailResponse>($"{_apiRoot}/unit/v2/{Uri.EscapeDataString(unitCode)}?includeArchived=false", _requestTimeout, _requestRetryCount);

        public ConfidentialLevelResponse GetUnitConfidential(string unitCode) =>
            TryApiGet<ConfidentialLevelResponse>($"{_apiRoot}/unit/v1/confidential/{Uri.EscapeDataString(unitCode)}", _requestTimeout, _requestRetryCount);

        public byte[] DownloadUnitFile(string fileId, long size) =>
            Raw($"{_apiRoot}/unit-files/v1/download?fileId={Uri.EscapeDataString(fileId)}", size);

        private byte[] Raw(string v, long size)
        {
            throw new NotImplementedException();
        }
    }
}