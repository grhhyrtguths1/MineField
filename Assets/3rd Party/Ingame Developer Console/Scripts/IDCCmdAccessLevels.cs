namespace IDC
{
    [System.Flags]
    public enum AccessLevel
    {
        EditorOnly = 1,
        ProductionBuildOnly = 2,
        DevBuildOnly = 4,
        EditorAndDevBuild = EditorOnly | DevBuildOnly,
        AnyBuild = ProductionBuildOnly | DevBuildOnly,
        Everywhere = AnyBuild | EditorOnly
    }
}