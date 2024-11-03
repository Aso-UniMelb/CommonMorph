using Microsoft.EntityFrameworkCore;
using CommonMorphAPI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);
_M.ConStr = "Server=kurdinus.com\\MSSQLSERVER2017;Database=CommonMorph;User Id=CommonMorph;Password=pU5b4k3#5;TrustServerCertificate=True;";
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
// .AddJwtBearer(options =>
// {
// 	options.TokenValidationParameters = new TokenValidationParameters
// 	{
// 		ValidateIssuer = true,
// 		ValidateAudience = false,
// 		ValidateLifetime = true,
// 		ValidateIssuerSigningKey = true,
// 		ValidIssuer = builder.Configuration["auth:jwt:issuer"],
// 		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["auth:jwt:secret"]))
// 	};
// });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors(builder => builder
   .AllowAnyOrigin()
   .AllowAnyMethod()
   .AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();