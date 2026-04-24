using ClickHouseProvider.Objects;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ActiveDirectoryAnalyzer.DomainAnalysis
{
    class LightedEdgesEqualityComparer : IEqualityComparer<PathDomainLink>
    {
        public bool Equals(PathDomainLink x, PathDomainLink y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;

            return x.Edge == y.Edge;
        }

        public int GetHashCode([DisallowNull] PathDomainLink obj)
        {
            return obj.Edge.GetHashCode();
        }
    }

    public class PathFinder
    {
        private static readonly object _obj = new();

        private ulong _startPoint;
        private ulong _endPoint;
        private readonly string _pathHashCode;

        private Dictionary<ulong, GraphEdge[]> _allEdgesFromCache = new();

        // Найденные пути в виде набора связей, в итоге собираются в подмножество графа уязвимостей
        private HashSet<GraphEdge> _allFoundPaths = new();
        // Подсвеченные связи, если вершину уже подсветили больше не подсвечиваем
        private HashSet<PathDomainLink> _lightedEdges = new(new LightedEdgesEqualityComparer());

        public PathFinder(ulong startPoint, ulong endPoint, Dictionary<ulong, GraphEdge[]> allEdgesFromCache)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            //_pathHashCode = $"{_startPoint}:{_endPoint}";
            //_allEdges = allEdges;
            _allEdgesFromCache = allEdgesFromCache;
        }

        public GraphPathEnumerable? FindPath()
        {
            var startEdge = new GraphEdge { To = _startPoint };
            var currStep = new HashSet<GraphEdge>() { startEdge };
            var nextStep = new HashSet<GraphEdge>();

            PathDomainLink curPathBack = new PathDomainLink();
            do
            {
                // продвижение по фронту подсвеченых ребёр
                foreach (var node in currStep)
                {
                    //получаем соседей ноды, если нет переходим на сл по фронту
                    if (!_allEdgesFromCache.TryGetValue(node.To, out var neighbors))
                        continue;

                    //var neighbors = _allEdges.Where(t => t.From == currStep[iNode].To).ToArray();
                    PathDomainLink pathback;
                    foreach (var neighbor in neighbors)
                    {
                        //Такое ребро в подсвеченных?
                        PathDomainLink edgeNotLighted;
                        _lightedEdges.TryGetValue(new PathDomainLink { Edge = neighbor }, out edgeNotLighted);
                        //Запомнинаем путь домой
                        if (edgeNotLighted == null)
                        {
                            var parent = new PathDomainLink { Edge = node, PrevSteps = new HashSet<PathDomainLink> { curPathBack } };
                            pathback = new PathDomainLink { Edge = neighbor, PrevSteps = new HashSet<PathDomainLink> { parent } };
                            _lightedEdges.Add(pathback);
                        }
                        else
                        {
                            pathback = new PathDomainLink { Edge = node, PrevSteps = new HashSet<PathDomainLink> { curPathBack } };
                            edgeNotLighted.PrevSteps.Add(pathback);
                        }
                        curPathBack = pathback;
                        // есть ли такой ключ есть в искомых или в уже найденых путях
                        if (/*искомый*/_endPoint == neighbor.To || _allFoundPaths.Contains(neighbor)/*найденые*/)
                        {

                            GoFoundWay(pathback);

                        }
                        else if (edgeNotLighted == null)
                            nextStep.Add(neighbor);




                        //lock (_obj)
                        //{
                        //if(currStep[iNode].From != 0)
                        //    GetParentsForPath(neighbor).Add(currStep[iNode]);
                        //}
                        //если ещё не подсвечивали то собираем колекцию для сл. шага


                    }
                }
                currStep = nextStep;
                nextStep = new();
            }
            while (currStep.Count != 0);// Пока не подсвечены все грани
            if (_allFoundPaths.Count > 0)
                return new GraphPathEnumerable { StartPoint = _startPoint, EndPoint = _endPoint, PathArray = _allFoundPaths.Select(e => e.Id).ToArray() };

            return null;
        }

        private void GoFoundWay(PathDomainLink neighbor)
        {
            try
            {
                var nextForward = new HashSet<PathDomainLink>();

                do// !>>  Добавляем пройденный путь в коллекцию найденых путей <<!
                {
                    if (!_allFoundPaths.Contains(neighbor.Edge) && neighbor.PrevSteps.Count != 0)
                        _allFoundPaths.Add(neighbor.Edge);

                    foreach (var curBackward in neighbor.PrevSteps)
                    {
                        if (!_allFoundPaths.Contains(curBackward.Edge) && curBackward.Edge.From != 0)
                            _allFoundPaths.Add(curBackward.Edge);
                        else continue; //если путь назад уже добавлен то берём сл-й

                        //идём обратно по следам пока не встретим пустой Parent, он только у стартовой точки
                        if (curBackward.PrevSteps == null)
                        {
                            //var edges = _lightedEdges.Where(h => !_allFoundPaths.Contains(h.Edge)
                            //    && curBackward.Parent == null
                            //    && curBackward.Parent.Contains(h.Edge)).ToList();
                            nextForward.UnionWith(curBackward.PrevSteps);
                        }
                    }
                    neighbor.PrevSteps = nextForward;// шаги назад, создём сл итерацию
                    nextForward = new();
                }
                while (neighbor.PrevSteps.Count > 0);// пока есть путь назад

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}