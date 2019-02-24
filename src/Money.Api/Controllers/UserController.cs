﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Money.Models;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Money.Controllers
{
    [Route("api/[controller]/[action]")]
    public class UserController : ControllerBase
    {
        private readonly JwtOptions configuration;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly JwtSecurityTokenHandler tokenHandler;

        public UserController(IOptions<JwtOptions> configuration, UserManager<ApplicationUser> userManager, JwtSecurityTokenHandler tokenHandler)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(userManager, "userManager");
            Ensure.NotNull(tokenHandler, "tokenHandler");
            this.configuration = configuration.Value;
            this.userManager = userManager;
            this.tokenHandler = tokenHandler;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            ApplicationUser user = await userManager.FindByNameAsync(model.UserName);
            if (user != null)
            {
                if (await userManager.CheckPasswordAsync(user, model.Password))
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, model.UserName)
                    };

                    var credentials = new SigningCredentials(configuration.GetSecurityKey(), SecurityAlgorithms.HmacSha256);
                    var expiry = DateTime.Now.Add(configuration.GetExpiry());

                    var token = new JwtSecurityToken(
                        configuration.Issuer,
                        configuration.Issuer,
                        claims,
                        expires: expiry,
                        signingCredentials: credentials
                    );

                    var response = new LoginResponse()
                    {
                        Token = tokenHandler.WriteToken(token)
                    };

                    return Ok(response);
                }
            }

            return BadRequest();
        }
    }
}
