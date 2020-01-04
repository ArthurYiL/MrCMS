using MrCMS.Entities.People;
using MrCMS.Web.Apps.Admin.Models;


namespace MrCMS.Web.Apps.Admin.Services
{
    public class AdminUserStatsService : IAdminUserStatsService
    {
        private readonly ISession _session;

        public AdminUserStatsService(ISession session)
        {
            _session = session;
        }

        public UserStats GetSummary()
        {
            return new UserStats
                   {
                       ActiveUsers = _session.QueryOver<User>().Where(x => x.IsActive).Cacheable().RowCount(),
                       InactiveUsers = _session.QueryOver<User>().WhereNot(x => x.IsActive).Cacheable().RowCount()
                   };
        }
    }
}