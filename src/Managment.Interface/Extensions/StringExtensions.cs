using System.Text;
using System;

namespace Managment.Interface.Extensions
{
    public static class StringExtensions
    {
        #region Uri extensions

        public static string ConvertGitHubLinkToRaw(this string link)
        {
            Uri originalUri = new(link);

            if (originalUri.Host.Equals("raw.githubusercontent.com", StringComparison.CurrentCultureIgnoreCase))
            {
                if(originalUri.Segments.Length != 7)
                    throw new ArgumentException("Invalid GitHubUserContent argument format");

                return originalUri.ToString();
            }

            if (!originalUri.Host.Equals("github.com", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new ArgumentException("The provided link is not a GitHub link.");
            }

            string[] pathParts = originalUri.AbsolutePath.Trim('/').Split('/');

            if (pathParts.Length < 5)
            {
                throw new ArgumentException("Invalid GitHub argument format");
            }

            string userName = pathParts[0];
            string repoName = pathParts[1];
            string heads = "refs/heads"; 
            string branch = pathParts[3]; 
            string filePath = pathParts[4];

            StringBuilder rawLinkBuilder = new();
            rawLinkBuilder.Append("https://raw.githubusercontent.com/")
                          .Append(userName)
                          .Append('/')
                          .Append(repoName)
                          .Append('/')
                          .Append(heads)
                          .Append('/')
                          .Append(branch)
                          .Append('/')
                          .Append(filePath);

            string rawLink = rawLinkBuilder.ToString().Split('#')[0];
            return rawLink;
        }

        public static string ConvertToRepositoryName(this string url)
        {
            Uri uri = new(url);
            if (!uri.Host.Equals("raw.githubusercontent.com", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new ArgumentException("The provided URL is not a valid GitHub raw URL.");
            }
            string[] pathParts = uri.AbsolutePath.Trim('/').Split('/');

            if (pathParts.Length < 4)
            {
                throw new ArgumentException("Invalid GitHub raw URL format.");
            }

            string repositoryName = pathParts[1];

            return repositoryName;
        }

        public static string ConvertRawUrlToGitUrl(this string rawUrl)
        {
            Uri rawUri = new(rawUrl);

            if (!rawUri.Host.Equals("raw.githubusercontent.com", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new ArgumentException("The provided URL is not a valid GitHub raw URL.");
            }

            string[] pathParts = rawUri.AbsolutePath.Trim('/').Split('/');

            if (pathParts.Length < 4)
            {
                throw new ArgumentException("Invalid GitHub raw URL format.");
            }

            string userName = pathParts[0];
            string repoName = pathParts[1];

            StringBuilder gitUrlBuilder = new();
            gitUrlBuilder.Append("https://github.com/")
                         .Append(userName)
                         .Append('/')
                         .Append(repoName)
                         .Append(".git");

            return gitUrlBuilder.ToString();
        }

        #endregion
    }
}
