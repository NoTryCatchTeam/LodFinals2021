$(document).ready(function () {

});

function initIndex() {
    commonInit();

    VanillaTilt.init(document.querySelectorAll(".analytics-item"));
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

    // VanillaTilt.init(document.querySelector(".modal__container"), {
    //     max: 15,
    //     gyroscope:              true,
    //     gyroscopeMinAngleX:     -15, 
    //     gyroscopeMaxAngleX:     15,  
    //     gyroscopeMinAngleY:     -15, 
    //     gyroscopeMaxAngleY:     15,
    // });
    
    topBarInit();
}

function topBarInit() {
    $(".notifications").bind("webkitAnimationEnd mozAnimationEnd animationEnd", function (e) {
        $(".notifications img").removeClass("animate__swing")
    }).mouseenter(function (e) {
        $(".notifications img").addClass("animate__swing");
    });
}