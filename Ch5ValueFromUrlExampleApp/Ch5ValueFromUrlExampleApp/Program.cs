var builder = WebApplication.CreateBuilder();
var app = builder.Build();

//app.UseExceptionHandler("/error");

var people = new List<Person>
{
    new("Tom", "Hanks"),
    new("Tim", "Allen"),
    new("Tom", "Hardy"),
    new("Tim", "Waltz"),
    new("John", "Goodman"),
    new("John", "Bosch")
};

app.MapGet("/person/{name}", (string name) =>
{
    //throw new Exception("Test get error");
    return people.FindAll((person) => person.FirstName.Contains(name, StringComparison.CurrentCultureIgnoreCase));
});
app.MapPost("/person/{name}", (string name) =>
{
    //throw new Exception("Test post error");
    people.Add(new(name, string.Empty));
});

app.Map("/error", () =>
{
    return "An error occurred";
});

app.Run();

internal record Person(string FirstName, string LastName);
