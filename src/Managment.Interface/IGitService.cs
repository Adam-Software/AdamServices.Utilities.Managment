using System.Threading.Tasks;

namespace Managment.Interface
{
    public interface IGitService
    {
        public void Clone(string repositoryUrl);
    }
}
