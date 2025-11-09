using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// The SizeDetails type implements BindAsync, which will be called during model binding
app.MapPost("/size", (SizeDetails size) => $"Size: {size}");

app.Run();

// It's possibly to create a custom type bound to HttpContext, either in whole or in part.
// To implement the binding, the custom type must contain one of two BindAsync overloads. The example below uses the more complex of the two methods; it includes a second parameter useful for obtaining details about the endpoint parameter being bound. The simpler overload has only an HttpContext parameter. At minimum, only the simpler of two overloads is required.
// Having one of these two overloads defined is enough. The IBindableFromHttpContext<T> interface formalises this requirement, specifying the implementing type must have the second, more complex of the two overloads that additionally accepts a ParameterInfo argument.
public record SizeDetails(double Height, double Width) : IBindableFromHttpContext<SizeDetails>
{
    // BindAsync returns either the instantiated instance of the implementing type -- in this case SizeDetails -- or null
    // If it returns null, one of two outcomes can be the result. If the endpoint parameter being bound is optional (achieved by declaring it as nullable), the endpoint handler will be called null as the argument for that parameter. If the endpoint parameter is required, binding fails and the EndpointMiddleware -- which determines which endpoint to call -- will throw BadRequestException and the middleware pipeline will return a 400 error response.
    // This method attempts to parse the body of the HTTP request as a pair of strings that can be parsed into doubles, separate by a newline
    public static async ValueTask<SizeDetails?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        using var streamReader = new StreamReader(context.Request.Body);

        if (await streamReader.ReadLineAsync(context.RequestAborted) is not string firstLine
            || !double.TryParse(firstLine, out double height))
        {
            // To trigger the correct 400 response from the pipeline -- indicating a problem with the request that client needs to resolve -- this method must return null should parsing fail, not throw an exception. If BindAsync throws, it will not be caught by the EndpointMiddleware, and the pipeline will return a 500 response.
            return null;
        }

        if (await streamReader.ReadLineAsync(context.RequestAborted) is not string secondLine ||
            !double.TryParse(secondLine, out double width))
        {
            return null;
        }

        return new SizeDetails(height, width);
    }
}