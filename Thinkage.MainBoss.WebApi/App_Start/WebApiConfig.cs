using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Thinkage.MainBoss.WebApi
{
	public static class WebApiConfig
	{
		public static void Register(HttpConfiguration config)
		{
			// Web API configuration and services
//			GlobalConfiguration.Configuration.Formatters.XmlFormatter.UseXmlSerializer = true;

//			GlobalConfiguration.Configuration.Formatters.JsonFormatter.Indent = true;
			var serializerSettings = GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings;
			serializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
			var contractResolver = (Newtonsoft.Json.Serialization.DefaultContractResolver)serializerSettings.ContractResolver;
			contractResolver.IgnoreSerializableAttribute = true;

			// Web API routes
			config.MapHttpAttributeRoutes();

			config.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/{controller}/{id}",
				defaults: new
				{
					id = RouteParameter.Optional
				}
			);
		}
	}
}
