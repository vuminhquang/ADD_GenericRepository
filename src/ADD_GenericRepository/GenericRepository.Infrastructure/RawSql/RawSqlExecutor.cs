using System.Collections;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace GenericRepository.Infrastructure.RawSql;

public static class RawSqlExecutor
{
    public static IList<dynamic> ExecuteRawSql(DbContext context, string sql)
    {
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        context.Database.OpenConnection();

        using var result = command.ExecuteReader();
        var dataTable = new DataTable();
        dataTable.Load(result);

        var dynamicType = DynamicTypeBuilder.CreateTypeFromDataTable(dataTable);
        var genericListType = typeof(List<>).MakeGenericType(dynamicType);
        var list = (IList)Activator.CreateInstance(genericListType);

        foreach (DataRow row in dataTable.Rows)
        {
            var obj = Activator.CreateInstance(dynamicType);
            foreach (DataColumn column in dataTable.Columns)
            {
                var property = dynamicType.GetProperty(column.ColumnName);
                var value = row[column] == DBNull.Value ? null : row[column];
                property.SetValue(obj, value);
            }
            list.Add(obj);
        }

        return list.Cast<dynamic>().ToList();
    }
}