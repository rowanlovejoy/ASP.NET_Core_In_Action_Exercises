using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/users", (UserModel user) => user.ToString())
   // ASP.NET doesn't contain a built-in validator for Minimal APIs. Microsoft's expects developers to source a third-party package or create their own.
   // A member of ASP.NET team published the MinimalApi.Extensions package (apparently unofficially), which contains the WithParameterValidation() extension method.
   // This method adds an endpoint filter that implements validation referencing the DataAnnotations attributes applied to the members of the endpoint handler parameters.
   .WithParameterValidation();

app.MapPost("/users/create", (CreateUserModel user) => user.ToString())
   .WithParameterValidation();

// Simple types declared directly in the endpoint handler signature cannot use DataAnnotations attributes for validation; the validator (*) looks specifically at the properties of types and whether they have DataAnnotions attributes, not the containing type instance itself.
// One solution to this limitation is to use the [AsParameters] attribute and wrap the simple type to validated in a container; it will then be a property of its containing type and DataAnnotations attributes attached to it will be considered during validation.
app.MapPost("/users/{id}", ([AsParameters] UserIdModel userId) => userId.ToString())
   .WithParameterValidation();

app.Run();

// * By default, the framework doesn't include a validator, code that reads the metadata added by the DataAnnoations attributes and uses it to validate the contents of properties. In this project, I'm using the MinimalApi.Extensions package and its WithParameterValidation() for this purpose. Would another validator implementation not have this limitation and thus be able to inspect attributes applied directly to endpoint parameters and validate the contents of those parameters?

// Attributes in the DataAnnotations namespace apply metadata to properties of a type than subsequently be used to validate the value it contains
// These attributes can specify whether a value is required, it's minimum and maximum length, its range, and whether it is formatted correctly (e.g., whether it's a correctly formed email address or phone number), and more.
// By themselves, these attributes only provide metadata, and this metadata must be manually read and used in a validation process.
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

// DataAnnotations attributes are primarily useful for validating the contents of individual properties discretely (for one exception, see the [Compare] attribute).
// For more complex validation that can access and compare one or more properties simultaneously and perform complex processing (this may also be achievable with a custom attribute), types can implement IValidatableObject, which includes the Validate method.
// During validation, this method will be executed if each property first satisfies its individual validation attributes**, e.g., a value is present if [Required] and is a correctly formatted [EmailAddress]. If any property fails its individual validation, IValidatableObject.Validate will never be called.
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

// ** Since the validation implementation is coming from this third-party package, could another validator, e.g., from a different package, decide to call IValidatableObject.Validate earlier in the process? The book describes this order of validation -- first individual properties, then Validate() -- before describing that that validator is external code that must be brought in. Is it a hard and fast rule that this order must be applied, or could a difgerent validator do it differently?

internal record UserIdModel
{
    [FromQuery(Name = "id")]
    [Range(1, 100)]
    public required int Id { get; init; }
}