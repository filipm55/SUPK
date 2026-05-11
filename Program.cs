using Microsoft.EntityFrameworkCore;
//using Npgsql;
using SUPK.Models;

var builder = WebApplication.CreateBuilder(args);

//var dataSourceBuilder = new NpgsqlDataSourceBuilder(
//    builder.Configuration.GetConnectionString("DefaultConnection"));

//dataSourceBuilder.EnableUnmappedTypes();
//dataSourceBuilder.MapEnum<TipPlacanja>("tip_placanja");

//var dataSource = dataSourceBuilder.Build();

//builder.Services.AddControllersWithViews();

//builder.Services.AddDbContext<CaffeBarDbContext>(options =>
//    options.UseNpgsql(dataSource));

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<CaffeBarDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();