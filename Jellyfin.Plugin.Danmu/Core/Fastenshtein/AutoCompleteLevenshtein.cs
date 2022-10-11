namespace Fastenshtein
{
    /// <summary>
    /// Measures the difference between two strings.
    /// Uses the Levenshtein string difference algorithm.
    /// </summary>
    public static class AutoCompleteLevenshtein
    {
        /// <summary>
        /// Compares the two strings and returns a measure of their summarily with 0 being an exact match.
        /// </summary>
        /// <param name="value1">the incomplete value entered by the user</param>
        /// <param name="value2">the value to compare value1 against</param>
        /// <returns>0 exact match less a positive value, lower the value the best match</returns>
        public static int Distance(string value1, string value2)
        {
            if (value1.Length == 0)
            {
                return 0;
            }

            int[] costs = new int[value1.Length];

            // Add indexing for insertion to first row
            for (int i = 0; i < costs.Length;)
            {
                costs[i] = ++i;
            }

            int minSize = value1.Length < value2.Length ? value1.Length : value2.Length;

            for (int i = 0; i < minSize; i++)
            {
                // cost of the first index
                int cost = i;
                int previousCost = i;

                // cache value for inner loop to avoid index lookup and bonds checking, profiled this is quicker
                char value2Char = value2[i];

                for (int j = 0; j < value1.Length; j++)
                {
                    int currentCost = cost;

                    // assigning this here reduces the array reads we do, improvement of the old version
                    cost = costs[j];

                    if (value2Char != value1[j])
                    {
                        if (previousCost < currentCost)
                        {
                            currentCost = previousCost;
                        }

                        if (cost < currentCost)
                        {
                            currentCost = cost;
                        }

                        ++currentCost;
                    }

                    /* 
                     * Improvement on the older versions.
                     * Swapping the variables here results in a performance improvement for modern intel CPU’s, but I have no idea why?
                     */
                    costs[j] = currentCost;
                    previousCost = currentCost;
                }
            }

            return costs[costs.Length - 1];
        }
    }
}
