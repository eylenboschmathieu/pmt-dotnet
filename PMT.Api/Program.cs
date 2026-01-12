using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using PMT.Data;
using PMT.Data.Repositories;
using PMT.Services;
using PMT.Api.HostedServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

    // Scheduling
builder.Services.AddHostedService<WeeklyRefreshTokenCleanup>();
builder.Services.AddHostedService<MonthlyScheduleSeeder>();

    // Services
builder.Services.AddTransient<RoleService>();
builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<TokenService>();
builder.Services.AddTransient<ShiftService>();

    // Repositories
builder.Services.AddTransient<IRoleRepository, RoleRepository>();
builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddTransient<ITokenRepository, TokenRepository>();
builder.Services.AddTransient<IUserShiftRepository, UserShiftRepository>();

// === Swagger === //
#if DEBUG
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[]{}
        }
    });
});
#endif

// === Database === //
/*string connectionString = builder.Configuration.GetConnectionString("PMT_DB_CONNECTION")
    ?? throw new InvalidOperationException("Connection string not found");*/

var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "app.db");

builder.Services.AddDbContext<ApplicationDbContext>(options => {
    options.UseSqlite($"Data Source={dbPath}");
    /*options.UseSqlServer(connectionString, sql => {
        sql.MaxBatchSize(150);
        sql.MigrationsAssembly("PMT.Data");
    });*/
});

// === Authentication === //
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["App:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["App:Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["App:Secret"]!)),

            // Change the internal structure of dotnet claims a little to work with Jwt and our own names
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = "Roles",
        };
    });

builder.Services.AddAuthorization(options => {
    options.AddPolicy("CanModify", policy => policy.RequireAssertion(context => {
        // Make it so only admin, management, and the user themselves can modify a users' data
        string myId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
        var routeUserId = context.Resource is HttpContext httpContext ? httpContext.Request.RouteValues["userId"]?.ToString() : null;
        return context.User.IsInRole("Admin") || context.User.IsInRole("Management") || routeUserId == myId;
    }));
});

// === CORS === // (Get chatty with angular)
builder.Services.AddCors(options => {
options.AddPolicy("AllowAngular", policy =>
    policy.WithOrigins(builder.Configuration["App:CORS:Origin"]!)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

//==============//

builder.Services.AddControllers();

var app = builder.Build();

// Migrate db
using (var scope = app.Services.CreateScope()) {
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();

    db.Database.Migrate();
}

// Create admin account from user secrets
using (var scope = app.Services.CreateScope()) {
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var adminEmail = config["App:AdminEmail"];

    if (!string.IsNullOrWhiteSpace(adminEmail)) {
        var userService = scope.ServiceProvider.GetRequiredService<UserService>();
        var roleService = scope.ServiceProvider.GetRequiredService<RoleService>();

        var user = await userService.FindByEmail(adminEmail);
        if (user == null) {
            // These roles are mapped to id's 1 and 2
            var adminRole = await roleService.FindById(1);
            var managementRole = await roleService.FindById(2);

            user = new PMT.Data.Entities.User {
                Email = adminEmail,
                Active = true,
                Roles = [adminRole!, managementRole!]
            };

            await userService.Create(user);
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
