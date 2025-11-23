using FestivalTicketsApp.WebUI;
using FestivalTicketsApp.WebUI.Helpers;
using FestivalTicketsApp.WebUI.Options.IdentityServer;

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguration();

builder.Services.AddDbContext(builder.Configuration);

IdentityServerOptions identityServerOptions = builder.Configuration
    .GetSection(IdentityServerOptionsSetup.SectionName)
    .Get<IdentityServerOptions>();
builder.Services.AddExternalAuthentication(identityServerOptions);

builder.Services.AddMvc()
    .AddMvcOptions(options =>
    {
        options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
            _ => "");
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddServices();

builder.Services.AddValidation();



// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (args.Contains("/seed"))
{
    await app.SeedData();
    return;
}

app.Run();
