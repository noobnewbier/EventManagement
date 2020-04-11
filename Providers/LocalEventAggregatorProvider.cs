using System;
using UnityEngine;

namespace EventManagement.Providers
{
    public class LocalEventAggregatorProvider : MonoBehaviour
    {
        private Lazy<EventAggregator> LazyInstance = new Lazy<EventAggregator>(() => new EventAggregator());
        public IEventAggregator ProvideEventAggregator() => LazyInstance.Value;
    }
}