/*
 * MCP server — Family Movies tools (ASP.NET Core / .NET 8).
 *
 * Environment variables:
 *   DB_PATH   Path to the SQLite database file.
 *             Defaults to db/family_movies.sqlite (local dev).
 *             Set to /app/db/family_movies.sqlite when running in Docker.
 */
using McpOrdersServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DatabaseService>();
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => { options.Stateless = true; })
    .WithToolsFromAssembly();

var app = builder.Build();
app.MapMcp();
app.Run();
