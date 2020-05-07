namespace SolutionLockFixer
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    public class DependenciesEqualityComparer : IEqualityComparer<JToken>
    {
        public bool Equals(JToken x, JToken y)
        {
            return x.Path.Equals(y.Path, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(JToken obj)
        {
            return obj.Path.ToLowerInvariant().GetHashCode();
        }
    }
}
