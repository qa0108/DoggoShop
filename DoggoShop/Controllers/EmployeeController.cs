namespace DoggoShopAPI.Controllers;

using DoggoShopAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : Controller
{
    private Prn221dbContext context = new Prn221dbContext();
    
    [HttpGet]
    [Route("GetAll")]
    public IActionResult GetAll()
    {
        var e =  this.context.Employees.ToList();
        return this.Ok(e);
    }
}