using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Demo.RestApiScanner
{
    public interface ICodable
    {
        string ItemCode { get; }
    }

    public class FindPagableResponse<TItem>
    {
        [JsonPropertyName("content")]
        public List<TItem> Content { get; set; }

        [JsonPropertyName("hasNext")]
        public bool HasNext { get; set; }

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalElements")]
        public int TotalElements { get; set; }
    }

    public class CodeNameDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class CodeNameWithDatesDto : CodeNameDto, IDataTimeDto
    {
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    interface IRelatedUsersDto
    {
        [JsonPropertyName("createdBy")]
        public UserDto CreatedBy { get; set; }

        [JsonPropertyName("updatedBy")]
        public UserDto UpdatedBy { get; set; }
    }

    internal interface IDataTimeDto
    {
        [JsonPropertyName("createdAt")]
        DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        DateTime UpdatedAt { get; set; }
    }

    public class AttributeInfoDto
    {
        public AttributeDto Attribute { get; set; }
        public UserDto Value { get; set; }
    }

    public class UserDto
    {
        [JsonPropertyName("externalId")]
        public string ExternalId { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("middleName")]
        public string MiddleName { get; set; }

        [JsonPropertyName("login")]
        public string Login { get; set; }

        [JsonPropertyName("userDetails")]
        public List<string> UserDetails { get; set; } = new List<string>();

    }

    public class SpaceDto : CodeNameWithDatesDto, ICodable
    {
        [JsonPropertyName("type")]
        public CodeNameDto Type { get; set; }

        [JsonPropertyName("userCount")]
        public int UserCount { get; set; }

        public string ItemCode => Code;
    }

    public class UnitContent : ICodable
    {
        [JsonPropertyName("unit")]
        public UnitDto Unit { get; set; }

        public string ItemCode => Unit.Code;
    }

    public class UnitDto : CodeNameWithDatesDto
    {
        public string Summary { get; set; }
        public string Description { get; set; }
        public SpaceDto Space { get; set; }
        public List<SpaceDto> Spaces { get; set; }
        public UserDto CreatedBy { get; set; }
        public UserDto UpdatedBy { get; set; }
        public bool IsFavorite { get; set; }
        public List<AttributeInfoDto> Attributes { get; set; }
    }

    public class UnitCommentDto : ICodable, IDataTimeDto, IRelatedUsersDto
    {
        [JsonPropertyName("unitCommentCode")]
        public string UnitCommentCode { get; set; }

        [JsonPropertyName("unitCode")]
        public string UnitCode { get; set; }

        [JsonPropertyName("commentText")]
        public string CommentText { get; set; }

        [JsonPropertyName("createdBy")]
        public UserDto CreatedBy { get; set; }

        [JsonPropertyName("updatedBy")]
        public UserDto UpdatedBy { get; set; }

        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string ItemCode => UnitCommentCode;
    }

    public class FileContentDto : ICodable
    {
        public DateTime CreatedAt { get; set; }
        public UserDto CreatedBy { get; set; }
        public FileMetadataDto FileMetadataDto { get; set; }
        public string FileId { get; set; }
        public FilePathParsedDto FilePathParsedDto { get; set; }

        public string ItemCode => FilePathParsedDto.RelativePath;
    }

    public class FileMetadataDto
    {
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
    }

    public class FilePathParsedDto
    {
        public string RelatedIoType { get; set; }
        public string FileName { get; set; }
        public string RelativePath { get; set; }
    }

    public class AttributeDto : CodeNameDto
    {
        public string Type { get; set; }
        public string Description { get; set; }
    }

    public class ConfidentialLevelResponse
    {
        [JsonPropertyName("confidentialLevel")]
        public string ConfidentialLevel { get; set; }
    }

    public class UnitPermissionsResponse
    {
        [JsonPropertyName("users")]
        public List<UserPermissionDto> Users { get; set; } = new List<UserPermissionDto>();

        [JsonPropertyName("groups")]
        public List<GroupPermissionDto> Groups { get; set; } = new List<GroupPermissionDto>();
    }

    public class UserPermissionDto
    {
        [JsonPropertyName("user")]
        public UserDto User { get; set; }

        [JsonPropertyName("permissionGroups")]
        public List<PermissionGroup> PermissionGroups { get; set; } = new List<PermissionGroup>();
    }

    public class GroupPermissionDto
    {
        [JsonPropertyName("group")]
        public GroupInfoDto Group { get; set; }

        [JsonPropertyName("permissionGroups")]
        public List<PermissionGroup> PermissionGroups { get; set; } = new List<PermissionGroup>();
    }

    public class GroupInfoDto
    {
        [JsonPropertyName("permissionGroups")]
        public List<PermissionGroup> PermissionGroups { get; set; }

        [JsonPropertyName("group")]
        public Group Group { get; set; }
    }

    public class PermissionGroup
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class Group : CodeNameDto
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class UnitType : CodeNameDto
    {
        [JsonPropertyName("icon")]
        public string Icon { get; set; }
    }

    public class SpaceUsersGroupsResponse
    {
        [JsonPropertyName("space")]
        public string Space { get; set; }

        [JsonPropertyName("usersInfo")]
        public List<UserDto> UsersInfo { get; set; }

        [JsonPropertyName("groupsInfo")]
        public List<GroupInfoDto> GroupsInfo { get; set; }
    }

    public class UnitDetailResponse : IDataTimeDto
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("isfavorite")]
        public bool IsFavorite { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("suit")]
        public UnitType Suit { get; set; }

        [JsonPropertyName("space")]
        public CodeNameDto Space { get; set; }

        [JsonPropertyName("validatorErrorMsgs")]
        public string ValidatorErrorMsgs { get; set; }
    }
}
