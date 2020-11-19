module EchoServer.App

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Giraffe.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open System
open System.IO
open System.Text.Json

// ---------------------------------
// Web app
// ---------------------------------

let echo: HttpHandler =
    fun next ctx -> task {
        let req = ctx.Request
        use reader = new StreamReader(req.Body)
        let! content = reader.ReadToEndAsync()

        let responseBody =
            {|
                Method = req.Method
                Path = req.Path.Value
                QueryString = req.QueryString.Value
                Content = content
            |}
        return! json responseBody next ctx
    }

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> json {| Error = ex.Message |}

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder
        .WithOrigins("http://localhost:8080")
        .AllowAnyMethod()
        .AllowAnyHeader()
        |> ignore

let configureApp (app : IApplicationBuilder) =
    app
        .UseGiraffeErrorHandler(errorHandler)
        .UseCors(configureCors)
        .UseGiraffe(echo)

let configureServices (services : IServiceCollection) =
    services
        .AddCors()
        .AddGiraffe()
        .AddSingleton<IJsonSerializer>(SystemTextJsonSerializer(JsonSerializerOptions()))
    |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder
        .AddConsole()
        .AddDebug()
    |> ignore

[<EntryPoint>]
let main args =
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseUrls("http://localhost:8080")
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                |> ignore
        )
        .Build()
        .Run()
    0