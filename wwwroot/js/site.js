// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Sayfa yüklendiğinde
document.addEventListener('DOMContentLoaded', function() {
    // Eksik görseller için yedek görsel
    const images = document.querySelectorAll('img');
    images.forEach(img => {
        img.addEventListener('error', function() {
            if (img.classList.contains('doctor-image')) {
                this.src = '/img/placeholder-doctor.jpg';
            } else {
                this.src = '/img/placeholder.jpg';
            }
        });
    });
    
    // Navbar scroll efekti
    const navbar = document.querySelector('.navbar');
    if (navbar) {
        window.addEventListener('scroll', function() {
            if (window.scrollY > 50) {
                navbar.classList.add('navbar-scrolled', 'shadow');
            } else {
                navbar.classList.remove('navbar-scrolled', 'shadow');
            }
        });
    }
    
    // Bootstrap tooltip aktivasyonu
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });
});
