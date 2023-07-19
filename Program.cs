using Microsoft.AspNetCore.Identity;
using starter_kit.Data;
using Npgsql;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

//postgres hookup below
var databaseUrl = builder.Configuration.GetValue<string>("DATABASE_URL");
if(!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"))){
   //handle heroku passing it to us live
   databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
}
var databaseUri = new Uri(databaseUrl);
var userInfo = databaseUri.UserInfo.Split(':');

var dbconnection = new NpgsqlConnectionStringBuilder
{
    Host = databaseUri.Host,
    Port = databaseUri.Port,
    Username = userInfo[0],
    Password = userInfo[1],
    Database = databaseUri.LocalPath.TrimStart('/'),
    TrustServerCertificate = true
};

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dbconnection.ToString()));
    builder.Services.AddDataProtection();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddHttpClient();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var userManager = builder.Services.BuildServiceProvider().GetRequiredService<UserManager<IdentityUser>>();
var roleManager = builder.Services.BuildServiceProvider().GetRequiredService<RoleManager<IdentityRole>>();

builder.Services.AddRazorPages(
    options => {
        options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Register");
    }
);

// Add authorization and require on all pages by default
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

using(var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }

// await DefaultRoles.SeedRolesAsync(userManager, roleManager);

app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto
    });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

//the below code is required for heroku hosting
if (app.Environment.IsDevelopment()) {
    app.Run();
}
else {
    var port = Environment.GetEnvironmentVariable("PORT");
    app.Run($"http://*:{port}");
}