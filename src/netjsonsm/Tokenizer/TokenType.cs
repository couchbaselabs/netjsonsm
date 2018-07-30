namespace netjsonsm.Tokenizer
{
    public enum TokenType
    {
        Unknown,
        End,
        String,
        Number,
        Integer,
        ObjectStart,
        ObjectEnd,
        ObjectKeyDelim,
        ArrayEnd,
        ArrayStart,
        ListDelim,
        EscapeString,
        True,
        False,
        Null
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.
