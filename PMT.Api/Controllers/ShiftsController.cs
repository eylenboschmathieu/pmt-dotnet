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
    [HttpGet("requests/{userId}/{year}/{month}")]
    public async Task<IActionResult> GetUserRequests(int userId, int year, int month) {
        Console.WriteLine("ShiftsController.GetUserRequests(int userId, int year, int month)");

        // Still have the 'minor' issue that somebody could just enter any userId other than their own and get access to their requests.
        // Looking into that now

        if (month < 1 || month > 12)
            return BadRequest(month);
        
        return Ok(await _shiftService.GetUserRequests(userId, year, month));
    }

    [Authorize(Policy = "CanModify")]
    [HttpPut("requests/update/{userId}")]
    public async Task<IActionResult> UpdateRequest(int userId, [FromBody] UpdateRequestDTO body) {
        Console.WriteLine("ShiftController.UpdateRequest");
        return Ok(await _shiftService.UpdateShiftRequest(userId, body));
    }

    [Authorize]
    [HttpGet("confirmed/{userId}/{year}/{month}")]
    public async Task<IActionResult> GetConfirmedShifts(int userId, int year, int month) {
        Console.WriteLine("ShiftsController.GetConfirmedShifts(int userId, int year, int month)");
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

    [HttpGet("planning/{year}/{month}")]
    [Authorize(Roles = "Admin, Management")]
    public async Task<IActionResult> GetMonthPlanning(int year, int month) {
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
}