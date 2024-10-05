using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Prg.Util
{
    /// <summary>
    /// Cache for <c>GameObject</c> prefabs that are inactive (disabled) while in cache and when returned from cache.
    /// </summary>
    public class PrefabCache : MonoBehaviour
    {
        private Queue<GameObject> _cache;

        private Func<GameObject> _instantiateItem;

        public void Initialize(GameObject template, int preWarmCacheSize = 0, Transform parent = null)
        {
            var isCacheable = !template.IsSceneObject() || !template.HasParent();
            Assert.IsTrue(isCacheable, "template is not prefab");
            Assert.IsTrue(preWarmCacheSize >= 0, "preWarmCacheSize must be >=0");
            if (parent == null)
            {
                parent = transform;
            }
            _instantiateItem = () =>
            {
                var instance = Instantiate(template, parent, false);
                instance.SetActive(false);
                instance.name = instance.name.Replace("Clone", $"{_cache.Count}");
                return instance;
            };
            _cache = new Queue<GameObject>(preWarmCacheSize);
            while (_cache.Count < preWarmCacheSize)
            {
                _cache.Enqueue(_instantiateItem());
            }
        }

        public GameObject Dequeue()
        {
            return _cache.Count == 0 ? _instantiateItem() : _cache.Dequeue();
        }

        public void Enqueue(GameObject cachedObject)
        {
            cachedObject.SetActive(false);
            _cache.Enqueue(cachedObject);
        }
    }
}
