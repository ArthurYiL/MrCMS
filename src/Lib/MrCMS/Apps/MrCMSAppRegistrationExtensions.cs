﻿using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using MrCMS.Services.Resources;
using MrCMS.Website;
using MrCMS.Website.Filters;
using MrCMS.Website.Profiling;
using StackExchange.Profiling.Internal;

namespace MrCMS.Apps
{
    public static class MrCMSAppRegistrationExtensions
    {
        public static MrCMSAppContext AddMrCMSApps(this IServiceCollection serviceCollection,
            Action<MrCMSAppContext> action)
        {
            var appContext = new MrCMSAppContext();

            action(appContext);

            foreach (var app in appContext.Apps)
                serviceCollection = app.RegisterServices(serviceCollection);

            serviceCollection.AddSingleton(appContext);

            return appContext;
        }

        public static IMvcBuilder AddAppMvcConfig(this IMvcBuilder builder, MrCMSAppContext context)
        {
            foreach (var app in context.Apps)
                builder.AddApplicationPart(app.Assembly);

            return builder;
        }

        public static IMvcBuilder AddMvcForMrCMS(this IServiceCollection services, MrCMSAppContext appContext,
            IFileProvider fileProvider)
        {
            return services.AddMvc(options =>
                {
                    // add custom binder to beginning of collection
                    options.ModelBinderProviders.Insert(0, new SystemEntityModelBinderProvider());
                    options.Filters.Add<EndRequestHandlerFilter>();
                    options.Filters.Add<ProfilingAsyncActionFilter<WebpageCachingFilter>>();
                    options.Filters.Add<ActionProfilingFilter>();
                    options.Filters.Add<ResultProfilingFilter>();
                    options.Filters.Add<GoogleRecaptchaFilter>();
                    options.Filters.Add<HoneypotFilter>();
                    options.Filters.Add<DoNotCacheFilter>();
                    options.Filters.Add<ProfilingActionFilter>();
                    options.EnableEndpointRouting = false;
                    appContext.SetupMvcOptions(options);
                })
                .AddApplicationPart(Assembly.GetAssembly(typeof(MrCMSAppRegistrationExtensions)))
                .AddRazorRuntimeCompilation(options =>
                {
                    options.FileProviders.Add(fileProvider);
                })
                .AddRazorOptions(options =>
                {
                    options.ViewLocationExpanders.Insert(0, new WebpageViewExpander());
                    options.ViewLocationExpanders.Insert(1, new AppViewLocationExpander());
                    options.ViewLocationExpanders.Insert(2, new ThemeViewLocationExpander());
                })
                .AddViewLocalization()
                .AddMrCMSDataAnnotations()
                .AddDataAnnotationsLocalization()
                .AddAppMvcConfig(appContext);
        } 

        public static IFileProvider AddFileProvider(this IServiceCollection services,
            IWebHostEnvironment environment, MrCMSAppContext appContext)
        {
            var physicalProvider = environment.ContentRootFileProvider;
            var compositeFileProvider =
                new CompositeFileProvider(new[] { physicalProvider }.Concat(appContext.ViewFileProviders));

            services.AddSingleton<IFileProvider>(compositeFileProvider);

            return compositeFileProvider;
        }
    }
}