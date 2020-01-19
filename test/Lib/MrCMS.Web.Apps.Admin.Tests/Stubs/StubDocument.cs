using System.Collections.Generic;
using MrCMS.Entities.Documents;

namespace MrCMS.Web.Apps.Admin.Tests.Stubs
{
    public class StubDocument : Document
    {
        public virtual void SetVersions(List<DocumentVersion> versions)
        {
            Versions = versions;
        }

        public override string UrlSegment { get; set; }
    }
}