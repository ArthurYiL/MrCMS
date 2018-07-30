using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MrCMS.DbConfiguration;
using MrCMS.Entities;
using MrCMS.Settings;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using X.PagedList;
using ISession = NHibernate.ISession;

namespace MrCMS.Helpers
{
    public static class SessionHelper
    {
        // TODO: get this from DI
        public static int DefaultPageSize = 10;
        public static ISession OpenFilteredSession(this ISessionFactory sessionFactory, IServiceProvider serviceProvider)
        {
            var sessionBuilder = sessionFactory.WithOptions()
                .Interceptor(new MrCMSInterceptor(serviceProvider));
            var session = new MrCMSSession(sessionBuilder.OpenSession());
            session.EnableFilter("NotDeletedFilter");
            return session;
        }

        public static HttpContext GetContext(this ISession session)
        {
            return (session.GetSessionImplementation().Interceptor as MrCMSInterceptor)?.Context;
        }

        public static T GetService<T>(this ISession session)
        {
            return !(session.GetSessionImplementation().Interceptor is MrCMSInterceptor mrCMSInterceptor)
                ? default(T)
                : mrCMSInterceptor.ServiceProvider.GetRequiredService<T>();
        }

        public static TResult Transact<TResult>(this ISession session, Func<ISession, TResult> func)
        {
            if (!session.Transaction.IsActive)
            {
                // Wrap in transaction
                TResult result;
                using (ITransaction tx = session.BeginTransaction())
                {
                    result = func.Invoke(session);
                    tx.Commit();
                }
                return result;
            }

            // Don't wrap;
            return func.Invoke(session);
        }

        public static void Transact(this ISession session, Action<ISession> action)
        {
            Transact(session, ses =>
            {
                action.Invoke(ses);
                return false;
            });
        }

        public static TResult Transact<TResult>(this IStatelessSession session, Func<IStatelessSession, TResult> func)
        {
            if (!session.Transaction.IsActive)
            {
                // Wrap in transaction
                TResult result;
                using (ITransaction tx = session.BeginTransaction())
                {
                    result = func.Invoke(session);
                    tx.Commit();
                }
                return result;
            }

            // Don't wrap;
            return func.Invoke(session);
        }

        public static void Transact(this IStatelessSession session, Action<IStatelessSession> action)
        {
            Transact(session, ses =>
            {
                action.Invoke(ses);
                return false;
            });
        }

        public static IPagedList<T> Paged<T>(this ISession session, QueryOver<T> query, int pageNumber,
            int? pageSize = null)
            where T : SystemEntity
        {
            int size = pageSize ?? DefaultPageSize; // MrCMSApplication.Get<SiteSettings>().DefaultPageSize;
            IEnumerable<T> values =
                query.GetExecutableQueryOver(session).Skip((pageNumber - 1) * size).Take(size).Cacheable().List<T>();

            var rowCount = query.GetExecutableQueryOver(session).ToRowCountQuery().SingleOrDefault<int>();

            return new StaticPagedList<T>(values, pageNumber, size, rowCount);
        }

        public static IPagedList<TResult> Paged<TResult>(this IQueryOver<TResult, TResult> queryBase, int pageNumber,
            int? pageSize = null)
            where TResult : SystemEntity
        {
            int size = pageSize ?? DefaultPageSize; //MrCMSApplication.Get<SiteSettings>().DefaultPageSize;
            IEnumerable<TResult> results = queryBase.Skip((pageNumber - 1) * size).Take(size).Cacheable().List();

            int rowCount = queryBase.Cacheable().RowCount();

            return new StaticPagedList<TResult>(results, pageNumber, size, rowCount);
        }

        public static IPagedList<TResult> Paged<TResult>(this IQueryable<TResult> queryable, int pageNumber,
            int? pageSize = null)
            where TResult : SystemEntity
        {
            int size = pageSize ?? DefaultPageSize; //MrCMSApplication.Get<SiteSettings>().DefaultPageSize;

            IQueryable<TResult> cacheable = queryable.WithOptions(options => options.SetCacheable(true));

            return new PagedList<TResult>(cacheable, pageNumber, size);
        }

        public static bool Any<T>(this IQueryOver<T> query)
        {
            return query.RowCount() > 0;
        }

        public static T GetByGuid<T>(this ISession session, Guid guid) where T : SystemEntity
        {
            // we use list here, as it seems to cache more performantly than .SingleOrDefault()
            return session.QueryOver<T>().Where(x => x.Guid == guid).Cacheable().List().FirstOrDefault();
        }
    }
}