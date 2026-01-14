using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PMT.Services;

namespace PMT.Api.Controllers;

[ApiController]
public class ShiftsController(ShiftService _shiftService) : ControllerBase {
    
    [Authorize]
    [HttpGet("shifts")]
    public IActionResult GetShiftHours() {
        return Ok(_shiftService.GetShiftHours());
    }

    [Authorize(Policy = "CanModify")]
    [HttpGet("requests")]
    public async Task<IActionResult> GetUserRequests([FromQuery] int userId, [FromQuery] int year, [FromQuery] int month) {
        Console.WriteLine($"ShiftsController.GetUserRequests(userId: {userId}, year: {year}, month: {month})");

        if (month < 1 || month > 12)
            return BadRequest(month);
        
        return Ok(await _shiftService.GetUserRequests(userId, year, month));
    }

    [Authorize(Policy = "CanModify")]
    [HttpPut("requests/update")]
    public async Task<IActionResult> UpdateRequest([FromQuery] int userId, [FromBody] UpdateRequestDTO body) {
        Console.WriteLine("ShiftController.UpdateRequest");
        return Ok(await _shiftService.UpdateShiftRequest(userId, body));
    }

    [Authorize]
    [HttpGet("confirmed")]
    public async Task<IActionResult> GetConfirmedShifts([FromQuery] int userId, [FromQuery] int year, [FromQuery] int month) {
        Console.WriteLine($"ShiftsController.GetConfirmedShifts(userId: {userId}, year: {year}, month: {month})");
        if (month < 1 || month > 12)
            return BadRequest(month);

        UserConfirmedDTO data = await _shiftService.GetConfirmedShiftsForUser(userId, year, month);
        
        return Ok(data);
    }

    [HttpGet("planning/dates")]
    [Authorize(Roles = "Admin, Management")]
    public async Task<IActionResult> GetPlanningMonths() {
        return Ok(await _shiftService.GetPlanningMonths());
    }

    [HttpGet("planning")]
    [Authorize(Roles = "Admin, Management")]
    public async Task<IActionResult> GetMonthPlanning([FromQuery] int year, [FromQuery] int month) {
        Console.WriteLine("ShiftsController.GetConfirmedShifts(int userId, int year, int month)");
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
        Console.WriteLine($"ShiftsController.UpdatePlanning({body.Confirm}, {body.ShiftId})");
        return Ok(await _shiftService.UpdateShiftPlanning(body.Confirm, body.ShiftId));
    }

    [HttpGet("overview")]
    [Authorize(Roles = "Admin, Management")]
    public async Task<IActionResult> Overview() {
        Console.WriteLine("ShiftsController.Overview()");

        return Ok(await _shiftService.GetUserShiftOverview());
    }
}