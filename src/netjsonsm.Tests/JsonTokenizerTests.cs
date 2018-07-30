using System;
using System.Text;
using netjsonsm.Tokenizer;
using Xunit;

namespace netjsonsm.Tests
{
    public class JsonTokenizerTests : IClassFixture<PeopleFixture>
    {
        private readonly PeopleFixture _fixture;

        public JsonTokenizerTests(PeopleFixture fixture)
        {
            _fixture = fixture;
        }

        private static void TestStep(ITokenizer tokeniser, TokenType expectedTokenType, string expectedValue)
        {
            var result = tokeniser.Step();
            Assert.Equal(expectedTokenType, result.Type);
            Assert.Equal(expectedValue, Encoding.UTF8.GetString(result.Data));
        }

        [Fact]
        public void TestTokenizerSeeking()
        {
            const string json = "{ \"a\": \"5b47eb0936ff92a567a0307e\", \"b\": false }";
            var bytes = Encoding.UTF8.GetBytes(json);

            var tokeniser = new JsonTokenizer(bytes);
            TestStep(tokeniser, TokenType.ObjectStart, "{");
            TestStep(tokeniser, TokenType.String, "\"a\"");

            var pos = tokeniser.Position;
            TestStep(tokeniser, TokenType.ObjectKeyDelim, ":");

            // reset and read same token
            tokeniser.Seek(pos);
            TestStep(tokeniser, TokenType.ObjectKeyDelim, ":");
            TestStep(tokeniser, TokenType.String, "\"5b47eb0936ff92a567a0307e\"");
        }

        [Fact]
        public void TestTokenizeObject()
        {
            const string json = "{ \"a\": \"5b47eb0936ff92a567a0307e\", \"b\": false }";
            var bytes = Encoding.UTF8.GetBytes(json);

            var tokeniser = new JsonTokenizer(bytes);

            TestStep(tokeniser, TokenType.ObjectStart, "{");
            TestStep(tokeniser, TokenType.String, "\"a\"");
            TestStep(tokeniser, TokenType.ObjectKeyDelim, ":");
            TestStep(tokeniser, TokenType.String, "\"5b47eb0936ff92a567a0307e\"");
            TestStep(tokeniser, TokenType.ListDelim, ",");
            TestStep(tokeniser, TokenType.String, "\"b\"");
            TestStep(tokeniser, TokenType.ObjectKeyDelim, ":");
            TestStep(tokeniser, TokenType.False, "false");
            TestStep(tokeniser, TokenType.ObjectEnd, "}");
            TestStep(tokeniser, TokenType.End, "");
        }

        [Fact]
        public void TestTokenizeArray()
        {
            const string json = "[1, 2999.22, null, \"hello\u2932world\" ]";
            var bytes = Encoding.UTF8.GetBytes(json);

            var tokeniser = new JsonTokenizer(bytes);

            TestStep(tokeniser, TokenType.ArrayStart, "[");
            TestStep(tokeniser, TokenType.Integer, "1");
            TestStep(tokeniser, TokenType.ListDelim, ",");
            TestStep(tokeniser, TokenType.Number, "2999.22");
            TestStep(tokeniser, TokenType.ListDelim, ",");
            TestStep(tokeniser, TokenType.Null, "null");
            TestStep(tokeniser, TokenType.ListDelim, ",");
            TestStep(tokeniser, TokenType.EscapeString, "\"hello\u2932world\"");
            TestStep(tokeniser, TokenType.ArrayEnd, "]");
            TestStep(tokeniser, TokenType.End, "");
        }

        private static void TestTokenizedValue(byte[] data, TokenType tokenType, string tokenData)
        {
            var tokeniser = new JsonTokenizer(data);
            TestStep(tokeniser, tokenType, tokenData);
            TestStep(tokeniser, TokenType.End, "");
        }

        private static void TestTokenizedValueEx(string data, TokenType token)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var whitespacebytes = Encoding.UTF8.GetBytes("  \t  \n ");

            // Check normally
            TestTokenizedValue(dataBytes, token, data);

            // Check with whitespace ahead of it
            var bytes = new byte[dataBytes.Length + whitespacebytes.Length];
            Buffer.BlockCopy(whitespacebytes, 0, bytes, 0, whitespacebytes.Length);
            Buffer.BlockCopy(dataBytes, 0, bytes, whitespacebytes.Length, dataBytes.Length);
            TestTokenizedValue(bytes, token, data);

            // Check with whitespace after of it
            Buffer.BlockCopy(dataBytes, 0, bytes, 0, dataBytes.Length);
            Buffer.BlockCopy(whitespacebytes, 0, bytes, dataBytes.Length, whitespacebytes.Length);
            TestTokenizedValue(bytes, token, data);
        }

        [Fact]
        public void TestTokenizeString()
        {
            TestTokenizedValueEx("\"lol\"", TokenType.String);
        }

        [Fact]
        public void TestTokenizeEscString()
        {
            TestTokenizedValueEx("\"l\nol\"", TokenType.EscapeString);
            TestTokenizedValueEx("\"l\u2321ol\"", TokenType.EscapeString);
        }

        [Fact]
        public void TestTokenizeInteger()
        {
            TestTokenizedValueEx("0", TokenType.Integer);
            TestTokenizedValueEx("123", TokenType.Integer);
            TestTokenizedValueEx("4565464651846548", TokenType.Integer);
        }

        [Fact]
        public void TestTokenizeNumber()
        {
            TestTokenizedValueEx("0.1", TokenType.Number);
            TestTokenizedValueEx("1999.1", TokenType.Number);
            TestTokenizedValueEx("14.29438383", TokenType.Number);
            TestTokenizedValueEx("1.0E+2", TokenType.Number);
            TestTokenizedValueEx("1.9e+22", TokenType.Number);
        }

        [Fact]
        public void TestTokenizeBool()
        {
            TestTokenizedValueEx("true", TokenType.True);
            TestTokenizedValueEx("false", TokenType.False);
        }

        [Fact]
        public void TestTokenizeNull()
        {
            TestTokenizedValueEx("null", TokenType.Null);
        }

        [Fact]
        public void TestTokenizeEndsForever()
        {
            const string json = "\"hello world\"";
            var bytes = Encoding.UTF8.GetBytes(json);

            var tokeniser = new JsonTokenizer(bytes);
            TestStep(tokeniser, TokenType.String, "\"hello world\"");
            TestStep(tokeniser, TokenType.End, string.Empty);
            TestStep(tokeniser, TokenType.End, string.Empty);
        }

        [Fact]
        public void TestTokenizerLong()
        {
            var tokeniser = new JsonTokenizer(_fixture.RawData);

            while (true)
            {
                var result = tokeniser.Step();
                if (result.Type == TokenType.End)
                {
                    break;
                }
            }
        }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.
