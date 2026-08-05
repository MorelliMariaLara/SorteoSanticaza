using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SorteoSanticaza.Data;
using SorteoSanticaza.Services;

namespace SorteoSanticaza
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LoadEnvFiles();
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<Db>();
                Console.WriteLine("  SQL Server: " + MaskConnectionString(db.ConnectionString));
                try
                {
                    db.EnsureCreatedAndSeeded();
                    Console.WriteLine("  Base SorteosSantiCaza lista.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  ERROR SQL Server: " + ex.Message);
                    Console.WriteLine("  Revisá que exista LARA-NB\\SQLEXPRESS02 y la BD SorteosSantiCaza.");
                    Console.WriteLine("  Script: database/01_CreateDatabaseAndTables.sql");
                    throw;
                }

                try
                {
                    var pay = scope.ServiceProvider.GetRequiredService<PaymentService>();
                    Console.WriteLine("  " + pay.CredentialDiagnostics());
                }
                catch
                {
                    /* ignore */
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var key in new[]
                             {
                                 "APP_URL", "PORT",
                                 "MP_PUBLIC_KEY", "MP_ACCESS_TOKEN",
                                 "MP_ALLOW_SIMULATE", "MP_WEBHOOK_URL",
                                 "AdminPassword", "CONNECTION_STRING",
                             })
                    {
                        var val = Environment.GetEnvironmentVariable(key);
                        if (!string.IsNullOrEmpty(val))
                            dict[key] = val;
                    }

                    var connEnv = Environment.GetEnvironmentVariable("CONNECTION_STRING");
                    if (!string.IsNullOrEmpty(connEnv))
                        dict["ConnectionStrings:SorteosSantiCaza"] = connEnv;

                    if (dict.Count > 0)
                        config.AddInMemoryCollection(dict);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static void LoadEnvFiles()
        {
            var roots = new[]
            {
                Directory.GetCurrentDirectory(),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..")),
                AppContext.BaseDirectory,
            };

            foreach (var file in new[] { ".env", ".env.local" })
            {
                foreach (var root in roots.Distinct())
                {
                    var candidate = Path.Combine(root, file);
                    if (!File.Exists(candidate)) continue;
                    Console.WriteLine("  .env cargado: " + candidate);
                    foreach (var line in File.ReadAllLines(candidate))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                        var i = trimmed.IndexOf('=');
                        if (i <= 0) continue;
                        var key = trimmed.Substring(0, i).Trim();
                        var val = trimmed.Substring(i + 1).Trim().Trim('"').Trim('\'');
                        Environment.SetEnvironmentVariable(key, val);
                    }

                    break;
                }
            }

            // Si MP está bien configurado, no forzar simulador
            var mpPk = Environment.GetEnvironmentVariable("MP_PUBLIC_KEY") ?? "";
            var mpTk = Environment.GetEnvironmentVariable("MP_ACCESS_TOKEN") ?? "";
            if ((mpPk.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase) ||
                 mpPk.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase)) &&
                (mpTk.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase) ||
                 mpTk.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase)) &&
                !mpPk.Contains("TEST-APP_USR", StringComparison.OrdinalIgnoreCase) &&
                !mpTk.Contains("TEST-APP_USR", StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable("MP_ALLOW_SIMULATE", "false");
            }
        }

        private static string MaskConnectionString(string cs)
        {
            if (string.IsNullOrEmpty(cs)) return "(vacío)";
            try
            {
                var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = cs };
                if (builder.ContainsKey("Password")) builder["Password"] = "****";
                if (builder.ContainsKey("Pwd")) builder["Pwd"] = "****";
                return builder.ConnectionString;
            }
            catch
            {
                return cs;
            }
        }
    }
}
