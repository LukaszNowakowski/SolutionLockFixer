namespace SolutionLockFixer
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    public class TargetsEqualityComparer : IEqualityComparer<JProperty>
    {
        public bool Equals(JProperty x, JProperty y)
        {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(JProperty obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
