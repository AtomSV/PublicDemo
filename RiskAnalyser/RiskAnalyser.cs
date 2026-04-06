using System.Data;
using Common;
using ClickHouseProvider.Objects;
using ActiveDirectoryAnalyzer.Writers;
using ADStructProvider = ClickHouseProvider.Providers.StructureEntries.LdapStructureEntriesProvider;
using Microsoft.Extensions.Configuration;
using ClickHouseProvider.Providers.StructureEntries;
using System.Text.Json;
using Db.Context;
using System.Text.RegularExpressions;
using Serilog.Sinks.Syslog;
using Serilog;
using static Common.Constants;

namespace RiskAnalyzer
{
    internal class RiskAnalyzer
    {
        [Flags]
        enum RiskSet : long
        {
            None    = 0,
            User    = 0x01,
            Group   = 0x02,
            Org     = 0x04,
            Comp    = 0x08,
            UserCompOrg = User | Org | Comp,
            All = User | Group | Org | Comp,
        }

        class LdapStructureEntryNodeContext
        {
            private readonly string _ntSecurityDescriptor = "ntsecuritydescriptor";            

            internal bool IsAdmin { get; }
            public string[] AclInStrings { get; }
            public List<string> Aces { get; }
            public LdapStructureEntryEnumerable Node { get; } = new LdapStructureEntryEnumerable();
            public string[]? Members { get; }
            public uint Uac { get; }

            public LdapStructureEntryNodeContext(LdapStructureEntryEnumerable node, bool _isFreeIpa, bool _isActiveDirectory, bool _isRedAdm)
            {
                Node = node;
                Members = GetAttribute(node, "member")?.Split(_isFreeIpa || _isActiveDirectory ? ';' : ',', StringSplitOptions.RemoveEmptyEntries);
                Uac = GetAttributeAsUInt32(node, "useraccountcontrol");
                IsAdmin = GetAttributeAsUInt32(node, "admincount") == 1;
                AclInStrings = GetAttribute(node, _ntSecurityDescriptor).Split('\n');
                Aces = AclInStrings.Where(s => s.Contains("ace", StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }
        class RiskCalculator
        {
            private readonly List<KeyValuePair<RiskSet, List<Risk>>> _entityRisksBox = new();
            private readonly bool _isFreeIpa;
            private readonly bool _isActiveDirectory;
            private readonly bool _isRedAdm;

            public RiskCalculator(bool isFreeIpa, bool isActiveDirectory, bool isRedAdm)
            {
                _isFreeIpa = isFreeIpa;
                _isActiveDirectory = isActiveDirectory;
                _isRedAdm = isRedAdm;
            }

            public void AddRiskSet(RiskSet entity, List<Risk> risks)
            {
                _entityRisksBox.Add(new KeyValuePair<RiskSet, List<Risk>>(entity, risks));
            }

            public void Calculate(LdapStructureEntryEnumerable entity)
            {
                var calcFlagType = Convert((StorageEntryType)entity.Type);
                if (calcFlagType == RiskSet.None) return;

                List<ActiveDirectoryEntryFlags> flags = new List<ActiveDirectoryEntryFlags>();
                foreach (var item in _entityRisksBox)
                {
                    if ((calcFlagType | item.Key) == item.Key)
                        item.Value.ForEach(riskEntity =>
                        {
                            var riskFlag = riskEntity.GetRisk(new LdapStructureEntryNodeContext(entity, _isActiveDirectory, _isFreeIpa, _isRedAdm));
                            if (riskFlag != ActiveDirectoryEntryFlags.None)
                                flags.Add(riskFlag);
                        });
                }

                entity.ActiveDirectoryFlags = flags.Cast<int>().ToArray();
            }
            private RiskSet Convert(StorageEntryType type)
            {
                return type switch
                {
                    StorageEntryType.User => RiskSet.User,
                    StorageEntryType.OrganizationalUnit => RiskSet.Org,
                    StorageEntryType.Group => RiskSet.Group,
                    StorageEntryType.Computer => RiskSet.Comp,
                    _ => RiskSet.None,
                };
            }
        }

        class Risk
        {
            public Risk(ActiveDirectoryEntryFlags riskFlag, Func<LdapStructureEntryNodeContext, bool> condition)
            {
                _riskFlag = riskFlag;
                _condition = condition;
            }
            private readonly ActiveDirectoryEntryFlags _riskFlag;
            private readonly Func<LdapStructureEntryNodeContext, bool> _condition;

            public ActiveDirectoryEntryFlags GetRisk(LdapStructureEntryNodeContext context) => 
                _condition(context) ? _riskFlag : ActiveDirectoryEntryFlags.None;
        }

        private static readonly string GROUP = ((int)StorageEntryType.Group).ToString();
        private static readonly string USER = ((int)StorageEntryType.User).ToString();
        private static readonly string ORG_UNIT = ((int)StorageEntryType.OrganizationalUnit).ToString();
        private static readonly string COMPUTER = ((int)StorageEntryType.Computer).ToString();

        private readonly uint DONT_EXPIRE_PASSWORD = 0x10000;
        private readonly uint PASSWORD_EXPIRED = 0x800000;
        private readonly uint PASSWD_NOTREQD = 0x0020;
        private readonly uint PASSWD_CANT_CHANGE = 0x0040;
        private readonly uint DONT_REQ_PREAUTH = 0x400000;
        private readonly uint ACCOUNTDISABLED = 0x0002;
        private readonly uint SERVER_TRUST_ACCOUNT = 0x2000;
        private readonly uint WORKSTATION_TRUST_ACCOUNT = 0x1000;
        private readonly uint NORMAL_USER_ACCOUNT = 0x200;
        private readonly uint USER_PRIMARY_GROUP_ID = 513;
        private readonly uint GUEST_PRIMARY_GROUP_ID = 514;
        private readonly uint GROUP_RID_COMPUTERS = 515;
        private readonly uint GROUP_RID_CONTROLLERS = 516;
        private readonly uint INHERITED_ACE = 0x10;

        private readonly bool _isFreeIpa = false;
        private readonly bool _isActiveDirectory = false;
        private readonly bool _isRedAdm = false;
        private readonly string _columnId = "id";
        private readonly string _columnAttrName = "attr_name";
        private readonly string _columnAttrValue = "attr_value";
        private readonly string _columnName = "name";

        private readonly string _adminAccount = "admincount";
        private readonly string _userAccControl = "useraccountcontrol";
        private readonly string _distinguishedName = "distinguishedname";
        private readonly string _uid = "uid";
        private readonly string _nsaccountLock = "nsaccountlock";

        private readonly string _allExtendedRights = "/1131f6aa-9c07-11d1-f79f-00c04fc2dcd2";
        private readonly int _allExtRightsFlag = 0x100;

        private readonly string _selfCantChangePwdStr = "S-1-5-10:6/0/0x100/ab721a53-1e2f-11d0-9819-00aa0040529b";
        private readonly string _everyoneCantChangePwdStr = "S-1-1-0:6/0/0x100/ab721a53-1e2f-11d0-9819-00aa0040529b";
        private readonly string _getValueFromArraysPairByName = $"{{0}}attr_value[arrayFirstIndex((id)-> (lowerUTF8(id) = '{{1}}'), {{0}}attr_name)]";
        private readonly Regex _acePattern = new Regex(@"\/\d+\/", RegexOptions.Compiled, _regExTimeOut);
        private readonly string _isUserDisisabled = $"if(x.type={USER}, toUInt32OrZero(x.attr_value[arrayFirstIndex((id) -> (id = 'useraccountcontrol'), x.attr_name)]), 0) as is_user_disabled";

        private readonly IConfiguration _configuration;
        private readonly LdapServerType _ldapServerType;

        private readonly int _storeId;
        private readonly string _postgressCon;
        private readonly string _clickhouseCon;
        private RiskAttributes[] _adminGroups;
        private RiskAttributes[] _disabledUsers;
        private IEnumerable<KeyId> _emptyOu;
        private IEnumerable<RiskAttributes> _computersNotSupportedOS;
        private readonly string _subQueryLeftTable;
        private readonly string _subQuery;
        private readonly DbAuthProvider _auth;
        private readonly FsDbContextExt _fsDbContext;
        private readonly ADStructProvider _provider;
        private readonly RisksWriter _writer;
        private readonly List<string> _fields;
        private readonly List<string> _excludeRiskUsersArr;

        private static readonly TimeSpan _regExTimeOut = TimeSpan.FromSeconds(1);
        private object _writerLock = new object();
        private readonly List<StorageEntryType> _allowedTypes = new()
        {
            StorageEntryType.User,
            StorageEntryType.Group,
            StorageEntryType.OrganizationalUnit,
            StorageEntryType.Computer,
            StorageEntryType.BuiltinDomain
        };

        private readonly RiskCalculator _riskCalculator;

        public RiskAnalyzer(int storeId, IConfiguration configuration)
        {
            _configuration = configuration;
            _storeId = storeId;

            _postgressCon = configuration.GetConnectionString("Postgres");
            _clickhouseCon = configuration.GetConnectionString("Clickhouse");

            _fsDbContext = new FsDbContextExt(_postgressCon);

            _auth = new DbAuthProvider(_postgressCon);
            _provider = new LdapStructureEntriesProvider(_clickhouseCon, _auth);

            var excludeRiskUsers = _fsDbContext.LdapServerSettings.Find(storeId)?.ExcludeRiskUsers;
            _excludeRiskUsersArr = !string.IsNullOrWhiteSpace(excludeRiskUsers) ? JsonSerializer.Deserialize<List<string>>(excludeRiskUsers) ?? new List<string>() : new List<string>();

            var ldapSetttings = _fsDbContext.LdapServerSettings.Find(storeId) ?? throw new ArgumentException($"Store with Id: {_storeId} was not found");
            _ldapServerType = (LdapServerType)ldapSetttings.Type;
            _writer = new RisksWriter(_clickhouseCon, _storeId, authProvider: _auth);

            if (_ldapServerType == LdapServerType.FreeIpa)
                _isFreeIpa = true;
            else if (_ldapServerType == LdapServerType.ActiveDirectory)
                _isActiveDirectory = true;
            else if (_ldapServerType == LdapServerType.RedAdm)
                _isRedAdm = true;

            _riskCalculator = new RiskCalculator(_isFreeIpa,  _isActiveDirectory, _isRedAdm);

            _fields = new List<string>
            {
                _columnId,
                _columnAttrName,
                _columnAttrValue,
                _columnName,
                $"toInt32(arrayExists((id) -> (id = '{_everyoneCantChangePwdStr}' OR id ='{_selfCantChangePwdStr}'), acl)) as is_pwds_in_descriptor"
            };
            _subQueryLeftTable = $"{ADStructProvider.Db}.{ADStructProvider.NamePattern}_{_storeId}";
            _subQuery = $" EXCEPT SELECT parent_id FROM {_subQueryLeftTable}";

            // Fill Risks
            var risksUser = new List<Risk>
            {
                new (ActiveDirectoryEntryFlags.Admin, (context) => context.IsAdmin),
                new (ActiveDirectoryEntryFlags.NonGroupAdmin,
                    (context) => context.IsAdmin && !GetAttribute(context.Node, "memberof")
                        .Split(';').Any(x => _adminGroups.Any(y => y.GetAttribute("distinguishedname") == x))),
                new (ActiveDirectoryEntryFlags.LockedOut, (context) => GetAttributeAsUInt32(context.Node, "lockouttime") >= 1),
                new (ActiveDirectoryEntryFlags.NotReqPwd, (context) => (context.Uac & PASSWD_NOTREQD) == PASSWD_NOTREQD),
                new (ActiveDirectoryEntryFlags.ExpiredPwd, (context) => (context.Uac & PASSWORD_EXPIRED) == PASSWORD_EXPIRED),
                new (ActiveDirectoryEntryFlags.NotExpiredPwd, (context) => (context.Uac & DONT_EXPIRE_PASSWORD) == DONT_EXPIRE_PASSWORD),
                new (ActiveDirectoryEntryFlags.DontReqPreAuth, (context) => (context.Uac & DONT_REQ_PREAUTH) == DONT_REQ_PREAUTH),
                new (ActiveDirectoryEntryFlags.DisabledAccount, (context) => _disabledUsers?.Any(e => e.Id == context.Node.Id) == true),
                new (ActiveDirectoryEntryFlags.NextLoginPwdChange, (context) => GetAttribute(context.Node, "pwdlastset") == "0"),
                new (ActiveDirectoryEntryFlags.PwdCantChange,
                    (context) => (context.Uac & PASSWD_CANT_CHANGE) == PASSWD_CANT_CHANGE || IsPwdsInDescriptor(context.Node)),
            };
            var risksGroup = new List<Risk>
            {
                new (ActiveDirectoryEntryFlags.AdminGroup, (context) => GetAttributeAsUInt32(context.Node, "admincount") == 1),
                new (ActiveDirectoryEntryFlags.EmptyGroup, (context) => context.Members?.Length == 0),
                new (ActiveDirectoryEntryFlags.GroupWithDisabledUsers, (context) => context.Members?.Any(x => _disabledUsers.Any(
                                y => y.GetAttribute(_distinguishedName).Equals(x, StringComparison.InvariantCultureIgnoreCase)
                                            || y.GetAttribute(_uid).Equals(x, StringComparison.InvariantCultureIgnoreCase))
                ) == true),
            };
            var risksComp = new List<Risk>
            {
                new (ActiveDirectoryEntryFlags.UnsupportedOS, (context) => _computersNotSupportedOS?.Any(dc => dc.Name == context.Node.Name) == true),
            };
            var risksOrg = new List<Risk>
            {
                new (ActiveDirectoryEntryFlags.EmptyOrganizationalUnit, (context) => _emptyOu ?.Any(o => o.Id == context.Node.Id) == true),
            };
            var risksUserCompOrg = new List<Risk>
            {
                new (ActiveDirectoryEntryFlags.PrimaryGroupIntegrity, PrimaryGroupRisk),
            };
            var risksAll = new List<Risk>
            {
                new (ActiveDirectoryEntryFlags.AllExtendedRights, AllExtendedRightsRisk),
                new (ActiveDirectoryEntryFlags.NonInheritedRights, (context) => !IsInheritedRights(context.AclInStrings)),
                new (ActiveDirectoryEntryFlags.Delegation, (context) => context.Node.Delegation.Length > 0),
            };

            _riskCalculator.AddRiskSet(RiskSet.User, risksUser);
            _riskCalculator.AddRiskSet(RiskSet.Group, risksGroup);
            _riskCalculator.AddRiskSet(RiskSet.Org, risksOrg);
            _riskCalculator.AddRiskSet(RiskSet.Comp, risksComp);
            _riskCalculator.AddRiskSet(RiskSet.UserCompOrg, risksUserCompOrg);
            _riskCalculator.AddRiskSet(RiskSet.All, risksAll);
        }

        public void Init()
        {
            //Админ группы
            _adminGroups = _provider
                .Get<RiskAttributes>(_storeId, $"type={GROUP} AND {GetQueryValueFromArraysPairByName(_adminAccount)} ='1' AND name IS NOT NULL")
                .ToArray();

            //Дизаблед пользователи
            Predicate<RiskAttributes> disabledFilter = _isFreeIpa ? (RiskAttributes x) => x.GetAttribute(_nsaccountLock).ToInt() == 1 :
                (RiskAttributes x) => (x.GetAttribute(_userAccControl).ToInt() & ACCOUNTDISABLED) == ACCOUNTDISABLED;

            _disabledUsers = _provider.Get<RiskAttributes>
                (_storeId, $"type={USER} AND {GetQueryValueFromArraysPairByName(_isFreeIpa ? _nsaccountLock : _userAccControl)} !='' ", _fields)
                .Where(t => disabledFilter(t))
                .ToArray();

            _emptyOu = _provider
                .Get<KeyId>(_storeId, $"type={ORG_UNIT} {_subQuery}", new List<string> { "id" })
                .ToArray();

            var ldapNotSupportedOsComps = _fsDbContext.LdapComputers.Where(comp => comp.LdapId == _storeId).ToArray()
                                .Where(c => UnsupportedOS.IsUnSupportedOS(c.OperatingSystem, c.OperatingSystemVersion.ToString())).ToArray();
            _computersNotSupportedOS = _provider.Get<RiskAttributes>(_storeId, $"type={COMPUTER}", _fields)
                .Where(c => ldapNotSupportedOsComps.Any(l => l.Name == c.Name))
                .ToArray();
        }

        public void AnalysAndSave()
        {
            var start = DateTime.Now;
            Log.Debug($"Risk Analys for storage: {_storeId} have started");
            var entityCount = 0;

            //Анализ Рисков по всему множеству
            foreach (var entity in _provider.Get<LdapStructureEntryEnumerable>(_storeId))
            {
                //запись может долго обрабатываться, соединение потому страемся как можно быстрее взять слейдующую
                ThreadPool.QueueUserWorkItem((state) => _riskCalculator.Calculate(entity));
                lock (_writerLock) { _writer.Add(entity); entityCount++; }
            }

            _writer.Complete();
            _fsDbContext.Dispose();
            Log.Debug($"Start at {start} duration {DateTime.Now - start}");
        }

        private bool PrimaryGroupRisk(LdapStructureEntryNodeContext node)
        {
            var primaryGroupId = GetAttributeAsUInt32(node.Node, "primarygroupid");
            if (primaryGroupId != 0)
            {
                var uac = GetAttributeAsUInt32(node.Node, "userAccountControl");

                if (((uac & NORMAL_USER_ACCOUNT) == NORMAL_USER_ACCOUNT)
                    && primaryGroupId != USER_PRIMARY_GROUP_ID)
                    return true;
                else if (((uac & SERVER_TRUST_ACCOUNT) == SERVER_TRUST_ACCOUNT)
                    && primaryGroupId != GROUP_RID_CONTROLLERS)
                    return true;
                else if (((uac & WORKSTATION_TRUST_ACCOUNT) == WORKSTATION_TRUST_ACCOUNT)
                    && primaryGroupId != GROUP_RID_COMPUTERS)
                    return true;
            }

            return false;
        }

        private bool IsPwdsInDescriptor(LdapStructureEntryEnumerable node) 
            => node.Acl.Any(acl => acl == _selfCantChangePwdStr || acl == _everyoneCantChangePwdStr);

        private bool AllExtendedRightsRisk(LdapStructureEntryNodeContext node)
        {
            foreach (var ace in node.AclInStrings)
                if (ace.Contains(_allExtendedRights, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        private bool IsInheritedRights(string[] acl)
        {
            var aces = acl.Where(s => s.Contains("ace", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var ace in aces)
            {
                var aceFlag = GetAceFlag(ace);
                if ((aceFlag & INHERITED_ACE) == INHERITED_ACE)
                    return true;
            }

            return false;
        }

        private int GetAceFlag(string ace)
        {
            var match = _acePattern.Match(ace);
            if (match.Success)
            {
                if (int.TryParse(match.Value.Trim('/'), out var flag))
                    return flag;
            }

            return 0;
        }

        public static string GetAttribute(LdapStructureEntry entry, string attrName)
        {
            try
            {
                return Array.Find(entry.AttrName, val => val.Equals(attrName, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private string GetQueryValueFromArraysPairByName(string attrName, string table = "")
            => string.Format(_getValueFromArraysPairByName, table, attrName);

        private static uint GetAttributeAsUInt32(LdapStructureEntry entry, string name)
            => uint.TryParse(GetAttribute(entry, name), out var result) ? result : 0;

        private ulong GetAttributeAsULong(LdapStructureEntry entry, string name)
            => ulong.TryParse(GetAttribute(entry, name), out var result) ? result : 0;
    }
}