using System.Collections.Generic;
namespace TableTweaker.Model
{
    public class TokenList
    {
        public int Index { get; set; }

        public List<Token> Values;

        public Token Current => Values[Index];

        public TokenList(List<Token> tokens)
        {
            Values = tokens;
        }

        public override string ToString()
        {
            return $"Index: {Index}, Current: {Current}";
        }
    }
}
