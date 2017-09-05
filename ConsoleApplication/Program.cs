using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();

            services.AddTransientWithInterception<ISampleService, SampleService>(m => m.InterceptBy<LogInterceptor>());

            var provider = services.BuildServiceProvider();

            var service = provider.GetRequiredService<ISampleService>();
            service.Call();
        }
    }

    public interface ISampleService
    {
        void Call();
    }

    public class SampleService : ISampleService
    {
        public virtual void Call()
        {
            Console.WriteLine("Hello Sample");
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTransientWithInstrumentation<T, TImplementation>(this IServiceCollection services) 
            where T : class
            where TImplementation: class, T
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddTransient<LogInterceptor>();
            services.TryAddTransient<TImplementation>();

            services.AddTransient(sp =>
            {
                var logInterceptor = sp.GetRequiredService<LogInterceptor>();
                var implementation = sp.GetRequiredService<TImplementation>();
                
                var proxyFactory = new ProxyGenerator();
                return proxyFactory.CreateInterfaceProxyWithTarget<T>(implementation, logInterceptor);
            });

            return services;
        }        
        
        public static IServiceCollection AddTransientWithInterception<T, TImplementation>(this IServiceCollection services, Action<IInterceptBy> action) 
            where T : class
            where TImplementation: class, T
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var interceptionOptions = new InterceptionOptions();
            action?.Invoke(interceptionOptions);

            interceptionOptions.Interceptors.ForEach(services.TryAddTransient);
            services.TryAddTransient<TImplementation>();

            services.AddTransient(sp =>
            {
                var interceptorInstances = interceptionOptions.Interceptors.Select(sp.GetRequiredService).Cast<IInterceptor>().ToArray();
                var implementation = sp.GetRequiredService<TImplementation>();
                
                var proxyFactory = new ProxyGenerator();
                return proxyFactory.CreateInterfaceProxyWithTarget<T>(implementation, interceptorInstances);
            });

            return services;
        }
        
        
    }

    public class InterceptionOptions : IInterceptBy, IThenInterceptBy
    {
        private readonly IDictionary<Type, Type> _interceptors = new Dictionary<Type, Type>();

        public void UseMethodSelectionConvention<TConvention>() where TConvention : IMethodSelectionConvenstion, new()
        {
            Convention = new TConvention();
        }

        public IThenInterceptBy ThenBy<TInterceptor>() where TInterceptor : IInterceptor
        {
            if (_interceptors.ContainsKey(typeof(TInterceptor)))
                _interceptors.Add(typeof(TInterceptor), typeof(TInterceptor));

            return this;
        }

        public IThenInterceptBy InterceptBy<TInterceptor>() where TInterceptor : IInterceptor
        {
            _interceptors.Clear();
            
            _interceptors.Add(typeof(TInterceptor), typeof(TInterceptor));

            return this;
        }

        public List<Type> Interceptors => _interceptors.Values.ToList();

        public IMethodSelectionConvenstion Convention { get; private set; } = new DefaultMethodSelectionConvenstion();
    }
    
    public interface IInterceptBy
    {
        IThenInterceptBy InterceptBy<TInterceptor>() where TInterceptor : IInterceptor;
    }

    public interface IThenInterceptBy : IUseMethodConvention
    {
        IThenInterceptBy ThenBy<TInterceptor>() where TInterceptor : IInterceptor;
    }

    public interface IUseMethodConvention
    {
        void UseMethodSelectionConvention<TConvention>() where TConvention : IMethodSelectionConvenstion, new();
    }

    public class LogInterceptor : IInterceptor
    {
        public virtual void Intercept(IInvocation invocation)
        {
            Console.WriteLine($"Before {invocation.Method.Name}");
            
            invocation.Proceed();
            
            Console.WriteLine($"After {invocation.Method.Name}");
        }
    }
}