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

    [Authorize(Roles="Admin")]
    [HttpGet("roles")]
    public async Task<IActionResult> AllRoles() {
        IEnumerable<RoleDTO> roles = (await _roleService.FindAll()).Select(e => new RoleDTO() {
            Id = e.Id,
            Name = e.Name
        });
        return Ok(roles);
    }
}
