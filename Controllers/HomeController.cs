using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using hastane.Models;

namespace hastane.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Doktor giriş yaptıysa Dashboard'a yönlendir
        if (HttpContext.Session.GetString("UserRole") == "Doctor")
        {
            return RedirectToAction("Dashboard", "Doctor");
        }
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
