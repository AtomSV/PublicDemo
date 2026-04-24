using Serilog;

namespace Demo.RestApiScanner
{
    public class InHouseClient : Client
    {
        private bool _disposed = false;
        private string? _savePoint;
        private string[] _locations = new string[0];
        private NodeInfo _lastNode;
        private CancellationToken _token;
        private List<InHouseSpaceRights> _rights;
        private readonly InHouseScanerProvider _provider;
        private readonly ClientConfig _config;
        private readonly string _pageSize = "100";
        private readonly PagableItemsProccesor<SpaceDto, FilterCriteria> _spaceProccesor;

        public object LastError { get; private set; }

        public event Action<NodeInfo> OnInfo;

        public InHouseClient(ClientConfig config)
        {
            _config = config;
            _provider = new InHouseScanerProvider(
                _config.Host,
                _config.Credentials.Domain,
                _config.Credentials.User,
                _config.Credentials.Pass,
                _config.PinKey,
                _config.UsePinning,
                _config.HttpRequestTimeout
            );
            IPagableItemsProccesor<UnitCommentDto, FilterCriteria> commentProccesor = new PagableItemsProccesor<UnitCommentDto, FilterCriteria>(
                this,
                GetUnitComments,
                (commentDetails, path) =>
                {
                    var commentPath = $"/{path}/{commentDetails.UnitCommentCode}";
                    return CreateNode(
                        id: commentDetails.UnitCommentCode,
                        level: 2,
                        path: path,
                        name: commentDetails.UnitCommentCode,
                        owner: commentDetails.CreatedBy?.FirstName,
                        size: commentDetails.CommentText?.Length ?? 0,
                        type: StorageEntryType.Page,
                        created: commentDetails.CreatedAt,
                        updated: commentDetails.UpdatedAt,
                        rights: _rights,
                        attr: GetCommentAttributes(commentDetails)
                        );
                }, _rights, _token, _pageSize, null);

            IPagableItemsProccesor<ICodable, FilterCriteria> fileProccesor = new PagableItemsProccesor<FileContentDto, FilterCriteria>(
                this,
                _provider.FindFiles,
                (fileDetails, path) =>
                {
                    var filePath = $"{path}/{fileDetails.ItemCode}";
                    return CreateNode(
                        id: fileDetails.FileId,
                        level: 2,
                        name: fileDetails.FilePathParsedDto.FileName,
                        path,
                        owner: fileDetails.CreatedBy?.FirstName,
                        size: fileDetails.FileMetadataDto?.ContentLength ?? 0,
                        type: StorageEntryType.File,
                        created: fileDetails.CreatedAt,
                        updated: DateTime.MinValue,
                        rights: _rights,
                        attr: GetFileAttributes(fileDetails));
                }, _rights, _token, _pageSize, null);

            IPagableItemsProccesor<ICodable, FilterCriteria> unitProccesor = new PagableItemsProccesor<UnitContent, FilterCriteria>(
                this,
                _provider.FindUnits,
                (unitDetails, path) =>
                {
                    var unitPath = $"{path}/{unitDetails.Unit.Code}";
                    return CreateNode(
                        id: unitDetails.Unit.Code,
                        level: 1,
                        name: unitDetails.Unit.Code,
                        path: path,
                        owner: unitDetails.Unit.Space.Name,
                        size: unitDetails.Unit.Description?.Length ?? 0,
                        type: StorageEntryType.Issue,
                        created: unitDetails.Unit.CreatedAt,
                        updated: unitDetails.Unit.UpdatedAt,
                        attr: GetUnitAttributes(unitDetails.Unit),
                        rights: _rights
                        );
                }, _rights, _token, _pageSize
                , new List<IPagableItemsProccesor<ICodable, FilterCriteria>> { commentProccesor, fileProccesor });

            _spaceProccesor = new PagableItemsProccesor<SpaceDto, FilterCriteria>(
                this,
                _provider.FindSpaces,
                (spaceDetails, path) =>
                {
                    _rights = BuildSpaceRights(spaceDetails.Code);
                    var unitPath = $"/{path}/{spaceDetails.ItemCode}";
                    return CreateNode(
                        id: spaceDetails.Code,
                        path: "",
                        name: spaceDetails.Code,
                        level: 0,
                        size: 0,
                        owner: null,
                        type: StorageEntryType.Space,
                        created: spaceDetails.CreatedAt,
                        updated: spaceDetails.UpdatedAt,
                        attr: GetSpaceAttributes(spaceDetails),
                        rights: _rights);
                }, _rights, _token, _pageSize
                , new List<IPagableItemsProccesor<ICodable, FilterCriteria>> { unitProccesor });
        }

        ~InHouseClient()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        public override ClientErrorInfo Check()
        {
            var req = new InHousePagableFindRequest<FilterCriteria>
            {
                Filter = new FilterCriteria { TextSearch = string.Empty },
                Page = new PagingInfo { Page = "0", Size = "1" }
            };

            var spaces = _provider.FindSpaces(req);
            var err = _provider.LastError;
            Log.Debug($"Checked conn : {err}");
            return new ClientErrorInfo(err != ClientError.None ? err : (spaces?.Content?.Any() == false ? ClientError.DataError : ClientError.None));
        }

        public override async Task<NodeInfo?> GetTreeAsync(CancellationToken token, string? savePoint = null)
        {
            _savePoint = savePoint;
            _locations = _config.Path ?? new string[0];
            Log.Information("Start scanning VWord for locations {0} with save point {1}", string.Join(";", _locations ?? Array.Empty<string>()), _savePoint);
            try
            {
                _ = await _spaceProccesor.ProccessItemsAsync("", "");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during start scanning of VWord");
                LastError = _provider.LastError;
                return null;
            }

            var root = new NodeInfo
            {
                Level = 0,
                Name = "/",
                Path = "",
                Type = StorageEntryType.Directory
            };

            return root;
        }

        public bool SkipPath(string path)
        {
            if (_locations.Length == 0 || _locations.Any(path.StartsWith))
            {
                Log.Debug($"Matched location in {path}");

                if (_savePoint == null) return false;

                if (path.Equals(_savePoint, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Debug($"Save point found: {path}");
                    _savePoint = null;
                    return true;
                }
            }

            return true;
        }

        public class InHouseId
        {
            public string Id { get; set; } = string.Empty;
            public StorageEntryType Type { get; set; } = StorageEntryType.Directory;
        }

        public class InHouseSpaceRights
        {
            public string Type { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string ExternalId { get; set; } = string.Empty;
            public string Permissions { get; set; } = string.Empty;
        }

        private NodeInfo CreateNode(string id,
                                long level,
                                string name,
                                string path,
                                string? owner,
                                long size,
                                StorageEntryType type,
                                DateTime created,
                                DateTime updated,
                                List<InHouseSpaceRights> rights,
                                Dictionary<string, string> attr)
        {
            return new NodeInfo
            {
                Id = id,
                Level = level,
                Name = name,
                Path = path,
                Owner = owner,
                Type = type,
                Create = created,
                Update = updated,
                Size = size,
                Acls = ConvertToAclStructure(rights),
                Attrs = attr
            };
        }

        private string[] ConvertToAclStructure(List<InHouseSpaceRights> rights)
        {
            throw new NotImplementedException();
        }

        public override byte[] Read(string path, long size)
        {
            try
            {
                return _provider.DownloadUnitFile(path, size);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error during read from id " + path);
            }

            return Array.Empty<byte>();
        }

        public FindPagableResponse<UnitCommentDto> GetUnitComments(InHousePagableFindRequest<FilterCriteria> request)
            => _provider.GetUnitComments(new UnitCommentsRequest { Page = request.Page, UnitCode = request.Filter.TextSearch });

        private List<InHouseSpaceRights> BuildSpaceRights(string spaceCode)
        {
            var rights = new List<InHouseSpaceRights>();
            try
            {
                var permissions = _provider.GetSpacePermissions(spaceCode);
                if (permissions?.GroupsInfo != null)
                {
                    foreach (var g in permissions.GroupsInfo)
                    {
                        rights.Add(new InHouseSpaceRights
                        {
                            Type = "Group",
                            Name = g.Group?.Name ?? string.Empty,
                            ExternalId = g.Group?.Code ?? string.Empty,
                            Permissions = string.Join(", ", (g.PermissionGroups ?? new List<PermissionGroup>()).Select(pg => pg.Name))
                        });
                    }
                }

                if (permissions?.UsersInfo != null)
                {
                    foreach (var u in permissions.UsersInfo)
                    {
                        rights.Add(new InHouseSpaceRights
                        {
                            Type = "User",
                            Name = $"{u.FirstName} {u.LastName}",
                            ExternalId = u.ExternalId,
                            Permissions = string.Empty
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error building space permissions for {0}", spaceCode);
            }

            return rights;
        }

        private static Dictionary<string, string> GetSpaceAttributes(SpaceDto space)
        {
            var attr = new Dictionary<string, string>();
            try
            {
                if (space.Type != null)
                {
                    attr.Add("TypeCode", space.Type.Code);
                    attr.Add("TypeName", space.Type.Name);
                }

                attr.Add(nameof(space.Name), space.Name);
                attr.Add(nameof(space.Code), space.Code);
                attr.Add(nameof(space.CreatedAt), space.CreatedAt.ToString());
                attr.Add(nameof(space.UpdatedAt), space.UpdatedAt.ToString());
                attr.Add(nameof(space.UserCount), space.UserCount.ToString());
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error during GetSpaceAttributes");
            }

            return attr;
        }

        private static Dictionary<string, string> GetUnitAttributes(UnitDto unit)
        {
            var attr = new Dictionary<string, string>();
            try
            {
                attr.Add("Code", unit.Code);
                if (!string.IsNullOrEmpty(unit.Summary)) attr.Add("Summary", unit.Summary);
                if (!string.IsNullOrEmpty(unit.Description)) attr.Add("Description", unit.Description);
                attr.Add("IsFavorite", unit.IsFavorite.ToString());
                if (unit.Space != null) attr.Add("Space", unit.Space.Name);

                foreach (var unitAttr in unit?.Attributes)
                {
                    if (unitAttr.Attribute != null && !string.IsNullOrEmpty(unitAttr.Attribute.Name))
                    {
                        var value = unitAttr.Value?.ToString() ?? "null";
                        if (!attr.ContainsKey(unitAttr.Attribute.Name))
                            attr.Add(unitAttr.Attribute.Name, value);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error during GetUnitAttributes");
            }

            return attr;
        }

        private static Dictionary<string, string> GetCommentAttributes(UnitCommentDto comment)
        {
            var attr = new Dictionary<string, string>();
            try
            {
                attr.Add(nameof(comment.UnitCommentCode), comment.UnitCommentCode);
                attr.Add(nameof(comment.UnitCode), comment.UnitCode);
                attr.Add(nameof(comment.ItemCode), comment.ItemCode);
                attr.Add(nameof(comment.CommentText), comment.CommentText);
                attr.Add(nameof(comment.Deleted), comment.Deleted.ToString());
                attr.Add(nameof(comment.CreatedAt), comment.CreatedAt.ToString());
                attr.Add(nameof(comment.UpdatedAt), comment.UpdatedAt.ToString());

                if (comment.UpdatedBy != null) attr.Add(nameof(comment.UpdatedBy), $"{comment.UpdatedBy.FirstName} {comment.UpdatedBy.LastName}");
                if (comment.CreatedBy != null) attr.Add(nameof(comment.CreatedBy), $"{comment.CreatedBy.FirstName} {comment.CreatedBy.LastName}");
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error during GetCommentAttributes");
            }

            return attr;
        }

        private Dictionary<string, string> GetFileAttributes(FileContentDto fileDetails)
        {
            var attr = new Dictionary<string, string>();
            try
            {
                if (fileDetails.FileMetadataDto != null)
                {
                    attr.Add(nameof(fileDetails.FileMetadataDto.ContentType), fileDetails.FileMetadataDto.ContentType ?? string.Empty);
                    attr.Add(nameof(fileDetails.FileMetadataDto.ContentLength), fileDetails.FileMetadataDto.ContentLength.ToString() ?? string.Empty);
                }
                if (fileDetails.FilePathParsedDto != null)
                {
                    attr.Add(nameof(fileDetails.FilePathParsedDto.RelatedIoType), fileDetails.FilePathParsedDto.RelatedIoType ?? string.Empty);
                    attr.Add(nameof(fileDetails.FilePathParsedDto.FileName), fileDetails.FilePathParsedDto.FileName ?? string.Empty);
                    attr.Add(nameof(fileDetails.FilePathParsedDto.RelativePath), fileDetails.FilePathParsedDto.RelativePath ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error during GetFileAttributes");
            }
            return attr;
        }

        internal void PublishInfo(NodeInfo node)
        {
            OnInfo?.Invoke(node);
        }
    }
}
