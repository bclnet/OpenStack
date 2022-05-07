namespace System.Globalization
{
    /// <summary>
    /// Grammar
    /// </summary>
    public static class Grammar
    {
        public static bool StartsWithVowel(this string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            var firstLetter = s.ToLower()[0];
            return "aeiou".IndexOf(firstLetter) >= 0;
        }

        /// <summary>
        /// For objects that don't have a PropertyString.PluralName
        /// </summary>
        public static string Pluralize(this string name)
            => name.EndsWith("us") ? name + "s" // This should be i but pcap shows "You have killed 4 Sarcophaguss! Your task is complete!"
            : (name.EndsWith("ch") || name.EndsWith("s") || name.EndsWith("sh") || name.EndsWith("x") || name.EndsWith("z")) ? name + "es"
            : name.EndsWith("th") ? name
            : name + "s";
    }
}
