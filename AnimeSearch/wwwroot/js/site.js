// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var gettingCurrent = "test";

var username = readCookie("userName");

/*if (username == null || username.trim() == "" || username == "null") {
    createCookie("userName", prompt("User Name"));
}*/

/**
 * 
 * @param {string} name
 * @param {string} value
 */
function createCookie(name, value) {
    if (name == null || name == "") return;

    var d = new Date();
    d.setTime(d.getTime() + (365 * 24 * 60 * 60 * 1000));
    var expires = "expires=" + d.toUTCString();

    document.cookie = encodeURIComponent(name) + "=" + encodeURIComponent(value) + ";" + expires + "; path=/;SameSite=Strict";
}

/**
 * 
 * @param {string} name
 */
function readCookie(name) {
    var nameEQ = encodeURIComponent(name) + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) === ' ')
            c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) === 0)
            return decodeURIComponent(c.substring(nameEQ.length, c.length));
    }
    return null;
}

function eraseCookie(name) {
    createCookie(name, "", -1);
}

window.ChangeUrl = function (url) {
    history.pushState(null, '', url);
}

var showPopUp = true;

$(document).ready(function () {
    $("center").remove();
    $("script[lang='JavaScript']").remove();
    $("div[onmouseover='S_ssac();']").remove();
    $("div[style='opacity: 0.9; z-index: 2147483647; position: fixed; left: 0px; bottom: 0px; height: 65px; right: 0px; display: block; width: 100%; background-color: #202020; margin: 0px; padding: 0px;']").remove();

    $("footer div a").first().click(function ()
    {
        window.open("/Pokemon");
    });

    var username = readCookie("userName");

    if (username != undefined && username != null && username != "" && username != "null")
    {
        var balise = document.getElementById("username");
        if (balise != undefined)
            balise.textContent = "Hello, " + username;
    }

    $("#change_username").on("click", function (event)
    {
        if (showPopUp)
        {
            createCookie("userName", prompt("User Name"));
            username = readCookie("userName");

            showPopUp = false;
        }
        else
        {
            showPopUp = true;
        }

        if (username != undefined && username != null && username != "" && username != "null")
        {
            var usernameBalise = document.getElementById("change_username");

            if (usernameBalise != undefined)
            {
                usernameBalise.textContent = "Hello, " + username;
                usernameBalise.href = "";
            }

            $(this).unbind();
        }
    });

    var divHeure = $("#heure");

    if (divHeure != undefined && divHeure[0] != undefined)
    {
        function onMinute(cb, init)
        {
            if (typeof cb === 'function')
            {
                var start_time = new Date(), timeslice = start_time.toString(), timeslices = timeslice.split(":"), start_minute = timeslices[1], last_minute = start_minute;
                var seconds = 60 - Number(timeslices[2].substr(0, 2));

                var spin = function ()
                {
                    var spin_id = setInterval(function ()
                    {
                        var time = new Date(), timeslice = time.toString(), timeslices = timeslice.split(":"), minute = timeslices[1];

                        if (last_minute !== minute)
                        {
                            clearInterval(spin_id);

                            var heure = new Date(divHeure.text());

                            heure.setMinutes(heure.getMinutes() + 1);

                            divHeure.text((heure.getMonth() + 1).toLocaleString("Fr-fr", { minimumIntegerDigits: 2 }) + "/" + heure.getDate().toLocaleString("Fr-fr", { minimumIntegerDigits: 2 }) + "/" + heure.getFullYear() + " " + heure.getHours().toLocaleString("Fr-fr", { minimumIntegerDigits: 2 }) + ":" + heure.getMinutes().toLocaleString("Fr-fr", { minimumIntegerDigits: 2 }) );

                            last_minute = minute;
                            cb(timeslice.split(" ")[4], Number(minute), time, timeslice);
                            setTimeout(spin, 58000);
                        }

                    }, 100);

                };

                setTimeout(spin, (seconds - 2) * 1000);

                if (init)
                {
                    cb(timeslice.split(" ")[4], Number(start_minute), start_time, timeslice, seconds);
                }
            }

        }


        onMinute(function (timestr, minute, time, timetext, seconds)
        {
            
        }, true);
    }
});

function setMenuPos(event)
{
    let menu = $("#rmenu");

    menu[0].style.top = mouseY(event) + "px";
    menu[0].style.left = mouseX(event) + "px";

    menu.show("fast");
}

function mouseX(evt)
{
    if (evt.pageX)
    {
        return evt.pageX;
    } else if (evt.clientX)
    {
        return evt.clientX + (document.documentElement.scrollLeft ?
            document.documentElement.scrollLeft :
            document.body.scrollLeft);
    } else
    {
        return null;
    }
}

function mouseY(evt)
{
    if (evt.pageY)
    {
        return evt.pageY;
    }
    else if (evt.clientY)
    {
        return evt.clientY + (document.documentElement.scrollTop ?
            document.documentElement.scrollTop :
            document.body.scrollTop);
    }
    else
    {
        return null;
    }
}

/**
 * 
 * @param {{ title: "", html: "", icon: "success" | "error" | "warning" | "info", showDenyButton: false, confirmButtonText: "Ok", denyButtonText: "", preConfirm: function(e):void, preDeny: function(e):void, didClose: function(e):void, didDestroy: function(e):void}} props
 * @param {boolean} isDark valeur true par default (didDestroy est override si valeur = false)
 * @returns {Promise}
 */
function SwalFire(props, isDark = true)
{
    var darkcss = $("#darkSweetAlert");

    if (!isDark)
    {
        darkcss.remove();

        props.didDestroy = () => $("body").append(darkcss);
    }

    return window.Swal.fire(props);
}