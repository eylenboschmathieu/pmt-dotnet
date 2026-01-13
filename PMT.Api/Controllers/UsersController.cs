using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PMT.Data.Entities;
using PMT.Services;

namespace PMT.Api.Controllers;

public class UserCreateDTO {
    public string Email { get; set; } = null!;
    public int[] Roles { get; set; } = [];
}

[ApiController]
public class UsersController(UserService _userService, RoleService _roleService) : ControllerBase {

    [Authorize(Roles = "Admin, Management")]
    [HttpGet("users")]
    public async Task<IActionResult> AllUsers() {
        Console.WriteLine("UsersController.AllUsers()");
        IEnumerable<User> users = await _userService.FindAll();

        return Ok(users.OrderByDescending(e => e.Active).ThenBy(e => string.IsNullOrEmpty(e.Name)).ThenBy(e => e.Name).Select(u => new {
            u.Id,
            u.Name,
            u.Email,
            u.Active,
        }));
    }
    
    [Authorize(Policy = "CanModify")]
    [HttpGet("user")]
    public async Task<IActionResult> GetUserData([FromQuery] int userId) {
        Console.WriteLine($"UsersController.GetUserData({userId})");

        UserDataDTO? user = await _userService.GetUserData(userId);

        return Ok(user);
    }

    [Authorize(Roles = "Admin, Management")]
    [HttpGet("users/select")]
    public async Task<IActionResult> UserSelect() {
        Console.WriteLine("UserController.UserSelect()");
        IEnumerable<User> users = await _userService.FindSelect();

        return Ok(users.Select(e => new UserSelectDTO() {
            Id = e.Id,
            Name = e.Name!,
            Icon = e.Roles.Any(e => e.Name.Equals("Intern")) ? "🎓" : "🚑"
        }).OrderBy(e => e.Name));
    }

    [Authorize(Roles = "Admin, Management")]
    [HttpPost("user/new")]
    public async Task<IActionResult> NewUser([FromBody] UserCreateDTO user) {
        Console.WriteLine("UserController.NewUser");
        List<Role> roles = (await _roleService.FindAll()).Where(r => user.Roles.Contains(r.Id)).ToList();
        
        User? createdBy = await _userService.FindById(int.Parse(HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value));
        User newUser = new() {
            Email = user.Email,
            Roles = roles,
            CreatedBy = createdBy,
            Active = true
        };
        await _userService.Create(newUser);
        return Ok();
    }
    
    [HttpPost("user/demo_new")]
    public async Task<IActionResult> DemoNewUser([FromBody] UserCreateDTO user) {
        Console.WriteLine("UserController.DemoNewUser");
        List<Role> roles = (await _roleService.FindAll()).Where(r => user.Roles.Contains(r.Id)).ToList();
        
        User newUser = new() {
            Email = user.Email,
            Roles = roles,
            CreatedBy = null,
            Active = true
        };

        return Ok(await _userService.Create(newUser) is not null);
    }

    [Authorize(Policy = "CanModify")]
    [HttpPut("user/update")]
    public async Task<IActionResult> UpdateUser([FromQuery] int userId, [FromBody] UpdateUserDTO body) {
        Console.WriteLine($"UserController.UpdateUser({userId}, {body.Id}, {body.Name}, {body.Active}, {body.Roles})");
        if (body.Id != userId) // Make sure nobody tries anything funny
            return BadRequest();
        return Ok(await _userService.Update(body));
    }
}
