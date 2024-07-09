using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace GenericRepository.Infrastructure.RawSql;
public static class DynamicTypeBuilder
{
    public static Type CreateTypeFromDataTable(DataTable table)
    {
        var typeBuilder = GetTypeBuilder("DynamicAssembly", "DynamicModule", "DynamicType");

        foreach (DataColumn column in table.Columns)
        {
            CreateProperty(typeBuilder, column.ColumnName, column.DataType);
        }

        return typeBuilder.CreateType();
    }

    private static TypeBuilder GetTypeBuilder(string assemblyName, string moduleName, string typeName)
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
        return moduleBuilder.DefineType(typeName, TypeAttributes.Public);
    }

    private static void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
    {
        var fieldBuilder = typeBuilder.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);
        var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

        var getterMethod = typeBuilder.DefineMethod($"get_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
        var getterIL = getterMethod.GetILGenerator();
        getterIL.Emit(OpCodes.Ldarg_0);
        getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
        getterIL.Emit(OpCodes.Ret);

        var setterMethod = typeBuilder.DefineMethod($"set_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[] { propertyType });
        var setterIL = setterMethod.GetILGenerator();
        setterIL.Emit(OpCodes.Ldarg_0);
        setterIL.Emit(OpCodes.Ldarg_1);
        setterIL.Emit(OpCodes.Stfld, fieldBuilder);
        setterIL.Emit(OpCodes.Ret);

        propertyBuilder.SetGetMethod(getterMethod);
        propertyBuilder.SetSetMethod(setterMethod);
    }
}