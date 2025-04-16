var builder = WebApplication.CreateBuilder();
var app = builder.Build();

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
    return people.FindAll((person) => person.FirstName.Contains(name));
});


app.Run();

internal record Person(string FirstName, string LastName);
