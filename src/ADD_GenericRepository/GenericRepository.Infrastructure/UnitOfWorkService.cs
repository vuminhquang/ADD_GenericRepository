using GenericRepository.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GenericRepository.Infrastructure
{
    public class UnitOfWorkService : IUnitOfWorkService
    {
        private readonly DbContext _context;
        private IDbContextTransaction? _currentTransaction;
        private readonly Dictionary<Type, dynamic> _repositories = new();
        private bool _disposed = false;

        public UnitOfWorkService(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            var type = typeof(TEntity);
            if (_repositories.TryGetValue(type, out var value)) return (IRepository<TEntity>)value;
            value = new Repository<TEntity>(_context);
            _repositories[type] = value;
            return (IRepository<TEntity>)value;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _repositories.Clear();
                _context.Dispose();
            }

            _disposed = true;
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void BeginTransaction()
        {
            _currentTransaction = _context.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            try
            {
                _context.SaveChanges();
                _currentTransaction?.Commit();
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
            finally
            {
                _currentTransaction?.Dispose();
                _currentTransaction = null;
            }
        }

        public void RollbackTransaction()
        {
            _currentTransaction?.Rollback();
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }

        public async Task BatchInsertOrUpdateAsync(int batchSize = 1000)
        {
            var insertEntities = _context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList();
            var updateEntities = _context.ChangeTracker.Entries().Where(e => e.State == EntityState.Modified).ToList();
            var deleteEntities = _context.ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted).ToList();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (insertEntities.Count != 0)
                {
                    await BatchInsertAsync(insertEntities, batchSize);
                }

                if (updateEntities.Count != 0)
                {
                    await BatchUpdateAsync(updateEntities, batchSize);
                }

                if (deleteEntities.Count != 0)
                {
                    await BatchDeleteAsync(deleteEntities, batchSize);
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task BatchInsertAsync(List<EntityEntry> entities, int batchSize)
        {
            var groupedEntities = entities.GroupBy(e => e.Entity.GetType());

            foreach (var group in groupedEntities)
            {
                var entityType = group.Key;
                var tableName = _context.Model.FindEntityType(entityType)?.GetTableName();
                if (tableName == null) continue;
                
                var properties = entityType.GetProperties();
                var totalBatches = (int)Math.Ceiling((double)group.Count() / batchSize);

                for (int i = 0; i < totalBatches; i++)
                {
                    var batch = group.Skip(i * batchSize).Take(batchSize).Select(e => e.Entity).ToList();
                    var insertCommand = BuildInsertCommand(batch, tableName, properties);
                    await _context.Database.ExecuteSqlRawAsync(insertCommand);
                }
            }
        }

        private async Task BatchUpdateAsync(List<EntityEntry> entities, int batchSize)
        {
            var groupedEntities = entities.GroupBy(e => e.Entity.GetType());

            foreach (var group in groupedEntities)
            {
                var entityType = group.Key;
                var tableName = _context.Model.FindEntityType(entityType)?.GetTableName();
                if (tableName == null) continue;

                var properties = entityType.GetProperties();
                var keyProperties = _context.Model.FindEntityType(entityType)?.FindPrimaryKey()?.Properties;
                if (keyProperties == null) continue;

                var totalBatches = (int)Math.Ceiling((double)group.Count() / batchSize);

                for (var i = 0; i < totalBatches; i++)
                {
                    var batch = group.Skip(i * batchSize).Take(batchSize).Select(e => e.Entity).ToList();
                    var updateCommand = BuildUpdateCommand(batch, tableName, properties, keyProperties);
                    await _context.Database.ExecuteSqlRawAsync(updateCommand);
                }
            }
        }

        private static string BuildUpdateCommand(List<object> entities, string tableName, PropertyInfo[] properties,
            IReadOnlyList<IProperty> keyProperties)
        {
            var sb = new StringBuilder();

            foreach (var entity in entities)
            {
                sb.Append($"UPDATE {tableName} SET ");

                var nonKeyProperties = properties.Where(p => !keyProperties.Any(k => k.Name == p.Name)).ToList();
                sb.Append(string.Join(", ",
                    nonKeyProperties.Select(p => $"{p.Name} = {FormatValue(p.GetValue(entity))}")));

                sb.Append(" WHERE ");
                sb.Append(string.Join(" AND ",
                    keyProperties.Select(k => 
                    {
                        var value = entity.GetType().GetProperty(k.Name)?.GetValue(entity);
                        return $"{k.Name} = {FormatValue(value)}";
                    })));
                sb.Append("; ");
            }

            return sb.ToString();
        }

        private async Task BatchDeleteAsync(List<EntityEntry> entities, int batchSize)
        {
            var groupedEntities = entities.GroupBy(e => e.Entity.GetType());

            foreach (var group in groupedEntities)
            {
                var entityType = group.Key;
                var tableName = _context.Model.FindEntityType(entityType)?.GetTableName();
                if (tableName == null) continue;

                var keyProperties = _context.Model.FindEntityType(entityType)?.FindPrimaryKey()?.Properties;
                if (keyProperties == null) continue;

                var totalBatches = (int)Math.Ceiling((double)group.Count() / batchSize);

                for (int i = 0; i < totalBatches; i++)
                {
                    var batch = group.Skip(i * batchSize).Take(batchSize).Select(e => e.Entity).ToList();
                    var deleteCommand = BuildDeleteCommand(batch, tableName, keyProperties);
                    await _context.Database.ExecuteSqlRawAsync(deleteCommand);
                }
            }
        }

        private static string BuildDeleteCommand(List<object> entities, string tableName,
            IReadOnlyList<IProperty> keyProperties)
        {
            var sb = new StringBuilder();

            foreach (var entity in entities)
            {
                sb.Append($"DELETE FROM {tableName} WHERE ");
                sb.Append(string.Join(" AND ",
                    keyProperties.Select(k => 
                    {
                        var value = entity.GetType().GetProperty(k.Name)?.GetValue(entity);
                        return $"{k.Name} = {FormatValue(value)}";
                    })));
                sb.Append("; ");
            }

            return sb.ToString();
        }

        private static string BuildInsertCommand<T>(List<T> entities, string tableName, PropertyInfo[] properties)
            where T : class
        {
            var sb = new StringBuilder();
            sb.Append($"INSERT INTO {tableName} (");

            sb.Append(string.Join(", ", properties.Select(p => p.Name)));
            sb.Append(") VALUES ");

            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                sb.Append('(');

                sb.Append(string.Join(", ", properties.Select(p => FormatValue(p.GetValue(entity)))));
                sb.Append(')');

                if (i < entities.Count - 1)
                {
                    sb.Append(", ");
                }
            }

            return sb.ToString();
        }

        private static string FormatValue(object? value)
        {
            if (value == null) return "NULL";

            return value switch
            {
                string str => $"'{str.Replace("'", "''")}'",
                DateTime date => $"'{date:yyyy-MM-dd HH:mm:ss}'",
                bool boolean => boolean ? "1" : "0",
                _ => value.ToString() ?? "NULL"
            };
        }
    }
}