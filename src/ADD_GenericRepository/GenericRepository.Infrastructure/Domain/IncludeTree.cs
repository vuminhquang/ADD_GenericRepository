using System.Linq.Expressions;

namespace GenericRepository.Domain;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

public class IncludeTree<TEntity>
{
    public class IncludeNode
    {
        public LambdaExpression NavigationPropertyPath { get; set; }
        public List<IncludeNode> Children { get; set; } = new List<IncludeNode>();
    }

    private List<IncludeNode> includes = new List<IncludeNode>();

    public IEnumerable<IncludeNode> GetIncludes()
    {
        return includes;
    }

    public IncludeTree<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath)
    {
        var includeNode = new IncludeNode { NavigationPropertyPath = navigationPropertyPath };
        includes.Add(includeNode);
        return this;
    }

    public IncludeTree<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
    {
        if (!includes.Any())
        {
            throw new InvalidOperationException("ThenInclude must follow an Include.");
        }

        var lastInclude = GetLastIncludeNode(includes.Last());
        lastInclude.Children.Add(new IncludeNode { NavigationPropertyPath = navigationPropertyPath });
        return this;
    }

    private IncludeNode GetLastIncludeNode(IncludeNode node)
    {
        return node.Children.Count > 0 ? GetLastIncludeNode(node.Children.Last()) : node;
    }
}