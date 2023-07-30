namespace KinoshitaProductions.Common.Helpers;

public static class WildcardMatchHelper
{
    // ReSharper disable once UnusedMember.Global
    public static bool IsMatch(string textToFilter, string wildcardFilter)
    {
        if (string.IsNullOrEmpty(textToFilter) || string.IsNullOrEmpty(wildcardFilter))
            return false;

        var exactStart = wildcardFilter[0] != '*';
#if NET7_0_OR_GREATER
        var exactEnd = wildcardFilter[^1] != '*';
#else
        var exactEnd = wildcardFilter[wildcardFilter.Length - 1] != '*';
#endif

        var startPosition = exactStart ? 0 : 1;
        var endPosition = wildcardFilter.Length - startPosition - (exactEnd ? 0 : 1);

        var requiredMatches = wildcardFilter.Substring(startPosition, endPosition)
            .Split('*', StringSplitOptions.RemoveEmptyEntries);

        if (exactStart && exactEnd && requiredMatches.Length == 1)
        {
            return textToFilter.Equals(wildcardFilter);
        }

        var currentPosition = 0;
        for (var matchIndex = 0; matchIndex < requiredMatches.Length; ++matchIndex)
        {
            if (matchIndex == 0 && exactStart && !textToFilter.StartsWith(requiredMatches[matchIndex]))
            {
                return false;
            }
            else if (matchIndex == requiredMatches.Length - 1 && exactEnd &&
                     !textToFilter.EndsWith(requiredMatches[matchIndex]))
            {
                return false;
            }

            var newPosition = textToFilter.IndexOf(requiredMatches[matchIndex], currentPosition,
                StringComparison.Ordinal);
            if (newPosition == -1)
            {
                return false;
            }

            currentPosition = newPosition + requiredMatches[matchIndex].Length;
        }

        return true;
    }
}
