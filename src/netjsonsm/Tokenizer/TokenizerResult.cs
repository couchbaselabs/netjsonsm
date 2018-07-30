namespace netjsonsm.Tokenizer
{
    public class TokenizerResult
    {
        public TokenizerResult(TokenType type, byte[] data = null)
        {
            Type = type;
            Data = data ?? new byte[0];
        }

        public TokenType Type { get; }
        public byte[] Data { get; }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.
