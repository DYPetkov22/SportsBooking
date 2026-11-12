using Microsoft.AspNetCore.Routing;

namespace SportsBooking.Web.Infrastructure;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // A disconnected client must not be turned into an internal server error.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled request error. TraceId: {TraceId}; Path: {Path}",
                context.TraceIdentifier, context.Request.Path.Value);
            if (context.Response.HasStarted) throw;

            var path = context.Request.Path;
            var query = context.Request.QueryString;
            var endpoint = context.GetEndpoint();
            var routeValues = context.Request.RouteValues;
            try
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.Headers.CacheControl = "no-store";
                context.Request.Path = "/Errors/500";
                context.Request.QueryString = QueryString.Empty;
                context.SetEndpoint(null);
                context.Request.RouteValues = new RouteValueDictionary();
                try { await next(context); }
                catch (Exception errorPageException)
                {
                    logger.LogError(errorPageException, "Error page failed. TraceId: {TraceId}", context.TraceIdentifier);
                    if (context.Response.HasStarted) throw;
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.Headers.CacheControl = "no-store";
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("<!doctype html><html lang=\"bg\"><meta charset=\"utf-8\"><title>500</title><h1>500 — Възникна грешка</h1><p>Опитайте отново по-късно.</p></html>");
                }
            }
            finally
            {
                context.Request.Path = path;
                context.Request.QueryString = query;
                context.SetEndpoint(endpoint);
                context.Request.RouteValues = routeValues;
            }
        }
    }
}
