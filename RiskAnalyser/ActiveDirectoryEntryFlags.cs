namespace Demo
{
    internal enum ActiveDirectoryEntryFlags
    {
        None,
        Admin,
        NonGroupAdmin,
        LockedOut,
        NotReqPwd,
        ExpiredPwd,
        NotExpiredPwd,
        DontReqPreAuth,
        DisabledAccount,
        NextLoginPwdChange,
        PwdCantChange,
        AdminGroup,
        EmptyGroup,
        GroupWithDisabledUsers,
        UnsupportedOS,
        EmptyOrganizationalUnit,
        PrimaryGroupIntegrity,
        AllExtendedRights,
        NonInheritedRights,
        Delegation
    }
}