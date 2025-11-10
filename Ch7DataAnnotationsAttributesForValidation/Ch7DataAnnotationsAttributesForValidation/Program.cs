using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/users", (UserModel user) => user.ToString())
   // ASP.NET doesn't contain a built-in validator for Minimal APIs (*) (it does for Razor and MCV projects). DataAnnotations metadata to generate and return validation results. Microsoft's expects developers to source a third-party package or create their own.
   // A member of ASP.NET team published the MinimalApi.Extensions package (apparently unofficially), which contains the WithParameterValidation() extension method.
   // This method adds an endpoint filter that implements validation referencing the DataAnnotations attributes applied to the members of the endpoint handler parameters.
   .WithParameterValidation();

app.MapPost("/users/create", (CreateUserModel user) => user.ToString())
   .WithParameterValidation();

// Simple types declared directly in the endpoint handler signature cannot use DataAnnotations attributes for validation; the validator (**) looks specifically at the properties of types and whether they have DataAnnotations attributes; applying attributes directly to the parameter, as in the first example below, will therefore not work. Technically, int is type that has properties, one of which contains the integral value, but we can't access it to apply DataAnnotations attributes.
// One solution to this limitation is to use the [AsParameters] attribute and wrap the simple type parameter to be validated in a container type; the wrapped parameter will then be a property of its containing type, DataAnnotations attributes can therefore be attached to it, and they will be considered during validation.
// app.MapPost("/users/{id}", ([Range(1, 100) int id) => userId.ToString())
//   .WithParameterValidation();
app.MapPost("/users/{id}", ([AsParameters] UserIdModel userId) => userId.ToString())
   .WithParameterValidation();

app.Run();

// * To be specific, Minimal APIs doesn't include an API for automatically performing validation on endpoint parameters using the associated DataAnnotations metadata. The static DataAnnotations.Validator class appears to be a validator. Its validation methods operate on the metadata applied via DataAnnotations attributes and populates an enumerable with validation results should any of the fields not be in the specified form.
// This class appears to have existed for some time, apparently predating ASP.NET Core Maybe this class is what powers validation for Razor and MCV projects.
// Presumably, I could create a custom filter that implements validation much the same as MinimalApi.Extensions by calling on the methods of DataAnnotations.Validate.

// ** Minimal APIs doesn't include a built-in solution that automatically reads the metadata added by the DataAnnotations attributes and uses it to validate the contents of properties. In this project, I'm using the MinimalApi.Extensions package and its WithParameterValidation() for this purpose; this method creates an endpoint filter that reads the metadata and performs validation, using a library called MiniValidator, which itself seems to rely on the underlying DataAnnotations.Validator. Is it DataAnnotations.Validator that defines that only annotations applied to properties will be examined, and not those applied to containing type itself? Would another validator implementation not have this limitation and thus be able to inspect attributes applied directly to endpoint parameters and validate the contents of those parameters?

// Attributes in the DataAnnotations namespace apply metadata to properties of a type that can subsequently be used to validate the value it contains.
// These attributes can specify whether a value is required, it's minimum and maximum length, its range, and whether it is formatted correctly (e.g., whether it's a correctly formed email address or phone number), and more.
// By themselves, these attributes provide only metadata, and this metadata must be manually read and used in a validation process by some other code.
internal record UserModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public required string FirstName { get; init; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public required string LastName { get; init; }

    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Phone]
    public string? PhoneNumber { get; set; }
}

// DataAnnotations attributes are primarily useful for validating the contents of individual properties discretely (with the possible exception of the [Compare] attribute).
// For more complex validation that can access and compare one or more properties simultaneously and perform complex processing (complex customer may also be achievable with a custom attribute), types can implement IValidatableObject, which includes the Validate method.
// During validation, this method will be executed if each property first satisfies its individual validation attributes (***), e.g., a value is present if [Required] and is a correctly formatted [EmailAddress]. If any property fails its individual validation, IValidatableObject.Validate will never be called.
internal record CreateUserModel : IValidatableObject
{
    [EmailAddress]
    public string? Email { get; init; }

    [Phone]
    public string? PhoneNumber { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(PhoneNumber))
        {
            yield return new ValidationResult("You must provide an email address or a phone number.",
                                              [nameof(Email), nameof(PhoneNumber)]);
        }
    }
}

// *** As per **, could another validator, e.g., from a different package, decide to call IValidatableObject.Validate earlier in the process? The book describes this order of validation -- first individual properties, then Validate() -- before describing that that validator is external code that must be brought in. Is it a hard and fast rule that this order must be applied, or could a another validator do it differently? Or is it that all external validators ultimately depend on DataAnnotations.Validate and it is this class that determines this order of operations, and that is why the book described this behaviour before introducing the need for a validator?

internal record UserIdModel
{
    [FromRoute(Name = "id")]
    [Range(1, 100)]
    public required int Id { get; init; }
}