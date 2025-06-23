using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity
builder.Services.AddDefaultIdentity<User>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// JWT Configuration
var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast256BitsLong!";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; 
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false, 
            ValidateAudience = false, 
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });


// Add API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger with detailed configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Recipe Sharing Platform API",
        Version = "v1",
        Description = "A comprehensive API for managing recipes, users, favorites, and ratings"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Format: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Handle potential enum issues
    c.UseInlineDefinitionsForEnums();

    // Support for file uploads
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseMigrationsEndPoint();

    // Enable Swagger
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Recipe Sharing Platform API V1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Recipe Sharing Platform API";
        c.DisplayRequestDuration();
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map both Razor Pages and API Controllers
app.MapRazorPages();
app.MapControllers();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedData.Initialize(services, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

app.Run();