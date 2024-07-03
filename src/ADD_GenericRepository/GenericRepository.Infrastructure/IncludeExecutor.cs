using GenericRepository.Domain;


using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace GenericRepository.Infrastructure;

public static class IncludeExecutor
    {
        public static IQueryable<TEntity> ExecuteIncludes<TEntity>(IQueryable<TEntity> query, IncludeTree<TEntity> includeTree) where TEntity : class
        {
            return includeTree.GetIncludes().Aggregate(query, ApplyInclude);
        }

        private static IQueryable<TEntity> ApplyInclude<TEntity>(IQueryable<TEntity> query, IncludeTree<TEntity>.IncludeNode includeNode) where TEntity : class
        {
            query = Include(query, includeNode.NavigationPropertyPath);

            // Apply ThenInclude on foreach child
            return includeNode.Children.Aggregate(query, ApplyThenInclude);
        }

        private static IQueryable<TEntity> ApplyThenInclude<TEntity>(IQueryable<TEntity> query, IncludeTree<TEntity>.IncludeNode includeNode) where TEntity : class
        {
            query = ThenInclude(query, includeNode.NavigationPropertyPath);

            // Apply ThenInclude on foreach child
            return includeNode.Children.Aggregate(query, ApplyThenInclude);
        }

        private static IQueryable<TEntity> Include<TEntity>(IQueryable<TEntity> query, LambdaExpression navigationPropertyPath) where TEntity : class
        {
            var method = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == "Include" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TEntity), navigationPropertyPath.Body.Type);

            return (IQueryable<TEntity>)method.Invoke(null, new object[] { query, navigationPropertyPath });
        }

        private static IQueryable<TEntity> ThenInclude<TEntity>(IQueryable<TEntity> query, LambdaExpression navigationPropertyPath) where TEntity : class
        {
            var method = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.Name == "ThenInclude" && m.GetParameters().Length == 2)
                .First(m => m.GetParameters()[0].ParameterType.GenericTypeArguments.Length == 2)
                .MakeGenericMethod(query.ElementType, navigationPropertyPath.Parameters[0].Type, navigationPropertyPath.Body.Type);

            return (IQueryable<TEntity>)method.Invoke(null, new object[] { query, navigationPropertyPath });
        }
    }