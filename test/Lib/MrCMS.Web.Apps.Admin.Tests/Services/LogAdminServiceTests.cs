using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MrCMS.Helpers;
using MrCMS.Logging;
using MrCMS.TestSupport;
using MrCMS.Web.Apps.Admin.Services;
using Xunit;

namespace MrCMS.Web.Apps.Admin.Tests.Services
{
    public class LogAdminServiceTests : MrCMSTest
    {
        private readonly LogAdminService _logService;

        public LogAdminServiceTests()
        {
            _logService = new LogAdminService(Context);
        }

        [Fact]
        public void LogAdminService_GetAllLogEntries_ReturnsAllLogEntries()
        {
            List<Log> list = CreateLogList();

            IList<Log> logs = _logService.GetAllLogEntries();

            logs.Should().BeEquivalentTo(list);
        }

        [Fact]
        public void LogAdminService_DeleteAllLogs_ShouldRemoveAllLogs()
        {
            List<Log> list = CreateLogList();

            _logService.DeleteAllLogs();

            Context.QueryOver<Log>().RowCount().Should().Be(0);
        }

        [Fact]
        public void LogAdminService_DeleteLog_ShouldRemoveTheDeletedLog()
        {
            List<Log> list = CreateLogList();

            _logService.DeleteLog(list[0].Id);

            Context.QueryOver<Log>().List().Should().NotContain(list[0]);
        }


        private List<Log> CreateLogList()
        {
            List<Log> logList =
                Enumerable.Range(1, 20).Select(i => new Log { Message = i.ToString() }).ToList();
            logList.ForEach(log => Context.Transact(session => session.Save(log)));
            return logList;
        }
    }
}