using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using VintageCollector.Collections;
namespace VintageCollector.Runtime {
    public sealed class UnityPhysicalSerializer : IPhysicalSerializer {
        public string Serialize(PhysicalStateData data) {
            if(data==null) throw new ArgumentNullException("data");
            return JsonUtility.ToJson(data);
        }
        public PhysicalStateData Deserialize(string text) {
            // JsonUtility silently wraps overflowing ints and coerces decimal/exponent
            // numbers. Shape first, then require a lossless canonical numeric round trip.
            // Store validation still owns schema, catalog, UTC, conditions and event semantics.
            CollectionJsonShape.Physical(text);
            var data=JsonUtility.FromJson<PhysicalStateData>(text);
            string[] before=Numbers(text),after=Numbers(JsonUtility.ToJson(data));
            if(before.Length!=after.Length) throw new InvalidDataException("Physical JSON numeric shape changed");
            for(int i=0;i<before.Length;i++) if(before[i]!=after[i]) throw new InvalidDataException("Physical JSON numeric value is out of range");
            return data;
        }
        private static string[] Numbers(string text) {
            // Shape admitted only nonnegative canonical integer tokens. Ignore strings,
            // including escaped quotes; sorting permits arbitrary legal property order.
            var numbers=new List<string>();bool quoted=false,escaped=false;
            for(int i=0;i<text.Length;i++) {
                char c=text[i];
                if(quoted) {if(escaped) escaped=false;else if(c=='\\') escaped=true;else if(c=='"') quoted=false;continue;}
                if(c=='"') {quoted=true;continue;}
                if(c>='0'&&c<='9') {int start=i;while(i+1<text.Length&&text[i+1]>='0'&&text[i+1]<='9') i++;numbers.Add(text.Substring(start,i-start+1));}
            }
            numbers.Sort(StringComparer.Ordinal);return numbers.ToArray();
        }
    }
}
