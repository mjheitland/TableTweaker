using System;
using System.Collections.Generic;

namespace TableTweaker.Model
{
    public enum TokenCategory
    {
        // any text that shall be copied into the output
        Text, // anything else

        // escape character
        Dollar, // $dollar;

        // input field data (table column)
        HeaderIndex, // $h0, $h1, ...
        InvertedHeaderIndex, // $h-0, $h-1, ...
        FieldIndex, // $0, $1, ...
        InvertedFieldIndex, // $-0, $-1, ...

        // input data (table)
        Header, // $header (first line)
        Row, // $row (current line)
        RowNum, // $rowNum
        RowNumOne, // $rowNumOne
        NumFields, // $numFields
        NumRows, // $numRows

        // C# code
        MethodCall, // $<method name>($0,$1,$2...)

        // state; signals end of previous section
        Once, // $ONCE
        Each, // $EACH
        EachPlus, // $EACH+

        // pseudo token 
        EndOfInput 
    }

    public static class TokenCategoryList
    {
        public static readonly List<Tuple<string, TokenCategory>> Mappings = new List<Tuple<string, TokenCategory>>
        {
            // order entries from longest match to shortest match!
            new Tuple<string, TokenCategory>("dollar;", TokenCategory.Dollar),
            new Tuple<string, TokenCategory>("header", TokenCategory.Header),
            new Tuple<string, TokenCategory>("rowNumOne", TokenCategory.RowNumOne),
            new Tuple<string, TokenCategory>("rowNum", TokenCategory.RowNum),
            new Tuple<string, TokenCategory>("row", TokenCategory.Row),
            new Tuple<string, TokenCategory>("numFields", TokenCategory.NumFields),
            new Tuple<string, TokenCategory>("numRows", TokenCategory.NumRows),
            new Tuple<string, TokenCategory>("ONCE\r\n", TokenCategory.Once),
            new Tuple<string, TokenCategory>("ONCE\n", TokenCategory.Once),
            new Tuple<string, TokenCategory>("EACH+\r\n", TokenCategory.EachPlus),	
            new Tuple<string, TokenCategory>("EACH+\n", TokenCategory.EachPlus),
            new Tuple<string, TokenCategory>("EACH\r\n", TokenCategory.Each),
            new Tuple<string, TokenCategory>("EACH\n", TokenCategory.Each)
        };
    }
}
