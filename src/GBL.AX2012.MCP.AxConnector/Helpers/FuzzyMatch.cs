namespace GBL.AX2012.MCP.AxConnector.Helpers;

public static class FuzzyMatch
{
    public static int Score(string search, string target)
    {
        if (string.IsNullOrEmpty(search) || string.IsNullOrEmpty(target))
            return 0;
        
        search = search.ToLowerInvariant();
        target = target.ToLowerInvariant();
        
        // Exact match
        if (target == search) return 100;
        
        // Contains match
        if (target.Contains(search)) return 90 - Math.Min(target.Length - search.Length, 20);
        
        // Starts with
        if (target.StartsWith(search)) return 85;
        
        // Levenshtein distance
        var distance = LevenshteinDistance(search, target);
        var maxLen = Math.Max(search.Length, target.Length);
        var similarity = (1 - (double)distance / maxLen) * 100;
        
        return (int)Math.Max(0, similarity);
    }
    
    private static int LevenshteinDistance(string s1, string s2)
    {
        var m = s1.Length;
        var n = s2.Length;
        var d = new int[m + 1, n + 1];
        
        for (var i = 0; i <= m; i++) d[i, 0] = i;
        for (var j = 0; j <= n; j++) d[0, j] = j;
        
        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        
        return d[m, n];
    }
}
