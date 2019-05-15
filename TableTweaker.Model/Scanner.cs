using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TableTweaker.Model
{
	public class Scanner
	{
		private readonly string _input;

		private int _pos;

		public Scanner(string input)
		{
			_input = input ?? "";
		}

        /*
            public enum TokenCategory
            {
                Text, // anything else
                Dollar, // $dollar;
                HeaderIndex, // $h0, $h1, ...
                InvertedHeaderIndex, // $h-0, $h-1, ...
                FieldIndex, // $0, $1, ...
                InvertedFieldIndex, // $-0, $-1, ...
                Header, // $header (first line)
                Row, // $row (current line)
                RowNum, // $rowNum
                RowNumOne, // $rowNumOne
                NumFields, // $numFields
                NumRows, // $numRows
                Once, // $ONCE
                Each, // $EACH
                EachPlus, // $EACH+
                EndOfInput // pseudo token 
            }
        */
        public Token GetNextToken()
		{
			if (_pos == _input.Length)
				return new Token (TokenCategory.EndOfInput);

            var rest = _input.Substring(_pos);

            var regex = new Regex(@"^[^\$]+");
            var match = regex.Match(rest);
            if (match.Success)
            {
                _pos += match.Value.Length;
                var value = match.Value; 
                return new Token(TokenCategory.Text, value);
            }

            // skip "$"
		    _pos++;
		    rest = rest.Substring(1); 

			foreach (var tokenCategoryMapping in TokenCategoryList.Mappings.Where(tokenCategoryMapping => rest.StartsWith(tokenCategoryMapping.Item1)))
			{
				_pos += tokenCategoryMapping.Item1.Length;
				return new Token (tokenCategoryMapping.Item2);
			}

			regex = new Regex(@"^h\d+");
			match = regex.Match(rest);
			if (match.Success)
			{
				_pos += match.Value.Length;
				var value = match.Value.Substring(1); // will be replaced later by header[value]
				return new Token(TokenCategory.HeaderIndex, value);
			}

			regex = new Regex(@"^h-\d+");
			match = regex.Match(rest);
			if (match.Success)
			{
				_pos += match.Value.Length;
				var value = match.Value.Substring(2); // will be replaced later by header[fieldLength - 1 - value]
				return new Token(TokenCategory.InvertedHeaderIndex, value);
			}

			regex = new Regex(@"^\d+");
			match = regex.Match(rest);
			if (match.Success)
			{
				_pos += match.Value.Length;
				var value = match.Value; // will be replaced by row[value]
				return new Token(TokenCategory.FieldIndex, value);
			}

			regex = new Regex(@"^-\d+");
			match = regex.Match(rest);
			if (match.Success)
			{
				_pos += match.Value.Length;
				var value = match.Value.Substring(1); // will be replaced later by row[fieldLength - 1 - value]
				return new Token(TokenCategory.InvertedFieldIndex, value);
			}

			// if you use brackets within your method arguments, use a different bracket type to surround your arguments: ()/{}/[]/<>
			regex = new Regex(@"^\w+((\([^)]*\))|(\[[^]]*\])|(\{[^}]*\})|(\<[^>]*\>))");
			match = regex.Match(rest);
            if (match.Success)
            {
                _pos += match.Value.Length;
                var value = match.Value; 
                return new Token(TokenCategory.MethodCall, value);
            }

			throw new Exception($"Scanner error: invalid token '{rest}'");
		}

		public List<Token> GetAllTokens()
		{
			Token token;
			var tokens = new List<Token>();
			do
			{
				token = GetNextToken();
				tokens.Add(token);
			} while (token.Category != TokenCategory.EndOfInput);

			return tokens;
		}
	}
}
