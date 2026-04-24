using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace ClickHouseProvider.Objects
{
    public class GraphEdge //: IEqualityComparer<GraphEdge>
    {
        [Column("id", TypeName = "UInt64")]
        public ulong Id { get; set; }

        [Column("from", TypeName = "UInt64")]
        public ulong From { get; set; }

        [Column("to", TypeName = "UInt64")]
        public ulong To { get; set; }

        [Column("type", TypeName = "UInt64")]
        public ulong Type { get; set; }

        [Column("sub_type", TypeName = "UInt64")]
        public ulong Subtype { get; set; }

        [Column("direction", TypeName = "UInt64")]
        public ulong Direction { get; set; }

        //public override bool Equals(object obj)
        //{
        //    if (obj is GraphEdge edge) 
        //        return From == edge.From && To == edge.To && Type == edge.Type;

        //    return false;
        //}

        //public bool Equals(GraphEdge x, GraphEdge y)
        //{
        //    if (x == null || y == null) return false;
        //    return x.From == y.From 
        //        && x.To == y.To 
        //        && x.Type == y.Type;
        //}
        //public int GetHashCode([DisallowNull] GraphEdge obj)
        //{
        //    return (int)(obj.From ^ obj.To ^ obj.Type);
        //}
    }

    class GraphEdgeComparer : IEqualityComparer<GraphEdge>
    {
        public bool Equals(GraphEdge? x, GraphEdge? y)
        {
            if (x == null || y == null) return false;
            return x.From == y.From
                && x.To == y.To
                && x.Type == y.Type;
        }
        public int GetHashCode([DisallowNull] GraphEdge obj)
        {
            return (int)(obj.From ^ obj.To ^ obj.Type);
        }
    }

    public class GraphEdgeEnumerable : GraphEdge, IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            foreach (var g in _getter)
                yield return g(this);
        }
        private static readonly IEnumerable<Func<GraphEdgeEnumerable, object>> _getter = Helper.Getter<GraphEdgeEnumerable>();
    }

    public class GraphPathEnumerable : GraphPath, IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            foreach (var g in _getter)
                yield return g(this);
        }
        private static readonly IEnumerable<Func<GraphPathEnumerable, object>> _getter = Helper.Getter<GraphPathEnumerable>();

        public ulong StartPoint { get; internal set; }
        public ulong EndPoint { get; internal set; }
        public List<ulong> PathArray { get; internal set; } = new List<ulong>();
    }

    public class GraphPath
    {
    }

    public class PathDomainLink : IEqualityComparer<PathDomainLink>
    {
        public GraphEdge Edge { get; set; } = new ();
        public HashSet<PathDomainLink> PrevSteps { get; set; } = [];

        // При поиске сл ноды сравниваем только по Edge, в уже подсеченых путях
        public bool Equals(PathDomainLink? x, PathDomainLink? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.Edge == y.Edge;
        }
        public int GetHashCode([DisallowNull] PathDomainLink obj)
        {
            return obj.Edge.GetHashCode();
        }
    }

    public static class Helper
    {
        // Возвращает IEnumerable<Func<T, object>> — делегаты, читающие все публичные instance-свойства типа T
        public static IEnumerable<Func<T, object>> Getter<T>()
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                Expression access = Expression.Property(parameter, prop);
                Expression convert = Expression.Convert(access, typeof(object));
                yield return Expression.Lambda<Func<T, object>>(convert, parameter).Compile();
            }
        }
    }
}