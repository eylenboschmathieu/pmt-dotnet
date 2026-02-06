using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PMT.Services;

namespace PMT.Api.Controllers;

public class RoleDTO {
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

[ApiController]
public class RolesController(RoleService _roleService) : ControllerBase {

    [Authorize(Roles="Admin, Management")]
    [HttpGet("roles")]
    public async Task<IActionResult> AllRoles() {
        if (int.TryParse(HttpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub), out int myId)) {
            var roles = (await _roleService.FindByUser(myId)).Select(e => e.Id);
            
            IEnumerable<RoleDTO> children = (await _roleService.FindChildRoles(roles)).Select(e => new RoleDTO {
                Id = e.Id,
                Name = e.Name
            });
            
            return Ok(children);
        }
        
        return StatusCode(StatusCodes.Status500InternalServerError);
    }
}
