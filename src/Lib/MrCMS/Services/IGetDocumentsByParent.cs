using System.Collections.Generic;
using System.Threading.Tasks;
using MrCMS.Entities.Documents;

namespace MrCMS.Services
{
    public interface IGetDocumentsByParent<T> where T : Document
    {
        Task<List<T>> GetDocuments(T parent);
    }
}