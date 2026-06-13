namespace LucidMicro.BuildingBlocks.Persistence.EFCore.Conventions;

public static class SoftDeleteRelationalConventions
{
    public const string IsDeletedColumnName = "is_deleted";

    public const string IsNotDeletedFilter = IsDeletedColumnName + " = false";
}
