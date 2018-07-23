using netjsonsm.Expressions;

namespace netjsonsm.Parsing
{
    public interface IParser
    {
        IExpression ParseJsonExpression(byte[] data);
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.
