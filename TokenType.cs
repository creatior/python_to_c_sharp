using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PythonToCSharp
{
    public enum TokenType
    {
        Identifier,
        Integer,
        FloatFixed,
        FloatExp,
        String,
        IndexedVar,
        Comment,
        FunctionCall,
        Operator,
        Assignment,
        Punctuation,
        NewLine,
        Whitespace,
        Mismatch
    }
}
