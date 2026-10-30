using System;
using UnityEngine;
using VintageCollector.Persistence;

namespace VintageCollector.Runtime
{
    // SessionStore owns DTO cloning, validation and durable publication.
    public sealed class UnityStateSerializer : IStateSerializer
    {
        public string Serialize(SessionData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            return JsonUtility.ToJson(data);
        }

        public SessionData Deserialize(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            return JsonUtility.FromJson<SessionData>(text);
        }
    }
}
