using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using netjsonsm.Expressions;
using netjsonsm.Matcher;
using netjsonsm.Parsing;
using Newtonsoft.Json.Linq;
using Xunit;

namespace netjsonsm.Tests
{
    public class SlowMatcherTests : IClassFixture<SlowMatcherTests.PeopleFixture>
    {
        private readonly IDictionary<string, string> _peopleData;

        public SlowMatcherTests(PeopleFixture fixture)
        {
            _peopleData = fixture.Data;
        }

        #region programmatic tests

        private const string EmptyJson = "{ }";

        [Fact]
        public void Simple_True_Expression()
        {
            var expression = new TrueExpresion();
            var bytes = Encoding.UTF8.GetBytes(EmptyJson);
            var matcher = new SlowMatcher(new[] {expression});

            Assert.True(matcher.Match(bytes));
        }

        [Fact]
        public void Simple_False_Expression()
        {
            var expression = new FalseExpression();
            var bytes = Encoding.UTF8.GetBytes(EmptyJson);
            var matcher = new SlowMatcher(new[] {expression});

            Assert.False(matcher.Match(bytes));
        }

        [Theory]
        [InlineData(5, false)]
        [InlineData(10, true)]
        [InlineData(15, true)]
        public void GreaterEqual(int value, bool expected)
        {
            var expression = new GreaterEqualExpression(
                new FieldExpression(0, "age"),
                new ValueExpression(10)
            );

            var json = $"{{ \"age\": {value} }}";
            var bytes = Encoding.UTF8.GetBytes(json);

            var matcher = new SlowMatcher(new[] {expression});
            Assert.Equal(expected, matcher.Match(bytes));
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(10, false)]
        [InlineData(15, false)]
        public void LessThan(int value, bool expected)
        {
            var expression = new LessThanExpression(
                new FieldExpression(0, "age"),
                new ValueExpression(10)
            );

            var json = $"{{ \"age\": {value} }}";
            var bytes = Encoding.UTF8.GetBytes(json);

            var matcher = new SlowMatcher(new[] {expression});
            Assert.Equal(expected, matcher.Match(bytes));
        }

        [Theory]
        [InlineData(10)]
        [InlineData(10.5)]
        [InlineData("mike")]
        [InlineData(true)]
        [InlineData(false)]
        public void Equals_(dynamic value)
        {
            var expression = new EqualsExpression(
                new FieldExpression(0, "value"),
                new ValueExpression(value)
            );

            if (value is string)
            {
                value = $"\"{value}\"";
            }

            var json = $"{{ \"value\": {value.ToString().ToLower()} }}";
            var bytes = Encoding.UTF8.GetBytes(json);

            var matcher = new SlowMatcher(new[] {expression});
            Assert.True(matcher.Match(bytes));
        }

        [Fact]
        public void And()
        {
            var expression = new AndExpression(
                new EqualsExpression(
                    new FieldExpression(0, "name"),
                    new ValueExpression("mike")
                ),
                new GreaterEqualExpression(
                    new FieldExpression(0, "age"),
                    new ValueExpression(10)
                )
            );

            const string json = "{ \"name\": \"mike\", \"age\": 10 }";
            var bytes = Encoding.UTF8.GetBytes(json);

            var matcher = new SlowMatcher(new[] { expression });
            Assert.True(matcher.Match(bytes));
        }

        [Fact]
        public void Or()
        {
            var expression = new OrExpression(
                new EqualsExpression(
                    new FieldExpression(0, "name"),
                    new ValueExpression("jeff")
                ),
                new GreaterEqualExpression(
                    new FieldExpression(0, "age"),
                    new ValueExpression(10) // should fail
                )
            );

            const string json = "{ \"name\": \"mike\", \"age\": 50 }";
            var bytes = Encoding.UTF8.GetBytes(json);

            var matcher = new SlowMatcher(new[] { expression });
            Assert.True(matcher.Match(bytes));
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(15, false)]
        public void Not_GreaterEqual(int value, bool expected)
        {
            // not >= 0
            var expression = new NotExpression(
                new GreaterEqualExpression(
                    new FieldExpression(0, "age"),
                    new ValueExpression(10)
                )
            );

            var json = $"{{ \"age\": {value} }}";
            var bytes = Encoding.UTF8.GetBytes(json);

            var matcher = new SlowMatcher(new[] {expression});
            Assert.Equal(expected, matcher.Match(bytes));
        }

        [Theory]
        [InlineData(5, false)]
        [InlineData(15, true)]
        public void Not_LessThan(int value, bool expected)
        {
            // not < 0
            var expression = new NotExpression(
                new LessThanExpression(
                    new FieldExpression(0, "age"),
                    new ValueExpression(10)
                )
            );

            var json = $"{{ \"age\": {value} }}";
            var bytes = Encoding.UTF8.GetBytes(json);

            var matcher = new SlowMatcher(new[] { expression });
            Assert.Equal(expected, matcher.Match(bytes));
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(10, false)]
        [InlineData(15, true)]
        public void Not_Equal(int value, bool expected)
        {
            // not == 0
            var expression = new NotExpression(
                new EqualsExpression(
                    new FieldExpression(0, "age"),
                    new ValueExpression(10)
                )
            );

            var json = $"{{ \"age\": {value} }}";
            var bytes = Encoding.UTF8.GetBytes(json);

            var matcher = new SlowMatcher(new[] { expression });
            Assert.Equal(expected, matcher.Match(bytes));
        }

        #endregion

        #region gojsonsm compatability tests - https://github.com/couchbaselabs/gojsonsm/blob/master/matcher_test.go

        [Fact]
        public void TestMatchStringEquals()
        {
            const string expressionJson = @"
[""equals"",
    [""field"", ""name""],
    [""value"",""Daphne Sutton""]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] {expression});

            var ids = new[] {"5b47eb0936ff92a567a0307e"};
            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact]
        public void TestMatcherNumericEquals()
        {
            const string expressionJson = @"
[""equals"",
    [""field"",""age""],
    [""value"",25]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] { expression });

            var ids = new[] { "5b47eb091f57571d3c3b1aa1" };
            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact]
        public void TestMatcherFloatEquals()
        {
            const string expressionJson = @"
[""equals"",
    [""field"",""latitude""],
    [""value"",-40.262556]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] { expression });

            var ids = new[] { "5b47eb096b1d911c0b9492fb" };
            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact]
        public void TestMatcherTrueEquals()
        {
            const string expressionJson = @"
[""equals"",
    [""field"",""isActive""],
    [""value"",true]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] { expression });

            var ids = new[] { "5b47eb0936ff92a567a0307e",
                "5b47eb0950e9076fc0aecd52",
                "5b47eb095c3ad73b9925f7f8",
                "5b47eb0962222a37d066e231",
                "5b47eb09996a4154c35b2f98",
                "5b47eb098eee4b4c4330ec64" };
            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact]
        public void TestMatcherFalseEquals()
        {
            const string expressionJson = @"
[""equals"",
    [""field"",""isActive""],
    [""value"",false]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] { expression });

            var ids = new[] { "5b47eb096b1d911c0b9492fb",
                "5b47eb093771f06ced629663",
                "5b47eb09ffac5a6ce37042e7",
                "5b47eb091f57571d3c3b1aa1" };
            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact]
        public void TestMatcherNotTrueEquals()
        {
            const string expressionJson =
@"
[""not"",
    [""equals"",
        [""field"", ""isActive""],
        [""value"", true]
    ]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] { expression });

            var ids = new[]
            {
                "5b47eb096b1d911c0b9492fb",
                "5b47eb093771f06ced629663",
                "5b47eb09ffac5a6ce37042e7",
                "5b47eb091f57571d3c3b1aa1"
            };

            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact(Skip = "TODO(brett19): Should probably discuss whether type-cast equals " +
                     "actually makes sense... This validates that these something like: " +
                     "(true == \"thisShouldBeABoolean\") === true" +
                     "which may not actually make a whole lot of sense...")]
        public void TestMatcherDisparateTypeEquals()
        {
            const string expressionJson = @"
[""equals"",
    [""field"", ""sometimesValue""],
    [""value"", ""thisShouldBeABoolean""]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] {expression});

            var ids = new[] {"5b47eb0936ff92a567a0307e", "5b47eb096b1d911c0b9492fb"};
            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact]
        public void TestMatcherSometimesMissingBoolEquals()
        {
            const string expressionJson = @"
[""equals"",
    [""field"", ""sometimesValue""],
    [""value"", false]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] {expression});

            var ids = new[]
            {
                "5b47eb0936ff92a567a0307e",
                "5b47eb096b1d911c0b9492fb"
            };
            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact]
        public void TestMatcherMissingStringEquals()
        {
            const string expressionJson = @"
[""equals"",
    [""field"", ""someValueWhichNeverExists""],
    [""value"", ""hello""]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] {expression});

            var ids = new string[] { };

            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact]
        public void TestMatcherAnyInEquals()
        {
            const string expressionJson = @"
[""anyin"",
    1,
    [""field"", ""tags""],
    [""equals"",
        [""field"", 1],
        [""value"", ""cillum""]
    ]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] { expression });

            var ids = new[]
            {
                "5b47eb0936ff92a567a0307e",
                "5b47eb09ffac5a6ce37042e7",
                "5b47eb095c3ad73b9925f7f8"
            };
            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact]
        public void TestMatcherNestedAnyInAnyInEquals()
        {
            const string expressionJson = @"
[""anyin"",
    1,
    [""field"", ""nestedArray""],
    [""anyin"",
        2,
        [""field"", 1],
        [""equals"",
            [""field"", 2],
            [""value"", ""g""]
        ]
    ]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] { expression });

            var ids = new[]
            {
                "5b47eb0936ff92a567a0307e"
            };
            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact]
        public void TestMatcherNestedAnyInAnyInEqualsNoMatch()
        {
            const string expressionJson = @"
[""anyin"",
    1,
    [""field"", ""nestedArray""],
    [""anyin"",
        2,
        [""field"", 1],
        [""equals"",
            [""field"", 2],
            [""value"", ""z""]
        ]
    ]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] { expression });

            var ids = new string[] { };
            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact(Skip = "Slow matcher does not support EveryIn expressions yet")]
        public void TestMatcherEveryInEquals()
        {
            const string expressionJson =
@"
[""everyin"",
    1,
    [""field"", ""testArray""],
    [""equals"",
        [""field"", 1],
        [""value"", ""jewels""]
    ]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] { expression });

            var ids = new []
            {
                "5b47eb0936ff92a567a0307e",
                "5b47eb09ffac5a6ce37042e7"
            };

            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        [Fact(Skip = "Slow matcher does not support EveryIn expressions yet")]
        public void TestMatcherAnyEveryInEquals()
        {
            const string expressionJson = @"
[""anyeveryin"",
    1,
    [""field"", ""testArray""],
    [""equals"",
        [""field"", 1],
        [""value"", ""jewels""]
    ]
]";
            var expressionBytes = Encoding.UTF8.GetBytes(expressionJson);

            var parser = new SimpleParser();
            var expression = parser.ParseJsonExpression(expressionBytes);

            var matcher = new SlowMatcher(new[] { expression });

            var ids = new[]
            {
                "5b47eb0936ff92a567a0307e"
            };
            foreach (var entry in _peopleData.Where(x => ids.Contains(x.Key)))
            {
                var data = Encoding.UTF8.GetBytes(entry.Value);
                Assert.True(matcher.Match(data));
            }
        }

        #endregion

        #region Test fixture

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

                Data = JArray.Parse(json)
                    .ToDictionary(
                        key => key.SelectToken("_id").Value<string>(),
                        value => value.ToString()
                    );
            }

            public Dictionary<string, string> Data { get; }
        }

        #endregion
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.
