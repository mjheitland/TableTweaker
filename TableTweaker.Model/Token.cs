namespace TableTweaker.Model
{
    public class Token
    {
        public TokenCategory Category { get; set; }

        public string Value { get; set; }

        public Token(TokenCategory category, string value = "")
        {
            Category = category;
            Value = value;
        }

        public override string ToString()
        {
            return $"Category: {Category}, Value: \"{Value}\"";
        }
    }
}
