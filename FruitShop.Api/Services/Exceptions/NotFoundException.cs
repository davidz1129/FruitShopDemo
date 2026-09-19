namespace FruitShop.Api.Services;

public sealed class NotFoundException(string entityName, long entityId) : Exception($"{entityName} with id '{entityId}' was not found.")
{
    public string EntityName { get; } = entityName;
    public long EntityId { get; } = entityId;
}