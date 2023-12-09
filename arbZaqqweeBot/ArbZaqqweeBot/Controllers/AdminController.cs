using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArbZaqqweeBot.Context;
using ArbZaqqweeBot.Data;
using Microsoft.AspNetCore.Identity;
using ArbZaqqweeBot.Dto;
using ArbZaqqweeBot.Helpers;
using ArbZaqqweeBot.Views;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ArbZaqqweeBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AdminController(UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager, SignInManager<IdentityUser> signInManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpPost("register")]
        public async Task Register([FromBody] RegisterViewModel model)
        {
            await _roleManager.CreateAsync(new IdentityRole("Admin"));

            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            if (!admins.Any()){
                var existingEmail = await _userManager.FindByEmailAsync(model.Email);

                if (existingEmail != null)
                {
                    Response.ContentType = "application/json";
                    Response.StatusCode = 409;
                    await Response.WriteAsync("Пользователь с данной эл.почтой зарегистрирован");
                    return;
                }

                var user = new IdentityUser
                {
                    Email = model.Email,
                    UserName = model.Username
                };

                await _userManager.CreateAsync(user, model.Password);
                var person = new User
                {
                    IdentityUser = user
                };
                await _context.Users.AddAsync(person);
                await _context.SaveChangesAsync(default);
                await _userManager.AddToRoleAsync(user, "Admin");
                await Token(model.Username);
            }
            else
            {
                Response.ContentType = "application/json";
                Response.StatusCode = 409;
                await Response.WriteAsync("Администратор уже зарегистрирован");
                return;
            }
        }

        [HttpPost("login")]
        public async Task Login([FromBody] LoginViewModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user != null)
            {
                var userIsAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                if (userIsAdmin)
                {
                    var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, false, false);

                    if (!result.Succeeded)
                    {
                        Response.StatusCode = 401;
                        Response.ContentType = "application/json";
                        await Response.WriteAsync("Неправильное имя пользователя или пароль");
                        return;
                    }

                    await Token(model.Username);
                }
                else
                {
                    Response.StatusCode = 401;
                    Response.ContentType = "application/json";
                    await Response.WriteAsync("Пользователь не является Администратором");
                    return;
                }
            }
        }

        [Authorize]
        [HttpGet("current")]
        public async Task<UserDto> CurrentUser()
        {
            var user = await _context.Users
                .Include(x => x.IdentityUser)
                .SingleOrDefaultAsync(x => x.IdentityUser.Email == User.ToUserInfo().Username ||
                    x.IdentityUser.UserName == User.ToUserInfo().Username);
            return Mapper.Map<User, UserDto>(user);
        }

        [Authorize]
        [HttpGet("getExchangers")]
        public async Task<List<ExchangerDto>> GetExchangers()
        {
            var exchangers = await _context.Exchangers.ToListAsync();

            return Mapper.Map<List<Exchanger>, List<ExchangerDto>>(exchangers);
        }

        private async Task Token(string email)
        {
            var identity = GetIdentity(email);
            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                notBefore: now,
                claims: identity.Claims,
                expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new
            {
                access_token = encodedJwt,
                username = identity.Name,
            };

            Response.ContentType = "application/json";
            await Response.WriteAsync(JsonConvert.SerializeObject(response, new JsonSerializerSettings { Formatting = Formatting.Indented }));
        }


        private static ClaimsIdentity GetIdentity(string login)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, login),
                new Claim(ClaimTypes.Role, "Admin"),
            };

            ClaimsIdentity claimIdentity = new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);

            return claimIdentity;
        }
    }
}
