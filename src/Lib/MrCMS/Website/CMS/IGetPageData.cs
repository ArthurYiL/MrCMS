using System.Threading.Tasks;

namespace MrCMS.Website.CMS
{
    public interface IGetPageData
    {
        Task<PageData> GetData(string url, string method);
    }
}