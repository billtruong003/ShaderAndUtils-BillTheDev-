using UnityEngine;
using System.Collections.Generic;

public class SceneOctree
{
    private readonly OctreeNode _rootNode;

    public SceneOctree(IEnumerable<Renderer> initialRenderers, int maxObjectsPerNode, int maxDepth)
    {
        var bounds = EncapsulateRenderers(initialRenderers);
        _rootNode = new OctreeNode(bounds, 0, maxObjectsPerNode, maxDepth);
        Build(initialRenderers);
    }

    public void Build(IEnumerable<Renderer> renderers)
    {
        _rootNode.Clear();
        foreach (var renderer in renderers)
        {
            if (renderer != null && renderer.enabled)
            {
                _rootNode.Insert(renderer);
            }
        }
    }

    public void GetRenderersInFrustum(Plane[] frustumPlanes, List<Renderer> results)
    {
        results.Clear();
        _rootNode.QueryFrustum(frustumPlanes, results);
    }

    private Bounds EncapsulateRenderers(IEnumerable<Renderer> renderers)
    {
        if (renderers == null) return new Bounds(Vector3.zero, Vector3.one * 100f);

        var totalBounds = new Bounds();
        bool first = true;
        foreach (var rend in renderers)
        {
            if (rend == null) continue;
            if (first)
            {
                totalBounds = rend.bounds;
                first = false;
            }
            else
            {
                totalBounds.Encapsulate(rend.bounds);
            }
        }

        if (totalBounds.size == Vector3.zero)
        {
            totalBounds = new Bounds(Vector3.zero, Vector3.one * 100f);
        }
        return totalBounds;
    }

    private class OctreeNode
    {
        private readonly Bounds _bounds;
        private readonly int _depth;
        private readonly int _maxObjectsPerNode;
        private readonly int _maxDepth;
        private readonly List<Renderer> _objects = new List<Renderer>();
        private OctreeNode[] _children;
        private bool _hasChildren;

        public OctreeNode(Bounds bounds, int depth, int maxObjects, int maxNodeDepth)
        {
            _bounds = bounds;
            _depth = depth;
            _maxObjectsPerNode = maxObjects;
            _maxDepth = maxNodeDepth;
        }

        public void Insert(Renderer rend)
        {
            if (!_bounds.Intersects(rend.bounds)) return;

            if (_hasChildren)
            {
                foreach (var child in _children) child.Insert(rend);
                return;
            }

            _objects.Add(rend);

            if (_objects.Count > _maxObjectsPerNode && _depth < _maxDepth)
            {
                Subdivide();
                DistributeObjectsToChildren();
                _objects.Clear();
            }
        }

        public void QueryFrustum(Plane[] frustumPlanes, List<Renderer> results)
        {
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, _bounds)) return;

            if (_hasChildren)
            {
                foreach (var child in _children) child.QueryFrustum(frustumPlanes, results);
            }
            else
            {
                foreach (var obj in _objects)
                {
                    if (GeometryUtility.TestPlanesAABB(frustumPlanes, obj.bounds))
                    {
                        results.Add(obj);
                    }
                }
            }
        }

        public void Clear()
        {
            _objects.Clear();
            if (!_hasChildren) return;

            foreach (var child in _children) child.Clear();
            _children = null;
            _hasChildren = false;
        }

        private void Subdivide()
        {
            _hasChildren = true;
            _children = new OctreeNode[8];
            Vector3 halfSize = _bounds.size * 0.5f;
            Vector3 center = _bounds.center;

            for (int i = 0; i < 8; i++)
            {
                Vector3 childCenter = center;
                childCenter.x += halfSize.x * 0.5f * ((i & 1) == 0 ? -1 : 1);
                childCenter.y += halfSize.y * 0.5f * ((i & 2) == 0 ? -1 : 1);
                childCenter.z += halfSize.z * 0.5f * ((i & 4) == 0 ? -1 : 1);
                _children[i] = new OctreeNode(new Bounds(childCenter, halfSize), _depth + 1, _maxObjectsPerNode, _maxDepth);
            }
        }

        private void DistributeObjectsToChildren()
        {
            foreach (var rend in _objects)
            {
                foreach (var child in _children)
                {
                    child.Insert(rend);
                }
            }
        }
    }
}