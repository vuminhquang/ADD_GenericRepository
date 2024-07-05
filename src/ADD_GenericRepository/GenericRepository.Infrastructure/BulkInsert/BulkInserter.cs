using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GenericRepository.Infrastructure.BulkInsert;

public class BulkInserter<T> where T : class
{
    private readonly DbContext _context;

    public BulkInserter(DbContext context)
    {
        _context = context;
    }

    public async Task BulkInsertAsync(IEnumerable<T> entities, int batchSize = 1000)
    {
        var entityList = new List<T>(entities);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var entity in entityList)
            {
                await AddOrUpdateRelatedEntities(entity);
            }

            // Generate SQL statements in parallel
            var sqlTasks = new List<Task<string>>();

            for (var i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList.GetRange(i, Math.Min(batchSize, entityList.Count - i));
                sqlTasks.Add(Task.Run(() => GenerateInsertSql(batch)));
            }

            // Wait for all SQL generation tasks to complete
            var sqlStatements = await Task.WhenAll(sqlTasks);

            // Execute the generated SQL statements sequentially or in parallel
            var executeTasks = new ConcurrentBag<Task<int>>();

            // use parallel.foreach to create a task for each sql statement
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(sqlStatements, parallelOptions, sql =>
            {
                executeTasks.Add(_context.Database.ExecuteSqlRawAsync(sql));
            });
            
            await Task.WhenAll(executeTasks);

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task AddOrUpdateRelatedEntities(T entity)
    {
        var entityType = _context.Model.FindEntityType(typeof(T));
        foreach (var navigation in entityType.GetNavigations())
        {
            var relatedEntity = navigation.PropertyInfo.GetValue(entity);
            if (relatedEntity != null)
            {
                var relatedEntityType = relatedEntity.GetType();
                var relatedEntityEntry = _context.Entry(relatedEntity);
                if (relatedEntityEntry.State == EntityState.Detached)
                {
                    var primaryKey = navigation.ForeignKey.PrincipalKey.Properties.First();
                    var primaryKeyValue = primaryKey.PropertyInfo.GetValue(relatedEntity);

                    var dbSetMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), new Type[] { });
                    var dbSet = dbSetMethod.MakeGenericMethod(relatedEntityType).Invoke(_context, null);
                    var findAsyncMethod = dbSet.GetType().GetMethod(nameof(DbSet<object>.FindAsync), new[] { typeof(object[]) });
                    var existingEntityTask = (Task)findAsyncMethod.Invoke(dbSet, new object[] { new object[] { primaryKeyValue } });
                    await existingEntityTask.ConfigureAwait(false);

                    var existingEntity = existingEntityTask.GetType().GetProperty("Result").GetValue(existingEntityTask);

                    if (existingEntity != null)
                    {
                        _context.Entry(existingEntity).CurrentValues.SetValues(relatedEntity);
                    }
                    else
                    {
                        var addMethod = dbSet.GetType().GetMethod(nameof(DbSet<object>.Add));
                        addMethod.Invoke(dbSet, new[] { relatedEntity });
                    }
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    private string GenerateInsertSql(List<T> entities)
    {
        var entityType = _context.Model.FindEntityType(typeof(T));
        var tableName = entityType.GetTableName();
        var schemaName = entityType.GetSchema();
        var fullTableName = string.IsNullOrEmpty(schemaName) ? tableName : $"{schemaName}.{tableName}";

        var properties = entityType.GetProperties();
        var columnNames = string.Join(", ", properties.Select(p => p.GetColumnName(StoreObjectIdentifier.Table(tableName, schemaName))));

        var valueFetchers = properties.Select(CreateValueFetcher).ToList();

        var sb = new StringBuilder();
        sb.Append($"INSERT INTO {fullTableName} ({columnNames}) VALUES ");

        for (int i = 0; i < entities.Count; i++)
        {
            var values = valueFetchers.Select(fetcher => fetcher(entities[i])).Select(FormatValue);
            sb.Append($"({string.Join(", ", values)})");

            if (i < entities.Count - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append(";");
        return sb.ToString();
    }

    private Func<T, object> CreateValueFetcher(IProperty property)
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        var propertyAccess = Expression.Property(parameter, property.PropertyInfo);

        if (property.IsForeignKey())
        {
            var foreignKey = property.GetContainingForeignKeys().FirstOrDefault();
            if (foreignKey != null)
            {
                var principalKey = foreignKey.PrincipalKey.Properties.FirstOrDefault();
                if (principalKey != null)
                {
                    var pkProperty = Expression.Property(propertyAccess, principalKey.PropertyInfo);
                    var convert = Expression.Convert(pkProperty, typeof(object));
                    return Expression.Lambda<Func<T, object>>(convert, parameter).Compile();
                }
            }
        }

        var convertProperty = Expression.Convert(propertyAccess, typeof(object));
        return Expression.Lambda<Func<T, object>>(convertProperty, parameter).Compile();
    }
    
    private static string FormatValue(object? value)
    {
        if (value == null)
            return "NULL";

        return value switch
        {
            bool boolValue => boolValue ? "1" : "0",
            string stringValue => $"'{stringValue.Replace("'", "''")}'",
            Guid guidValue => $"'{guidValue}'",
            DateTime dateTimeValue => $"'{dateTimeValue:yyyy-MM-dd HH:mm:ss.fff}'",
            _ => value.ToString() ?? "NULL"
        };
    }
}