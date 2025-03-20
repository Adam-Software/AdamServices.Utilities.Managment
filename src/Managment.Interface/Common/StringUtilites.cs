using System;

namespace Managment.Interface.Common
{
    public static class StringUtilites
    {
        /// <summary>
        /// Compares software versions in the 0.0.0.0 format
        /// </summary>
        /// <param name="version1">Software versions in the 0.0.0.0 format</param>
        /// <param name="version2">Software versions in the 0.0.0.0 format</param>
        /// <returns>
        /// -1 if version1 is less than version2
        ///  0 if version1 equal version2
        ///  1 if version1 is greater than version2
        /// </returns>
        public static int CompareVersions(string version1, string version2)
        {
            string[] parts1 = version1.Split('.');
            string[] parts2 = version2.Split('.');

            int length = Math.Max(parts1.Length, parts2.Length);
            for (int i = 0; i < length; i++)
            {
                int num1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
                int num2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;

                if (num1 < num2) return -1;
                if (num1 > num2) return 1;
            }

            return 0; 
        }
    }
}
