using System;
using System.Linq;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using System.Web.Http;
using EPiServer.Logging;

namespace ServiceAPIExtensions.Business
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.ServiceApi.IntegrationInitialization))]
    public class APIInit : IInitializableModule //IInitializableHttpModule
    {
        private static readonly ILogger _log = LogManager.GetLogger(typeof(APIInit));

        public void Initialize(InitializationEngine context)
        {
            //GlobalConfiguration.Configure(WebApiExtConfig.Register);

            GlobalConfiguration.Configure(delegate(HttpConfiguration config)
            {
                //// Web API routes - Enabling Attribute Routing
                //config.MapHttpAttributeRoutes();

                config.Formatters.Add(new BinaryMediaTypeFormatter());
                config.EnsureInitialized();
            });
        }

        public void Preload(string[] parameters) { }

        public void Uninitialize(InitializationEngine context)
        {
            //Add uninitialization logic
        }

        public void InitializeHttpEvents(System.Web.HttpApplication application)
        {
            //throw new NotImplementedException();
        }
    }

    public static class WebApiExtConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.EnableSystemDiagnosticsTracing();

            config.Formatters.Add(new BinaryMediaTypeFormatter());

            //// Web API routes - Enabling Attribute Routing
            //config.MapHttpAttributeRoutes();

            //config.Routes.MapHttpRoute(
            //    name: "DefaultApi",
            //    routeTemplate: "api/{controller}/{id}",
            //    defaults: new { id = RouteParameter.Optional }
            //);

            config.Routes.MapHttpRoute(
                name: "Api extensions",
                //routeTemplate: "dummydata/{controller}/{id}",                
                //defaults: new { controller = "Hotels", id = RouteParameter.Optional }
                routeTemplate: "apiext/{controller}/{contentId}",
                defaults: new { controller = "ContentAPi", contentId = RouteParameter.Optional }
            );

            config.EnsureInitialized();
        }
    }
}