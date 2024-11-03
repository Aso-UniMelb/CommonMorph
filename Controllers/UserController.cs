using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using CommonMorphAPI.Model;
using static CommonMorphAPI.Model._DbContext;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace CommonMorphAPI.Controllers
{
  public class UserController(IConfiguration config) : Controller
  {
    private readonly IConfiguration _config = config;

    private string GenerateJwtToken(ClaimsIdentity identity)
    {
      var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_config["auth:jwt:secret"]));
      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Subject = identity,
        Expires = DateTime.Now.AddDays(30),
        Issuer = _config["auth:jwt:issuer"],
        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
      };
      var tokenHandler = new JwtSecurityTokenHandler();
      var token = tokenHandler.CreateToken(tokenDescriptor);
      return tokenHandler.WriteToken(token);
    }

    // [HttpPost]
    // public async Task<IActionResult> Login([FromBody] JsonElement data)
    // {
    //   // TODO: CHECK IF USER AND PASSWORD ARE CORRECT

    //   var claims = new List<Claim> {
    //     new("Id", id.ToString(), ClaimValueTypes.UInteger32),
    //     new("Email", email),
    //   };
    //   claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));
    //   var identity = new ClaimsIdentity(claims);
    //   var principal = new ClaimsPrincipal(identity);
    //   return Json(new
    //   {
    //     token = GenerateJwtToken(identity)
    //   });
    // }
  }
}