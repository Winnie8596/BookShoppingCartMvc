using Microsoft.AspNetCore.Identity;
using BookShoppingCartMvcUI.Shared;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Data;

public class DbSeeder
{
    public static async Task SeedDefaultData(IServiceProvider service)
    {
        var logger = service.GetRequiredService<ILogger<DbSeeder>>();
        try
        {
            var context = service.GetRequiredService<ApplicationDbContext>();
            var config = service.GetRequiredService<IConfiguration>();

            // apply pending migrations, unless the deploy already did it with --migrate
            var migrateOnStartup = config.GetValue("Database:MigrateOnStartup", true);
            if (migrateOnStartup && (await context.Database.GetPendingMigrationsAsync()).Any())
            {
                await context.Database.MigrateAsync();
            }

            var userMgr = service.GetRequiredService<UserManager<IdentityUser>>();
            var roleMgr = service.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in new[] { Roles.Admin.ToString(), Roles.User.ToString() })
            {
                if (!await roleMgr.RoleExistsAsync(role))
                {
                    EnsureSucceeded(await roleMgr.CreateAsync(new IdentityRole(role)), $"create the {role} role");
                }
            }

            await SeedAdminAsync(userMgr, config);
            await SeedGenresAsync(context);

            if (!await context.Books.AnyAsync())
            {
                await SeedBooksAsync(context);
            }

            if (!await context.OrderStatuses.AnyAsync())
            {
                await SeedOrderStatusAsync(context);
            }
        }
        catch (Exception ex)
        {
            // don't start the site on a half seeded database
            logger.LogError(ex, "Seeding the database failed");
            throw;
        }
    }

    #region private methods

    // Seed:AdminEmail is in appsettings.json. Seed:AdminPassword comes from appsettings.Development.json locally
    // and from the environment (or appsettings.Production.json) in production
    private static async Task SeedAdminAsync(UserManager<IdentityUser> userMgr, IConfiguration config)
    {
        var email = config["Seed:AdminEmail"];
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Seed:AdminEmail is not set.");
        }

        if (await userMgr.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("Seed:AdminPassword is not set, it's needed to create the admin account.");
        }

        var admin = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        EnsureSucceeded(await userMgr.CreateAsync(admin, password), "create the admin user");
        EnsureSucceeded(await userMgr.AddToRoleAsync(admin, Roles.Admin.ToString()), "add the admin user to the Admin role");
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Couldn't {action}: {errors}");
        }
    }

    private static readonly string[] GenreNames = { "Romance", "Action", "Thriller", "Crime", "SelfHelp", "Programming" };

    // only adds the genres that are missing, so it's safe on an existing database
    private static async Task SeedGenresAsync(ApplicationDbContext context)
    {
        var existing = await context.Genres.Select(g => g.GenreName).ToListAsync();
        var missing = GenreNames.Except(existing, StringComparer.OrdinalIgnoreCase).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        context.Genres.AddRange(missing.Select(name => new Genre { GenreName = name }));
        await context.SaveChangesAsync();
    }

    private static async Task SeedOrderStatusAsync(ApplicationDbContext context)
    {
        var orderStatuses = OrderWorkflow.Statuses.Select(name => new OrderStatus { StatusName = name });

        context.OrderStatuses.AddRange(orderStatuses);
        await context.SaveChangesAsync();
    }

    private static async Task SeedBooksAsync(ApplicationDbContext context)
    {
        // genre ids depend on insert order, so look them up by name
        var genreIds = await context.Genres.ToDictionaryAsync(g => g.GenreName, g => g.Id, StringComparer.OrdinalIgnoreCase);

        var books = new (string Genre, string Title, string Author, decimal Price)[]
        {
            ("Romance", "Pride and Prejudice", "Jane Austen", 12.99m),
            ("Romance", "The Notebook", "Nicholas Sparks", 11.99m),
            ("Romance", "Outlander", "Diana Gabaldon", 14.99m),
            ("Romance", "Me Before You", "Jojo Moyes", 10.99m),
            ("Romance", "The Fault in Our Stars", "John Green", 9.99m),

            ("Action", "The Bourne Identity", "Robert Ludlum", 14.99m),
            ("Action", "Die Hard", "Roderick Thorp", 13.99m),
            ("Action", "Jurassic Park", "Michael Crichton", 15.99m),
            ("Action", "The Da Vinci Code", "Dan Brown", 12.99m),
            ("Action", "The Hunger Games", "Suzanne Collins", 11.99m),

            ("Thriller", "Gone Girl", "Gillian Flynn", 11.99m),
            ("Thriller", "The Girl with the Dragon Tattoo", "Stieg Larsson", 10.99m),
            ("Thriller", "The Silence of the Lambs", "Thomas Harris", 12.99m),
            ("Thriller", "Before I Go to Sleep", "S.J. Watson", 9.99m),
            ("Thriller", "The Girl on the Train", "Paula Hawkins", 13.99m),

            ("Crime", "The Godfather", "Mario Puzo", 13.99m),
            ("Crime", "The Girl with the Dragon Tattoo", "Stieg Larsson", 12.99m),
            ("Crime", "The Cuckoo's Calling", "Robert Galbraith", 14.99m),
            ("Crime", "In Cold Blood", "Truman Capote", 11.99m),
            ("Crime", "The Silence of the Lambs", "Thomas Harris", 15.99m),

            ("SelfHelp", "The 7 Habits of Highly Effective People", "Stephen R. Covey", 9.99m),
            ("SelfHelp", "How to Win Friends and Influence People", "Dale Carnegie", 8.99m),
            ("SelfHelp", "Atomic Habits", "James Clear", 10.99m),
            ("SelfHelp", "The Subtle Art of Not Giving a F*ck", "Mark Manson", 7.99m),
            ("SelfHelp", "You Are a Badass", "Jen Sincero", 11.99m),

            ("Programming", "Clean Code", "Robert C. Martin", 19.99m),
            ("Programming", "Design Patterns", "Erich Gamma", 17.99m),
            ("Programming", "Code Complete", "Steve McConnell", 21.99m),
            ("Programming", "The Pragmatic Programmer", "Andrew Hunt", 18.99m),
            ("Programming", "Head First Design Patterns", "Eric Freeman", 20.99m)
        };

        // every seeded book starts with 10 in stock
        context.Books.AddRange(books.Select(b => new Book
        {
            BookName = b.Title,
            AuthorName = b.Author,
            Price = b.Price,
            GenreId = genreIds[b.Genre],
            Stock = new Stock { Quantity = 10 }
        }));
        await context.SaveChangesAsync();
    }

    #endregion
}
