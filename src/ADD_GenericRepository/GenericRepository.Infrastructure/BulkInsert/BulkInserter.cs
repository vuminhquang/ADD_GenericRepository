using System.Data;
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
            // Generate SQL statements in parallel
            var sqlTasks = new List<Task<string>>();

            for (var i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList.GetRange(i, Math.Min(batchSize, entityList.Count - i));
                sqlTasks.Add(Task.Run(() => GenerateInsertSql(batch)));
            }

            // Wait for all SQL generation tasks to complete
            var sqlStatements = await Task.WhenAll(sqlTasks);

            // Get the underlying connection from the original context
            var connection = _context.Database.GetDbConnection();

            // Ensure the connection is open
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            // Use Parallel.ForEach to execute SQL statements in parallel
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(sqlStatements, sql =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            });

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error: {ex.Message}");
            // Write the exception to file
            File.WriteAllText("error.txt", ex.ToString());
        }
    }

    private string GenerateInsertSql(List<T> entities)
    {
        var entityType = _context.Model.FindEntityType(typeof(T));
        var tableName = entityType.GetTableName();
        var schemaName = entityType.GetSchema();
        var fullTableName = string.IsNullOrEmpty(schemaName) ? tableName : $"{schemaName}.{tableName}";

        var properties = entityType.GetProperties();
        var columnNames = string.Join(", ",
            properties.Select(p => p.GetColumnName(StoreObjectIdentifier.Table(tableName, schemaName))));

        // Precompile value fetchers for all properties
        var valueFetchers = properties.Select(CreateValueFetcher).ToList();

        var sb = new StringBuilder();
        sb.Append($"INSERT INTO {fullTableName} ({columnNames}) VALUES ");

        var valuesList = new List<string>(entities.Count);

        foreach (var entity in entities)
        {
            var values = valueFetchers.Select(fetcher => fetcher(entity)).Select(FormatValue);
            valuesList.Add($"({string.Join(", ", values)})");
        }

        sb.Append(string.Join(", ", valuesList));
        sb.Append(';');

        return sb.ToString();
    }

    private Func<T, object> CreateValueFetcher(IProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(property.PropertyInfo);

        var parameter = Expression.Parameter(typeof(T), "entity");
        var propertyAccess = Expression.Property(parameter, property.PropertyInfo);

        if (property.IsForeignKey())
        {
            var foreignKey = property.GetContainingForeignKeys().FirstOrDefault();
            var principalKey = foreignKey?.PrincipalKey.Properties[0];

            if (principalKey != null && principalKey.PropertyInfo != null)
            {
                // Debugging information, open it to see the values
                // Console.WriteLine($"property.PropertyInfo.Name: {property.PropertyInfo.Name}");
                // Console.WriteLine($"property.PropertyInfo.PropertyType: {property.PropertyInfo.PropertyType}");
                // Console.WriteLine($"principalKey.PropertyInfo.Name: {principalKey.PropertyInfo.Name}");
                // Console.WriteLine($"principalKey.PropertyInfo.PropertyType: {principalKey.PropertyInfo.PropertyType}");

                // Check if propertyAccess type is Guid
                if (propertyAccess.Type == typeof(Guid))
                {
                    // Return the Guid value directly
                    var convertGuid = Expression.Convert(propertyAccess, typeof(object));
                    return Expression.Lambda<Func<T, object>>(convertGuid, parameter).Compile();
                }

                var pkProperty = Expression.Property(propertyAccess, principalKey.PropertyInfo);
                var convertPkProperty = Expression.Convert(pkProperty, typeof(object));
                return Expression.Lambda<Func<T, object>>(convertPkProperty, parameter).Compile();
            }
        }

        var convertEntityProperty = Expression.Convert(propertyAccess, typeof(object));
        return Expression.Lambda<Func<T, object>>(convertEntityProperty, parameter).Compile();
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