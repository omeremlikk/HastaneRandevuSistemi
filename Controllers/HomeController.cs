using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using hastane.Models;
using hastane.Data;
using System.Linq;

namespace hastane.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        // Doktor giriş yaptıysa Dashboard'a yönlendir
        if (HttpContext.Session.GetString("UserRole") == "Doctor")
        {
            return RedirectToAction("Dashboard", "Doctor");
        }
        
        // Doktor sayısını al
        ViewBag.DoctorCount = _context.Doctors.Count();
        
        return View();
    }

    public IActionResult Privacy()
    {
        // Doktor giriş yaptıysa Dashboard'a yönlendir
        if (HttpContext.Session.GetString("UserRole") == "Doctor")
        {
            return RedirectToAction("Dashboard", "Doctor");
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
