using System.Collections.Generic;
using UnityEngine;

namespace Entitas.Unity
{
    public class EntityBehaviour : MonoBehaviour
    {
        public IContext Context => _context;
        public IEntity Entity => _entity;

        IContext _context;
        IEntity _entity;
        Stack<EntityBehaviour> _entityBehaviourPool;
        string _cachedName;

        public void Initialize(IContext context, IEntity entity, Stack<EntityBehaviour> entityBehaviourPool)
        {
            _context = context;
            _entity = entity;
            _entityBehaviourPool = entityBehaviourPool;

            _entity.OnEntityReleased += OnEntityReleased;
            gameObject.hideFlags = HideFlags.None;
            gameObject.SetActive(true);
            Update();
        }

        void OnEntityReleased(IEntity e)
        {
            _entity.OnEntityReleased -= OnEntityReleased;
            gameObject.hideFlags = HideFlags.HideInHierarchy;
            gameObject.SetActive(false);
            _entityBehaviourPool.Push(this);
            _cachedName = null;
            name = string.Empty;
        }

        void Update()
        {
            if (_entity != null && _cachedName != _entity.ToString())
                name = _cachedName = _entity.ToString();
        }

        void OnDestroy()
        {
            if (_entity != null)
                _entity.OnEntityReleased -= OnEntityReleased;
        }
    }
}
