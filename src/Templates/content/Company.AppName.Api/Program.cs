Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Company.AppName.Api.Startup>()).Build().Run();
