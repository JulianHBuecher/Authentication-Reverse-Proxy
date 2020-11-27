using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer
{
	public static class MiddlewareForRequestDiagnosticForDebugging
	{
		static readonly string inspectPath = "/show-me-my-headers";
		static readonly (string, Uri) redirectPath = ("/redirect-to-stp", new Uri("https://stp-online.de"));

		public static void UseHeaderInspectionBeforeAfter(this IApplicationBuilder app, Action<IApplicationBuilder> inbetween)
		{
			app.UseHeaderInspectionBefore();
			inbetween(app);
			app.UseHeaderInspectionAfter();
		}

		public static void UseHeaderInspectionBefore(this IApplicationBuilder app)
		{
			app.Use(Before(inspectPath, redirectPath));
		}

		public static void UseHeaderInspectionAfter(this IApplicationBuilder app)
		{
			app.Use(After(inspectPath));
		}





		private static Func<HttpContext, Func<Task>, Task> Before(string inspectPath, (string, Uri) redirectPath)
		{
			return async (context, next) =>
			{
				if (context.Request.Path.Equals(redirectPath.Item1))
				{
					context.Response.Redirect(redirectPath.Item2.ToString());
				}
				else if (context.Request.Path.Equals(inspectPath))
				{
					var beforeHeaders = context.Request.Headers.ToArray();
					var beforePath = context.Request.Path;
					var beforePathBase = context.Request.PathBase;

					var config = (IConfiguration)context.RequestServices.GetService(typeof(IConfiguration));
					var configFabric_ServiceName = config["Fabric_ServiceName"];

					await next.Invoke();

					var afterHeaders = context.Request.Headers.ToArray();
					var afterPath = context.Request.Path;
					var afterPathBase = context.Request.PathBase;

					await context.Response.WriteAsync($@"
<!DOCTYPE html>
<html>
<head>
<title>UM.ID - RequestDiagnosticForDebugging</title>
</head>
<body>
<h1>UM.ID - RequestDiagnosticForDebugging</h1>
Fabric_ServiceName (config): {configFabric_ServiceName}<br>
<h2>path before</h2>
{beforePath}<br>{beforePathBase}
<h2>headers before</h2>
{string.Join("<br>", beforeHeaders.Select(h => $"{h.Key}: {h.Value}"))}
<h2>path after</h2>
{afterPath}<br>{afterPathBase}
<h2>headers after</h2>
{string.Join("<br>", afterHeaders.Select(h => $"{h.Key}: {h.Value}"))}
<h2>environment variables</h2>
{string.Join("<br>", Environment.GetEnvironmentVariables().OfType<DictionaryEntry>().Select(h => $"{h.Key}: {h.Value}"))}
</body>
</html>"
					);
				}
				else
				{
					//dont inspect anything
					await next.Invoke();
				}
			};
		}

		private static Func<HttpContext, Func<Task>, Task> After(string inspectPath)
		{
			return async (context, next) =>
			{
				if (!context.Request.Path.Equals(inspectPath))
				{
					await next.Invoke();
				}
			};
		}
	}
}
