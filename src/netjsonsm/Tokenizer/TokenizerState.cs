namespace netjsonsm
{
    public enum TokenizerState
    {
        BeginValue,
        BeginValueOrEmpty,
        BeginString,
        BeginStringOrEmpty,
        InString,
        InStringEsc,
        InStringEscU,
        InStringEscU1,
        InStringEscU12,
        InStringEscU123,
        Negative,
        One,
        Zero,
        Dot,
        DotZero,
        E,
        ESign,
        EZero,
        T,
        Tr,
        Tru,
        F,
        Fa,
        Fal,
        Fals,
        N,
        Nu,
        Nul
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.
