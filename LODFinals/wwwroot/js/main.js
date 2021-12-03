$(document).ready(function () {

});

function initIndex() {
    commonInit();

    VanillaTilt.init(document.querySelectorAll(".analytics-item"));

    // MicroModal.show('modal-1');
}

function initLogin() {
    commonInit();
}

function initPhotos() {
    commonInit();
}

function commonInit() {
    MicroModal.init({
        awaitOpenAnimation: true,
        awaitCloseAnimation: true,
    });
    
    topBarInit();
}

function topBarInit() {
    $(".notifications").bind("webkitAnimationEnd mozAnimationEnd animationEnd", function (e) {
        $(".notifications img").removeClass("animate__swing")
    }).mouseenter(function (e) {
        $(".notifications img").addClass("animate__swing");
    });
}