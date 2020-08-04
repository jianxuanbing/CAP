using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;
using AspectCore.DynamicProxy.Parameters;
using AspectCore.Extensions.AspectScope;
using AspectCore.Extensions.DependencyInjection;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Savorboard.CAP.InMemoryMessageQueue;

namespace Sample.AspectCoreWithCap
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ITestEventHandler, TestEventHandler>();
            services.AddCap(o =>
            {
                o.UseInMemoryStorage();
                // 设置处理成功的数据在数据库中保存的时间（秒），为保证系统性能，数据会定期清理
                o.SucceedMessageExpiredAfter = 24 * 3600;
                // 设置失败重试次数
                o.FailedRetryCount = 5;
                o.Version = "bing_test";
                // 启用内存队列
                o.UseInMemoryMessageQueue();
            });

            services.AddControllers();
            services.ConfigureDynamicProxy(config =>
            {
                config.EnableParameterAspect();
                config.ThrowAspectException = false;
            });
            services.AddScoped<IAspectScheduler, ScopeAspectScheduler>();
            services.AddScoped<IAspectBuilderFactory, ScopeAspectBuilderFactory>();
            services.AddScoped<IAspectContextFactory, ScopeAspectContextFactory>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public interface ITestEventHandler
    {
        Task WritLogAsync(string message);
    }

    public class TestEventHandler : ITestEventHandler, DotNetCore.CAP.ICapSubscribe
    {
        private readonly IServiceProvider _serviceProvider;

        public TestEventHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [Test]
        [CapSubscribe("test")]
        public Task WritLogAsync(string message)
        {
            var result = _serviceProvider.GetService<ITestEventHandler>();
            Console.WriteLine(message);
            return Task.CompletedTask;
        }
    }

    public class TestAttribute : AbstractInterceptorAttribute
    {
        public int[] Times { get; set; } = { 1, 100, 10000, 1000000 };
        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            foreach (var times in Times)
            {
                for (var i = 0; i < times; i++)
                    await next(context);
            }
        }
    }
}
