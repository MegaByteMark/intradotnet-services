using IntraDotNet.Results;

namespace IntraDotNet.Services;

/// <summary>
/// Defines the base service interface for entities that require validation.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <remarks>
/// This interface extends the base service interface to include a method for validating entities.
/// It is designed to be implemented by services that manage entities with validation requirements.
/// </remarks>
public interface IBaseValidatableService<TEntity> : IBaseService<TEntity> where TEntity : class
{
    Task<ValueResult<bool>> ValidateAsync(TEntity entity);
}
