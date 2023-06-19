var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseMiddleware<OperationCanceledMiddleware>();  // <-- Add this middleware

app.MapGet("/", () => "Hello World!");

app.MapGet("/slowtest", (CancellationToken token) =>
{
    app.Logger.LogInformation("Starting to do slow work");

    for(var i=0; i<10; i++)
    {
        token.ThrowIfCancellationRequested();
        // slow non-cancellable work
        Thread.Sleep(1000);
    }

    var message = "Finished slow delay of 10 seconds.";

    app.Logger.LogInformation(message);

    return message;
});

app.Run();

class OperationCanceledMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OperationCanceledMiddleware> _logger;
    public OperationCanceledMiddleware(
        RequestDelegate next, 
        ILogger<OperationCanceledMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch(OperationCanceledException)
        {
            _logger.LogInformation("Request was cancelled");
            context.Response.StatusCode = 409;
        }
    }
}