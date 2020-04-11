using System;
using UnityEngine;

namespace EventManagement.Providers
{
    //if adapted scriptable paradigm, we will refactor this so every new scriptable instance hold a different event aggregator
    [CreateAssetMenu(menuName = "ScriptableService/EventAggregator")]
    public class EventAggregatorProvider : ScriptableObject
    {
        private readonly Lazy<IEventAggregator> _lazyInstance = new Lazy<IEventAggregator>(() => new EventAggregator());
        // ReSharper disable once MemberCanBeMadeStatic.Global
        public IEventAggregator ProvideEventAggregator() => _lazyInstance.Value;
    }
}