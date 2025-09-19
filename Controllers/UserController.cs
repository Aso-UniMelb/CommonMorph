using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;
using NETCore.MailKit.Core;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace common_morph_backend.Controllers
{
  public class UserController : Controller
  {
    private readonly IEmailService _EmailService;
    private readonly AppDbContext _context;
    public UserController(IEmailService emailService, AppDbContext context)
    {
      _EmailService = emailService;
      _context = context;
    }
    //=====================================================================
    // Google Login/Register
    //=====================================================================
    [HttpPost]
    public async Task<IActionResult> GoogleLogin(string ln, string credential)
    {
      var handler = new JwtSecurityTokenHandler();
      var jsonToken = handler.ReadToken(credential);
      var tokenS = jsonToken as JwtSecurityToken;
      var email = tokenS.Claims.First(claim => claim.Type == "email").Value.Trim().ToLower();
      var name = tokenS.Claims.First(claim => claim.Type == "name").Value;

      var checkuser = _context.users.FirstOrDefault(x => x.username == email);
      // if new user, add to db
      if (checkuser == null)
      {
        var newUser = new User
        {
          role = UserRole.speaker,
          username = email,
          name = name,
        };
        _context.users.Add(newUser);
        _context.SaveChanges();
        // email admins to notify
        var admins = _context.users.Where(x => x.role == UserRole.admin).ToList();
        foreach (var admin in admins)
        {
          _EmailService.Send(
            admin.username,
            "New Common Morph User Registration via Google",
            @$"<h2>New User Registration</h2>
            <p><a href=""https://morph.kurdinus.com/app/admin"" style=""background:#1B84FF;color:#fff; padding:5px 10px;border-radius:5px;"">Click here to check this registration</a></p>",
            isHtml: true);
        }
      }

      // generate kookie
      var user = _context.users.FirstOrDefault(x => x.username == email);
      var claims = new List<Claim>
        {
          new Claim(ClaimTypes.Name, email),
          new Claim(ClaimTypes.Role, user.role.ToString()),
          new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
          new Claim(ClaimTypes.GivenName, user.name)
        };
      var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
      var principal = new ClaimsPrincipal(identity);
      var authProperties = new AuthenticationProperties
      {
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
        IsPersistent = true
      };
      HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
      return RedirectToAction("dashboard", "App");
    }

    //=====================================================================
    // Login/logout/register
    //=====================================================================
    [HttpGet]
    public IActionResult Login()
    {
      ViewBag.Error = "";
      return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
      var user = _context.users.FirstOrDefault(x => x.username == username);
      if (user == null || !VerifyPassword(password, user.passwordhash, user.passwordsalt))
      {
        ViewBag.Error = "Invalid username or password.";
        return View();
      }
      var principal = GetUser(username, password);
      var authProperties = new AuthenticationProperties
      {
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
        IsPersistent = true
      };
      HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
      return RedirectToAction("dashboard", "App");
    }

    [Authorize]
    public IActionResult Logout()
    {
      HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
      return RedirectToAction("login", "User");
    }

    [HttpGet]
    public IActionResult Register()
    {
      ViewBag.Error = "";
      return View();
    }
    [HttpPost]
    public IActionResult Register(string username, UserRole desiredRole)
    {
      var usr = _context.users.FirstOrDefault(x => x.username == username);
      if (usr != null)
      {
        ViewBag.Error = "User already exists.";
        return View();
      }
      var random = new Random();
      var code = random.Next(100000, 999999).ToString();

      usr = new User
      {
        username = username,
        registrationcode = code,
        role = UserRole.speaker
      };

      if (desiredRole == UserRole.speaker)
        usr.desiredrole = desiredRole;

      _context.users.Add(usr);
      _context.SaveChanges();
      // email registration code to the user
      _EmailService.Send(
        usr.username,
        "You are invited to the CommonMorph!",
        @$"<h2>You are invited to contribute to the CommonMorph Project!</h2>
          <p><a href=""https://morph.kurdinus.com/user/activate?username={usr.username}&code={code}"" style=""background:#1B84FF;color:#fff; padding:5px 10px;border-radius:5px;"">Click here to start</a></p>",
        isHtml: true);
      ViewBag.Error = "Please check your email for the registration code.";
      // email admins to notify
      var admins = _context.users.Where(x => x.role == UserRole.admin).ToList();
      foreach (var admin in admins)
      {
        _EmailService.Send(
          admin.username,
          "New CommonMorph User Registration via Form",
          @$"<h2>New User Registration</h2>
          <p><a href=""https://morph.kurdinus.com/app/admin"" style=""background:#1B84FF;color:#fff; padding:5px 10px;border-radius:5px;"">Click here to check this registration</a></p>",
          isHtml: true);
      }
      return View();
    }

    [HttpGet]
    public IActionResult Activate(string username, string code)
    {
      ViewBag.Username = username;
      ViewBag.Code = code;
      return View();
    }

    [HttpPost]
    public IActionResult Activate(string username, string code, string password, string name)
    {
      var user = _context.users.FirstOrDefault(x => x.username == username && x.registrationcode == code);
      if (user == null)
      {
        ViewBag.Error = "Invalid username or registration code.";
        return View();
      }
      var salt = GenerateSalt();
      var hashedPassword = HashPassword(password, salt);
      user.name = name;
      user.passwordhash = hashedPassword;
      user.passwordsalt = salt;
      _context.Entry(user).State = EntityState.Detached;
      _context.users.Update(user);
      _context.SaveChanges();

      var principal = GetUser(username, password);
      var authProperties = new AuthenticationProperties
      {
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
        IsPersistent = true
      };
      HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
      return RedirectToAction("dashboard", "App");
    }

    //forget password
    [HttpGet]
    public IActionResult forgot()
    {
      ViewBag.Error = "";
      return View();
    }
    [HttpPost]
    public IActionResult forgot(string username)
    {
      var user = _context.users.FirstOrDefault(x => x.username == username);
      if (user == null)
      {
        ViewBag.Error = "Invalid username.";
        return View();
      }
      // random code between 1000 and 9999
      var random = new Random();
      var code = random.Next(100000, 999999).ToString();
      _EmailService.Send(
        user.username,
        "CommonMorph: Password reset",
        @$"<h2>Please click this link to reset your password in the CommonMorph Project!</h2>
          <p><a href=""https://morph.kurdinus.com/user/activate?username={user.username}&code={code}"" style=""background:#1B84FF;color:#fff; padding:5px 10px;border-radius:5px;"">Click here to start</a></p>",
        isHtml: true);
      user.registrationcode = code;
      _context.Entry(user).State = EntityState.Detached;
      _context.users.Update(user);
      _context.SaveChanges();
      return RedirectToAction("login", "User");
    }

    // ======================================================================
    // API Endpoints
    // ======================================================================

    [Authorize(Roles = "admin")]
    [HttpGet("User/list")]
    public IActionResult list()
    {
      var list = _context.users.Select(x => new { x.id, x.username, x.role, x.name, x.desiredrole })
      .OrderBy(x => x.role).ThenBy(x => x.username).ToList();
      return Ok(list);
    }


    [Authorize(Roles = "admin")]
    [HttpPost("User/invite")]
    public async Task<IActionResult> invite(User usr)
    {
      var checkuser = _context.users.FirstOrDefault(x => x.username == usr.username);
      if (checkuser != null)
        return BadRequest("user already exists");
      // random code between 1000 and 9999
      var random = new Random();
      var code = random.Next(100000, 999999).ToString();
      _EmailService.Send(
        usr.username,
        "You are invited to the CommonMorph!",
        @$"<h2>You are invited to contribute to the CommonMorph Project!</h2>
          <p><a href=""https://morph.kurdinus.com/user/activate?username={usr.username}&code={code}"" style=""background:#1B84FF;color:#fff; padding:5px 10px;border-radius:5px;"">Click here to start</a></p>",
        isHtml: true);
      var user = new User
      {
        role = (UserRole)usr.role,
        username = usr.username,
        registrationcode = code
      };
      _context.users.Add(user);
      _context.SaveChanges();
      return Ok("User created successfully.");
    }

    [Authorize(Roles = "admin")]
    [HttpPost("User/changeRole")]
    public async Task<IActionResult> ChangeRole(int usrId, UserRole newRole)
    {
      var usr = _context.users.FirstOrDefault(x => x.id == usrId);
      if (usr == null)
      {
        return BadRequest("User not found.");
      }
      usr.role = newRole;
      _context.Entry(usr).State = EntityState.Detached;
      _context.users.Update(usr);
      _context.SaveChanges();
      return Ok("User's Role Changed successfully.");
    }

    [Authorize]
    [HttpPost("User/changeMyRole")]
    public async Task<IActionResult> ChangeMyRole(UserRole newRole)
    {
      var userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      var usr = _context.users.FirstOrDefault(x => x.id == userid);
      usr.role = newRole;
      _context.Entry(usr).State = EntityState.Detached;
      _context.users.Update(usr);
      _context.SaveChanges();
      //update cookie
      var claims = new List<Claim>
        {
          new Claim(ClaimTypes.NameIdentifier, userid.ToString()),
          new Claim(ClaimTypes.Role, newRole.ToString()),
          new Claim(ClaimTypes.GivenName, usr.name)
        };
      var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
      var principal = new ClaimsPrincipal(identity);
      var authProperties = new AuthenticationProperties
      {
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
        IsPersistent = true
      };
      HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
      return Ok("User's Role Changed successfully.");
    }

    // ======================================================================
    // Functions
    // ======================================================================
    private string GenerateSalt()
    {
      var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
      var salt = new byte[16];
      rng.GetBytes(salt);
      return Convert.ToBase64String(salt);
    }

    private string HashPassword(string password, string salt)
    {
      using (var sha256 = System.Security.Cryptography.SHA256.Create())
      {
        var thePassword = Encoding.UTF8.GetBytes(password + salt);
        return Convert.ToBase64String(sha256.ComputeHash(thePassword));
      }
    }

    private bool VerifyPassword(string password, string storedHash, string salt)
    {
      var hashedPassword = HashPassword(password, salt);
      return hashedPassword == storedHash;
    }

    private ClaimsPrincipal GetUser(string username, string password)
    {
      if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        return null;
      var user = _context.users.FirstOrDefault(x => x.username == username);
      if (user == null || !VerifyPassword(password, user.passwordhash, user.passwordsalt))
      {
        return null;
      }
      else
      {
        var claims = new List<Claim>
          {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, user.role.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
            new Claim(ClaimTypes.GivenName, user.name)
          };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
      }
    }
  }
}