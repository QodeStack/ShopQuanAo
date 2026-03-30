// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    $('.hero-slider').slick({
        autoplay: true,
        autoplaySpeed: 5000,
        speed: 1000,
        fade: true,
        cssEase: 'ease-in-out',
        arrows: true,
        dots: false,
        pauseOnHover: false
    });

    /* Overlay text is positioned absolute over the slider wrapper */
    var $overlay = $('.hero-overlay');
    var $slider = $('.hero-slider');
    $slider.css('position', 'relative');
    $overlay.css({
        position: 'absolute',
        zIndex: 10
    });
});

