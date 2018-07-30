using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

namespace netjsonsm.Tests
{
    public class PeopleFixture
    {
        public PeopleFixture()
        {
            const string resourceName = "netjsonsm.Tests.people.json";

            string json;
            using (var stream = typeof(SlowMatcherTests).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                json = reader.ReadToEnd();
            }

            RawData = Encoding.UTF8.GetBytes(json);
            Data = JArray.Parse(json)
                .ToDictionary(
                    key => key.SelectToken("_id").Value<string>(),
                    value => value.ToString()
                );
        }

        public byte[] RawData { get; }
        public Dictionary<string, string> Data { get; }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.
