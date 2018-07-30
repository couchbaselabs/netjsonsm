using netjsonsm.Tokenizer;

namespace netjsonsm
{
    public interface ITokenizer
    {
        int Position { get; }

        TokenizerResult Step();
        void Reset(byte[] data);
        void Seek(int position);
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.
