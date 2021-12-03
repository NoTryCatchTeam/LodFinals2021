$(document).ready(function () {


});

function initIndex() {
    VanillaTilt.init(document.querySelectorAll(".analytics-item"));

    $(".notifications").bind("webkitAnimationEnd mozAnimationEnd animationEnd", function (e) {
        $(".notifications img").removeClass("animate__swing")
    }).mouseenter(function (e) {
        $(".notifications img").addClass("animate__swing");
    });
}