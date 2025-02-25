using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Twilio.AspNet.Core;
using Twilio.Clients;
using Twilio.Rest.Trunking.V1;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddTwilioRequestValidation();
builder.Services.AddTwilioClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/test", async (HttpContext httpContext) =>
{
    // if (IsValidTwilioRequest(httpContext) == false)
    //     return Results.StatusCode((int) HttpStatusCode.Forbidden);
    
    var options = httpContext.RequestServices
        .GetService<Microsoft.Extensions.Options.IOptions<TwilioClientOptions>>()
        ?.Value ?? throw new Exception("Twilio missing.");

    var twilioRestClient = new TwilioRestClient(
        options.AccountSid,
        options.AuthToken,
        httpClient: new Twilio.Http.SystemNetHttpClient(new HttpClient())
    );

    var configuration = httpContext.RequestServices
        .GetService<IConfiguration>();

    var from = configuration.GetSection("TwilioMotion:From").Value;
    var to = configuration.GetSection("TwilioMotion:To").Value;

    try
    {
        // Make a call using the SIP Trunk 
        var call = await CallResource.CreateAsync(
            url: new Uri("http://demo.twilio.com/docs/voice.xml"),
            to: new PhoneNumber(to),
            from: new PhoneNumber(from),
            client: twilioRestClient
        );

        await Task.Delay(100000);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }

    return TypedResults.Ok();
});

app.Run();

bool IsValidTwilioRequest(HttpContext httpContext)
{
    var options = httpContext.RequestServices
        .GetService<Microsoft.Extensions.Options.IOptions<TwilioRequestValidationOptions>>()
        ?.Value ?? throw new Exception("TwilioRequestValidationOptions missing.");

    string? urlOverride = null;
    if (options.BaseUrlOverride is not null)
    {
        var request = httpContext.Request;
        urlOverride = $"{options.BaseUrlOverride.TrimEnd('/')}{request.Path}{request.QueryString}";
    }

    return RequestValidationHelper.IsValidRequest(httpContext, options.AuthToken, urlOverride, options.AllowLocal ?? true);
}
