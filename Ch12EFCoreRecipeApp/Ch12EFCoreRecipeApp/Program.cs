using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<RecipeService>();

var app = builder.Build();

app.MapGet("/", () => "Recipe App");

app.MapPost("/recipe", async (CreateRecipeCommand createRecipeCommand, RecipeService recipeService) =>
{
    return await recipeService.CreateRecipe(createRecipeCommand);
});

app.MapGet("/recipe", async (RecipeService recipeService) =>
{
    return await recipeService.GetRecipes();
});

app.MapGet("/recipe/{id}", async (int id, RecipeService recipeService) =>
{
    return await recipeService.GetRecipe(id);
});

app.MapPut("/recipe", async (UpdateRecipeCommand updateRecipeCommand, RecipeService recipeService) =>
{
    return await recipeService.UpdateRecipe(updateRecipeCommand);
});

app.MapDelete("/recipe/{id}", async (int id, RecipeService recipeService) =>
{
    return await recipeService.DeleteRecipe(id);
});

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.Run();

internal class RecipeService(AppDbContext context)
{
    private readonly AppDbContext dbContext = context;

    public async Task<int> CreateRecipe(CreateRecipeCommand createRecipeCommand)
    {
        var recipe = new Recipe
        {
            Name = createRecipeCommand.Name,
            TimeToCook = new TimeSpan(createRecipeCommand.TimeToCookHours, createRecipeCommand.TimeToCookMinutes, 0),
            Method = createRecipeCommand.Method,
            IsVegetarian = createRecipeCommand.IsVegetarian,
            IsVegan = createRecipeCommand.IsVegan,
            Ingredients = [.. createRecipeCommand.Ingredients.Select(ingredient => new Recipe.Ingredient
            {
                Name = ingredient.Name,
                Quantity = ingredient.Quantity,
                Unit = ingredient.Unit
            })]
        };

        dbContext.Add(recipe);

        await dbContext.SaveChangesAsync();

        return recipe.RecipeId;
    }

    public async Task<IEnumerable<RecipeSummaryViewModel>> GetRecipes()
    {
        return await dbContext.Recipes
            .Select(recipe => new RecipeSummaryViewModel
            {
                Id = recipe.RecipeId,
                Name = recipe.Name,
                TimeToCook = $"{recipe.TimeToCook.TotalMinutes} minutes"
            })
            .ToListAsync();
    }

    public async Task<RecipeDetailViewModel> GetRecipe(int id)
    {
        return await dbContext.Recipes
            .Where(recipe => recipe.RecipeId == id)
            .Select(recipe => new RecipeDetailViewModel
            {
                Id = recipe.RecipeId,
                Name = recipe.Name,
                Method = recipe.Method,
                Ingredients = recipe.Ingredients
                    .Select(ingredient => new RecipeDetailViewModel.Ingredient(
                        ingredient.Name,
                        ingredient.Quantity,
                        ingredient.Unit
                    ))
            })
            .SingleAsync();
    }

    public async Task<RecipeDetailViewModel> UpdateRecipe(UpdateRecipeCommand updateRecipeCommand)
    {
        var recipe = await dbContext.Recipes
            .Where(recipe => recipe.RecipeId == updateRecipeCommand.Id)
            .Include(recipe => recipe.Ingredients)
            .SingleAsync();

        UpdateRecipe(recipe, updateRecipeCommand);

        await dbContext.SaveChangesAsync();

        static void UpdateRecipe(Recipe recipe, UpdateRecipeCommand updateRecipeCommand)
        {
            recipe.Name = updateRecipeCommand.Name ?? recipe.Name;
            recipe.TimeToCook = updateRecipeCommand.TimeToCookHours is int hours && updateRecipeCommand.TimeToCookMinutes is int minutes
                ? new TimeSpan(hours, minutes, 0)
                : recipe.TimeToCook;
            recipe.Method = updateRecipeCommand.Method ?? recipe.Method;
            recipe.IsVegetarian = updateRecipeCommand.IsVegetarian ?? recipe.IsVegetarian;
            recipe.IsVegan = updateRecipeCommand.IsVegan ?? recipe.IsVegan;
            if (updateRecipeCommand.Ingredients is not null)
            {
                recipe.Ingredients = [.. recipe.Ingredients
                    .Zip(updateRecipeCommand.Ingredients)
                    .Select((pair) =>
                    {
                        var (original, updated) = pair;

                        original.Name = updated.Name ??  original.Name;
                        original.Quantity = updated.Quantity ?? original.Quantity;
                        original.Unit = updated.Unit ?? original.Name;

                        return original;
                    })];
            }
        }

        return new RecipeDetailViewModel
        {
            Id = recipe.RecipeId,
            Name = recipe.Name,
            Method = recipe.Method,
            Ingredients = recipe.Ingredients
                .Select(ingredient => new RecipeDetailViewModel.Ingredient(
                    ingredient.Name,
                    ingredient.Quantity,
                    ingredient.Unit
                ))
        };
    }

    public async Task<IResult> DeleteRecipe(int id)
    {
        var recipe = await dbContext.Recipes.FindAsync(id);
        if (recipe is null)
        {
            return Results.NotFound();
        }

        recipe.IsDeleted = true;

        await dbContext.SaveChangesAsync();

        return Results.Ok();
    }
}

internal class RecipeDetailViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Method { get; set; }

    public required IEnumerable<Ingredient> Ingredients { get; set; }

    internal class Ingredient(string name, decimal quantity, string unit)
    {
        public string Name { get; set; } = name;
        public string Quantity { get; set; } = $"{quantity} {unit}";
    }
}

internal class RecipeSummaryViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string TimeToCook { get; set; }
}

internal class CreateRecipeCommand
{
    public required string Name { get; set; }
    public int TimeToCookHours { get; set; }
    public int TimeToCookMinutes { get; set; }
    public required string Method { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsVegan { get; set; }
    public required IEnumerable<CreateIngredientCommand> Ingredients { get; set; }

    internal class CreateIngredientCommand
    {
        public required string Name { get; set; }
        public int Quantity { get; set; }
        public required string Unit { get; set; }
    }
}

internal class UpdateRecipeCommand
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? TimeToCookHours { get; set; }
    public int? TimeToCookMinutes { get; set; }
    public string? Method { get; set; }
    public bool? IsVegetarian { get; set; }
    public bool? IsVegan { get; set; }
    public IEnumerable<UpdateIngredientCommand>? Ingredients { get; set; }

    internal class UpdateIngredientCommand
    {
        public string? Name { get; set; }
        public int? Quantity { get; set; }
        public string? Unit { get; set; }
    }
}

internal class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Recipe>().HasQueryFilter(recipe => !recipe.IsDeleted);
    }
}

internal class Recipe
{
    public int RecipeId { get; set; }
    public required string Name { get; set; }
    public TimeSpan TimeToCook { get; set; }
    public bool IsDeleted { get; set; }
    public required string Method { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsVegan { get; set; }
    public required ICollection<Ingredient> Ingredients { get; set; }

    internal class Ingredient
    {
        public int IngredientId { get; set; }
        public int RecipeId { get; set; }
        public required string Name { get; set; }
        public decimal Quantity { get; set; }
        public required string Unit { get; set; }
    }
}
