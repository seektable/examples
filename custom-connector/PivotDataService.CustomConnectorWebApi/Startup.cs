using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace PivotDataService.CustomConnectorWebApi {
	public class Startup {
		public Startup(IConfiguration configuration, IWebHostEnvironment env) {
			Configuration = configuration;
			ApplicationPath = env.ContentRootPath;
		}

		public IConfiguration Configuration { get; }

		public string ApplicationPath { get; set; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) {
			services.AddControllers();
			services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options => {
				options.AllowSynchronousIO = true;
			});

			var sqliteDbFileName = Path.Combine(ApplicationPath, "data/sample.db");
			services.AddScoped<Services.SQLiteDb>((srvPrv) => {
				return new Services.SQLiteDb(sqliteDbFileName);
			});

			if (!File.Exists(sqliteDbFileName)) {
				var folder = Path.GetDirectoryName(sqliteDbFileName);
				if (!Directory.Exists(folder))
					Directory.CreateDirectory(folder);
				var db = new Services.SQLiteDb(sqliteDbFileName);
				db.CreateSampleData();
			}

		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {

			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints => {
				endpoints.MapControllers();
			});
		}
	}
}
