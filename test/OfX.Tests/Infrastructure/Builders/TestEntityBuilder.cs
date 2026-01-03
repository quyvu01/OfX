namespace OfX.Tests.Infrastructure.Builders;

/// <summary>
/// Base builder for test entities with fluent API
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TBuilder">The builder type (for fluent returns)</typeparam>
public abstract class TestEntityBuilder<TEntity, TBuilder>
    where TEntity : class, new()
    where TBuilder : TestEntityBuilder<TEntity, TBuilder>
{
    protected TEntity Entity { get; set; }

    protected TestEntityBuilder()
    {
        Entity = new TEntity();
        SetDefaults();
    }

    /// <summary>
    /// Override to set default values for the entity
    /// </summary>
    protected virtual void SetDefaults()
    {
    }

    /// <summary>
    /// Build the entity
    /// </summary>
    public TEntity Build() => Entity;

    /// <summary>
    /// Build multiple entities with different IDs
    /// </summary>
    public List<TEntity> BuildMany(int count)
    {
        var entities = new List<TEntity>();
        for (int i = 0; i < count; i++)
        {
            entities.Add(Build());
        }
        return entities;
    }

    /// <summary>
    /// Returns this for fluent chaining
    /// </summary>
    protected TBuilder This() => (TBuilder)this;
}
