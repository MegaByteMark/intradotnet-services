# IntraDotNet.Services

A convenience package for service layer abstractions to reduce boilerplate code in .NET applications. This package provides base classes and interfaces for implementing CRUD operations with optional validation support.

## Features

- **Base Service Abstractions**: Common CRUD operations interface and implementation
- **Validation Support**: Optional validation layer for entities that require business rule validation
- **Result Pattern Integration**: Built on top of [`IntraDotNet.Results`](https://www.nuget.org/packages/IntraDotNet.Results/) for robust error handling
- **Generic Implementation**: Works with any entity type
- **Async/Await Support**: Fully asynchronous operations

## Installation

```bash
dotnet add package IntraDotNet.Services
```

## Package Dependencies

- [`IntraDotNet.Results`](https://www.nuget.org/packages/IntraDotNet.Results/) (>= 1.0.0)
- .NET 9.0+

## Core Interfaces and Classes

### IBaseService<TEntity>

The core interface defining CRUD operations for any entity type.

```csharp
public interface IBaseService<TEntity> where TEntity : class
{
    Task<ValueResult<TEntity>> CreateAsync(TEntity entity);
    Task<ValueResult<TEntity>> UpdateAsync(TEntity entity);
    Task<ValueResult<TEntity>> DeleteAsync(TEntity entity);
    Task<ValueResult<IEnumerable<TEntity>>> GetAllAsync();
    Task<ValueResult<IEnumerable<TEntity>>> FindAsync(Func<TEntity, bool> predicate);
}
```

### BaseService<TEntity>

Abstract base class implementing `IBaseService<TEntity>` that you can inherit from to reduce boilerplate.

### IBaseValidatableService<TEntity>

Extends `IBaseService<TEntity>` to include validation capabilities.

```csharp
public interface IBaseValidatableService<TEntity> : IBaseService<TEntity> where TEntity : class
{
    Task<ValueResult<bool>> ValidateAsync(TEntity entity);
}
```

### BaseValidatableService<TEntity>

Abstract base class that automatically validates entities before create/update operations.

## Usage Examples

### Basic Service Implementation

Here's an example of implementing a simple product service using `BaseService<TEntity>`:

```csharp
using IntraDotNet.Services;
using IntraDotNet.Results;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}

public class ProductService : BaseService<Product>
{
    private readonly List<Product> _products = new();
    private int _nextId = 1;

    public override async Task<ValueResult<Product>> CreateAsync(Product entity)
    {
        try
        {
            entity.Id = _nextId++;
            _products.Add(entity);
            return ValueResult<Product>.Success(entity);
        }
        catch (Exception ex)
        {
            return ValueResult<Product>.Failure($"Failed to create product: {ex.Message}");
        }
    }

    public override async Task<ValueResult<Product>> UpdateAsync(Product entity)
    {
        try
        {
            var existingProduct = _products.FirstOrDefault(p => p.Id == entity.Id);
            if (existingProduct == null)
            {
                return ValueResult<Product>.Failure("Product not found");
            }

            existingProduct.Name = entity.Name;
            existingProduct.Price = entity.Price;
            existingProduct.StockQuantity = entity.StockQuantity;

            return ValueResult<Product>.Success(existingProduct);
        }
        catch (Exception ex)
        {
            return ValueResult<Product>.Failure($"Failed to update product: {ex.Message}");
        }
    }

    public override async Task<ValueResult<Product>> DeleteAsync(Product entity)
    {
        try
        {
            var existingProduct = _products.FirstOrDefault(p => p.Id == entity.Id);
            if (existingProduct == null)
            {
                return ValueResult<Product>.Failure("Product not found");
            }

            _products.Remove(existingProduct);
            return ValueResult<Product>.Success(existingProduct);
        }
        catch (Exception ex)
        {
            return ValueResult<Product>.Failure($"Failed to delete product: {ex.Message}");
        }
    }

    public override async Task<ValueResult<IEnumerable<Product>>> GetAllAsync()
    {
        try
        {
            return ValueResult<IEnumerable<Product>>.Success(_products);
        }
        catch (Exception ex)
        {
            return ValueResult<IEnumerable<Product>>.Failure($"Failed to retrieve products: {ex.Message}");
        }
    }

    public override async Task<ValueResult<IEnumerable<Product>>> FindAsync(Func<Product, bool> predicate)
    {
        try
        {
            var results = _products.Where(predicate);
            return ValueResult<IEnumerable<Product>>.Success(results);
        }
        catch (Exception ex)
        {
            return ValueResult<IEnumerable<Product>>.Failure($"Failed to find products: {ex.Message}");
        }
    }
}
```

### Service with Validation

Here's an example using `BaseValidatableService<TEntity>` for entities that require validation:

```csharp
using IntraDotNet.Services;
using IntraDotNet.Results;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
}

public class UserService : BaseValidatableService<User>
{
    private readonly List<User> _users = new();
    private int _nextId = 1;

    public override async Task<ValueResult<bool>> ValidateAsync(User entity)
    {
        var errors = new List<string>();

        // Email validation
        if (string.IsNullOrWhiteSpace(entity.Email))
        {
            errors.Add("Email is required");
        }
        else if (!IsValidEmail(entity.Email))
        {
            errors.Add("Email format is invalid");
        }
        else if (_users.Any(u => u.Email == entity.Email && u.Id != entity.Id))
        {
            errors.Add("Email already exists");
        }

        // Name validation
        if (string.IsNullOrWhiteSpace(entity.FirstName))
            errors.Add("First name is required");

        if (string.IsNullOrWhiteSpace(entity.LastName))
            errors.Add("Last name is required");

        // Age validation
        var age = DateTime.Now.Year - entity.DateOfBirth.Year;
        if (age < 18)
            errors.Add("User must be at least 18 years old");

        if (errors.Any())
        {
            return ValueResult<bool>.Failure(errors);
        }

        return ValueResult<bool>.Success(true);
    }

    protected override async Task<ValueResult<User>> CreateInternalAsync(User entity)
    {
        try
        {
            entity.Id = _nextId++;
            _users.Add(entity);
            return ValueResult<User>.Success(entity);
        }
        catch (Exception ex)
        {
            return ValueResult<User>.Failure($"Failed to create user: {ex.Message}");
        }
    }

    protected override async Task<ValueResult<User>> UpdateInternalAsync(User entity)
    {
        try
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == entity.Id);
            if (existingUser == null)
            {
                return ValueResult<User>.Failure("User not found");
            }

            existingUser.Email = entity.Email;
            existingUser.FirstName = entity.FirstName;
            existingUser.LastName = entity.LastName;
            existingUser.DateOfBirth = entity.DateOfBirth;

            return ValueResult<User>.Success(existingUser);
        }
        catch (Exception ex)
        {
            return ValueResult<User>.Failure($"Failed to update user: {ex.Message}");
        }
    }

    public override async Task<ValueResult<User>> DeleteAsync(User entity)
    {
        try
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == entity.Id);
            if (existingUser == null)
            {
                return ValueResult<User>.Failure("User not found");
            }

            _users.Remove(existingUser);
            return ValueResult<User>.Success(existingUser);
        }
        catch (Exception ex)
        {
            return ValueResult<User>.Failure($"Failed to delete user: {ex.Message}");
        }
    }

    public override async Task<ValueResult<IEnumerable<User>>> GetAllAsync()
    {
        try
        {
            return ValueResult<IEnumerable<User>>.Success(_users);
        }
        catch (Exception ex)
        {
            return ValueResult<IEnumerable<User>>.Failure($"Failed to retrieve users: {ex.Message}");
        }
    }

    public override async Task<ValueResult<IEnumerable<User>>> FindAsync(Func<User, bool> predicate)
    {
        try
        {
            var results = _users.Where(predicate);
            return ValueResult<IEnumerable<User>>.Success(results);
        }
        catch (Exception ex)
        {
            return ValueResult<IEnumerable<User>>.Failure($"Failed to find users: {ex.Message}");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

### Using the Services

Here's how you would use these services in your application:

```csharp
// Basic service usage
var productService = new ProductService();

var newProduct = new Product
{
    Name = "Laptop",
    Price = 999.99m,
    StockQuantity = 10
};

var createResult = await productService.CreateAsync(newProduct);
if (createResult.IsSuccess)
{
    Console.WriteLine($"Created product with ID: {createResult.Value!.Id}");
}
else
{
    Console.WriteLine($"Failed to create product: {createResult.ErrorMessage}");
}

// Service with validation usage
var userService = new UserService();

var newUser = new User
{
    Email = "john.doe@example.com",
    FirstName = "John",
    LastName = "Doe",
    DateOfBirth = new DateTime(1990, 1, 1)
};

var userCreateResult = await userService.CreateAsync(newUser);
if (userCreateResult.IsSuccess)
{
    Console.WriteLine($"Created user with ID: {userCreateResult.Value!.Id}");
}
else
{
    Console.WriteLine($"Validation failed: {userCreateResult.ErrorMessage}");
    // If multiple validation errors exist:
    if (userCreateResult.AggregateErrors != null)
    {
        foreach (var error in userCreateResult.AggregateErrors)
        {
            Console.WriteLine($"- {error}");
        }
    }
}

// Finding entities
var expensiveProducts = await productService.FindAsync(p => p.Price > 500);
if (expensiveProducts.IsSuccess)
{
    foreach (var product in expensiveProducts.Value!)
    {
        Console.WriteLine($"{product.Name}: ${product.Price}");
    }
}
```

## Entity Framework Integration

You can easily integrate these services with Entity Framework:

```csharp
public class ProductService : BaseValidatableService<Product>
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    protected override async Task<ValueResult<Product>> CreateInternalAsync(Product entity)
    {
        try
        {
            _context.Products.Add(entity);
            await _context.SaveChangesAsync();
            return ValueResult<Product>.Success(entity);
        }
        catch (Exception ex)
        {
            return ValueResult<Product>.Failure($"Failed to create product: {ex.Message}");
        }
    }

    public override async Task<ValueResult<IEnumerable<Product>>> GetAllAsync()
    {
        try
        {
            var products = await _context.Products.ToListAsync();
            return ValueResult<IEnumerable<Product>>.Success(products);
        }
        catch (Exception ex)
        {
            return ValueResult<IEnumerable<Product>>.Failure($"Failed to retrieve products: {ex.Message}");
        }
    }

    // ... other implementations
}
```

## Dependency Injection Setup

Register your services in your DI container:

```csharp
// Program.cs (or Startup.cs)
builder.Services.AddScoped<IBaseService<Product>, ProductService>();
builder.Services.AddScoped<IBaseValidatableService<User>, UserService>();
```

## Key Benefits

1. **Reduced Boilerplate**: Common CRUD operations are abstracted away
2. **Consistent Error Handling**: All operations return `ValueResult<T>` for uniform error handling
3. **Validation Integration**: Automatic validation before create/update operations
4. **Flexible**: Can be used with any data store (Entity Framework, Dapper, in-memory, etc.)
5. **Testable**: Easy to mock and unit test

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Repository

[https://github.com/MegaByteMark/intradotnet-services](https://github.com/MegaByteMark/intradotnet-services)