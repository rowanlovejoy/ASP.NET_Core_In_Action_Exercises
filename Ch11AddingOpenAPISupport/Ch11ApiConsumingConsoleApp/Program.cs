using Fruit;

IFruitClient fruitClient = new FruitClient(new HttpClient { BaseAddress = new Uri("https://localhost:7203") });

Fruit.Fruit createdFruit = await fruitClient.CreateFruitAsync("13", new Fruit.Fruit { Name = "Apple", Stock = 20 });
Console.WriteLine($"Created fruit {createdFruit.Name} with stock {createdFruit.Stock}");

Fruit.Fruit retrievedFruit = await fruitClient.GetFruitAsync("13");
Console.WriteLine($"Retrieved fruit {retrievedFruit.Name} with stock {createdFruit.Stock}");

Fruit.Fruit updatedFruit = await fruitClient.UpdateFruitAsync("13", new Fruit.Fruit { Name = "Apple", Stock = 15 });
Console.WriteLine($"Updated fruit with ID 13: Name {updatedFruit.Name} - Stock: {updatedFruit.Stock}");
