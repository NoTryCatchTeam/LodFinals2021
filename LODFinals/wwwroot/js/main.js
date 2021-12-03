$(document).ready(function () {

    // $(document).on('click', 'a.nav-href', function (e) {
    //     let a = $(this)[0];
    //     console.log(a);
    //
    //     let href = a.href;
    //     e.preventDefault();
    //     $(".index").removeClass("animate__fadeIn").addClass("animate__fadeOut");
    //
    //     setTimeout(function () {
    //             window.location.replace(href);
    //         },
    //         1000);
    // });

});

function initIndex() {
    commonInit();

    VanillaTilt.init(document.querySelectorAll(".analytics-item"));

    $(".notifications").bind("webkitAnimationEnd mozAnimationEnd animationEnd", function (e) {
        $(".notifications img").removeClass("animate__swing")
    }).mouseenter(function (e) {
        $(".notifications img").addClass("animate__swing");
    });


}

function commonInit() {
}