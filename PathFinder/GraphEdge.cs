using ClickHouseProvider.Utils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace ClickHouseProvider.Objects
{
    public class GraphEdge //: IEqualityComparer<GraphEdge>
    {
        [Column(Name = "id", Type = DbType.UInt64)]
        public ulong Id { get; set; }

        [Column(Name = "from", Type = DbType.UInt64)]
        public ulong From { get; set; }

        [Column(Name = "to", Type = DbType.UInt64)]
        public ulong To { get; set; }

        [Column(Name = "type", Type = DbType.UInt64)]
        public ulong Type { get; set; }

        [Column(Name = "sub_type", Type = DbType.UInt64)]
        public ulong Subtype { get; set; }

        [Column(Name = "direction", Type = DbType.UInt64)]
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
        public bool Equals(GraphEdge x, GraphEdge y)
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
    public class PathDomainLink : IEqualityComparer<PathDomainLink>
    {
        public GraphEdge Edge { get; set; }
        public HashSet<PathDomainLink> PrevSteps { get; set; }

        //При поиске сл ноды сравниваем только по Edge, в уже подсеченых путях
        public bool Equals(PathDomainLink x, PathDomainLink y)
        {
            if (x == null || y == null) return false;
            return x.Edge == y.Edge;
        }
        public int GetHashCode([DisallowNull] PathDomainLink obj)
        {
            return obj.Edge.GetHashCode();
        }
    }
}