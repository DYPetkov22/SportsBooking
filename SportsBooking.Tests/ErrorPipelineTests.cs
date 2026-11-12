using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SportsBooking.Web.Controllers;
using SportsBooking.Web.Infrastructure;

namespace SportsBooking.Tests;

public sealed class ErrorPipelineTests
{
    private static TestServer Server() => new(new WebHostBuilder()
        .UseEnvironment("Production")
        .ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddControllersWithViews(options =>
                options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()))
                .AddApplicationPart(typeof(ErrorsController).Assembly);
        })
        .Configure(app =>
        {
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseStatusCodePagesWithReExecute("/Errors/{0}");
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.Map("/throw", (RequestDelegate)(_ => throw new InvalidOperationException("PRIVATE_INTERNAL_DETAILS")));
                endpoints.Map("/missing-record", context => { context.Response.StatusCode = 404; return Task.CompletedTask; });
                endpoints.Map("/ok", context => context.Response.WriteAsync("OK"));
            });
        }));

    [Fact]
    public async Task UnknownRouteRenders404WithoutRedirect()
    {
        using var server = Server();
        var response = await server.CreateClient().GetAsync("/no-such-page");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Contains("404 — Страницата не е намерена", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MissingRecordRenders404()
    {
        using var server = Server();
        var response = await server.CreateClient().GetAsync("/missing-record");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Търсената страница или запис не съществува.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExceptionRenders500WithoutLeakingDetails()
    {
        using var server = Server();
        var response = await server.CreateClient().GetAsync("/throw");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("500 — Възникна грешка", html);
        Assert.DoesNotContain("PRIVATE_INTERNAL_DETAILS", html);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task PostExceptionRenders500WithoutAntiforgeryRejection()
    {
        using var server = Server();
        var response = await server.CreateClient().PostAsync("/throw", new StringContent(""));
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("500 — Възникна грешка", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SuccessfulResponseIsUnchanged()
    {
        using var server = Server();
        var response = await server.CreateClient().GetAsync("/ok");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("OK", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FailingErrorPageFallsBackToSafe500AndRestoresRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/original";
        context.Request.QueryString = new("?a=1");
        context.Response.Body = new MemoryStream();
        var middleware = new ErrorHandlingMiddleware(_ => throw new InvalidOperationException("PRIVATE_INTERNAL_DETAILS"),
            NullLogger<ErrorHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        var html = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Contains("500 — Възникна грешка", html);
        Assert.DoesNotContain("PRIVATE_INTERNAL_DETAILS", html);
        Assert.Equal("/original", context.Request.Path.Value);
        Assert.Equal("?a=1", context.Request.QueryString.Value);
    }

    [Fact]
    public async Task ClientCancellationIsNotConvertedTo500()
    {
        var context = new DefaultHttpContext { RequestAborted = new CancellationToken(true) };
        var middleware = new ErrorHandlingMiddleware(_ => throw new OperationCanceledException(),
            NullLogger<ErrorHandlingMiddleware>.Instance);
        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
        Assert.Equal(200, context.Response.StatusCode);
    }
}
