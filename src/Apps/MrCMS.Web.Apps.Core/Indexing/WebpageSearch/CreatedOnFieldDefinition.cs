using System.Collections.Generic;
using Lucene.Net.Documents;
using MrCMS.Entities.Documents.Web;
using MrCMS.Indexing.Management;

namespace MrCMS.Web.Apps.Core.Indexing.WebpageSearch
{
    public class CreatedOnFieldDefinition : StringFieldDefinition<WebpageSearchIndexDefinition, Webpage>
    {
        public CreatedOnFieldDefinition(ILuceneSettingsService luceneSettingsService)
            : base(luceneSettingsService, "createdon")
        {
        }

        protected override async IAsyncEnumerable<string> GetValues(Webpage obj)
        {
            yield return DateTools.DateToString(obj.CreatedOn, DateTools.Resolution.SECOND);
        }
    }
}