namespace Vme2900.Core;

/// <summary>Small shell-like tokenizer supporting quoted arguments.</summary>
public static class CommandParser
{
    public static ParsedCommand Parse(string line)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        char quote = '\0';
        bool escaping = false;

        foreach (char ch in line)
        {
            if (escaping) { current.Append(ch); escaping = false; continue; }
            if (ch == '\\' && quote != '\0') { escaping = true; continue; }
            if (quote != '\0')
            {
                if (ch == quote) quote = '\0';
                else current.Append(ch);
                continue;
            }
            if (ch is '"' or '\'') { quote = ch; continue; }
            if (char.IsWhiteSpace(ch))
            {
                if (current.Length > 0) { tokens.Add(current.ToString()); current.Clear(); }
                continue;
            }
            current.Append(ch);
        }

        if (quote != '\0') throw new FormatException("Unterminated quoted argument.");
        if (current.Length > 0) tokens.Add(current.ToString());
        if (tokens.Count == 0) return new ParsedCommand("", Array.Empty<string>());
        return new ParsedCommand(tokens[0].ToUpperInvariant(), tokens.Skip(1).ToArray());
    }
}
