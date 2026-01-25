using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PMT.Services;

namespace PMT.Api.Controllers;

[ApiController]
public class ShiftsController(IAuthorizationService _authorizationService, UserShiftService _shiftService) : ControllerBase {
    
    [HttpGet("shifts")]
    [Authorize]
    public IActionResult GetShiftHours() => Ok(_shiftService.GetShiftHours());

    [HttpGet("requests/dates")]
    [Authorize]
    public async Task<IActionResult> GetRequestedMonths() {
        Console.WriteLine("ShiftsController.GetRequestedMonths()");
        return Ok(await _shiftService.GetRequestedMonths());
    }

    [HttpGet("requests/{userId:int}/{year:int}/{month:int}")]
    [Authorize]
    public async Task<IActionResult> GetUserRequests(int userId, int year, int month) {
        Console.WriteLine($"ShiftsController.GetUserRequests(userId: {userId}, year: {year}, month: {month})");

        AuthorizationResult authorized = await _authorizationService.AuthorizeAsync(User, userId, "CanModify");
        if (!authorized.Succeeded)
            return Forbid();

        if (month < 1 || month > 12)
            return BadRequest(month);
        
        return Ok(await _shiftService.GetUserRequests(userId, year, month));
    }

    [HttpPut("requests/update")]
    [Authorize]
    public async Task<IActionResult> UpdateRequest([FromBody] UpdateRequestDTO body) {
        Console.WriteLine("ShiftController.UpdateRequest()");
        
        AuthorizationResult authorized = await _authorizationService.AuthorizeAsync(User, body.UserId, "CanModify");
        if (!authorized.Succeeded)
            return Forbid();

        return Ok(await _shiftService.UpdateShiftRequest(body));
    }

    [HttpGet("confirmed/{userId:int}/{year:int}/{month:int}")]
    [Authorize]
    public async Task<IActionResult> GetConfirmedShifts(int userId, int year, int month) {
        Console.WriteLine($"ShiftsController.GetConfirmedShifts(userId: {userId}, year: {year}, month: {month})");

        AuthorizationResult authorized = await _authorizationService.AuthorizeAsync(User, userId, "CanModify");
        if (!authorized.Succeeded)
            return Forbid();

        if (month < 1 || month > 12)
            return BadRequest(month);

        return Ok(await _shiftService.GetConfirmedShiftsForUser(userId, year, month));
    }

    [HttpGet("planning/dates")]
    [Authorize(Roles = "Admin, Management")]
    public async Task<IActionResult> GetPlanningMonths() {
        Console.WriteLine("ShiftsController.GetPlanningMonths()");
        return Ok(await _shiftService.GetPlanningMonths());
    }

    [HttpGet("planning/{year:int}/{month:int}")]
    [Authorize(Roles = "Admin, Management")]
    public async Task<IActionResult> GetMonthPlanning(int year, int month) {
        Console.WriteLine($"ShiftsController.GetConfirmedShifts(year: {year}, month: {month})");
        if (month < 1 || month > 12)
            return BadRequest("Bad month " + month);
            
        List<DayPlanningDTO> data = await _shiftService.GetPlanningForMonth(year, month);
        
        return Ok(data);
    }

    [HttpPut("planning/lock")]
    [Authorize(Roles = "Admin, Management")]
    // Lock a months planning
    public async Task<IActionResult> LockMonth([FromBody] LockMonthDTO body) {
        Console.WriteLine($"ShiftsController.LockMonth({body.Date}, {body.Locked})");
        return Ok(await _shiftService.LockMonth(body.Date, body.Locked));
    }

    [HttpPut("planning/update")]
    [Authorize(Roles = "Admin, Management")]
    public async Task<IActionResult> UpdatePlanning([FromBody] UpdateShiftPlanningDTO body) {
        Console.WriteLine($"ShiftsController.UpdatePlanning(ShiftId: {body.ShiftId}, Planned: {body.Planned})");
        return Ok(await _shiftService.UpdateShiftPlanning(body.ShiftId, body.Planned));
    }

    [HttpGet("overview")]
    [Authorize(Roles = "Admin, Management")]
    public async Task<IActionResult> Overview() {
        Console.WriteLine("ShiftsController.Overview()");

        return Ok(await _shiftService.GetUserShiftOverview());
    }
}