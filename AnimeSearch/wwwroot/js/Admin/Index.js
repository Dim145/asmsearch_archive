$(document).ready(function ()
{
    var baseURL = $("#ips").attr("href");

    $(".user_lig").each(function ()
    {
        var thisTR = $(this);

        thisTR.on("contextmenu", function ()
        {
            $(".dcm").show("fast");

            var menu = $("#rmenu");

            $("#ips").attr("href", baseURL + "admin/ips/" + thisTR.data("userid"));
            $("#recherches").attr("href", baseURL + "admin/recherches/" + thisTR.data("userid"));
            $("#dons").attr("href", baseURL + "admin/dons/" + thisTR.data("userid"));
            $("#savedsearch").attr("href", baseURL + "admin/SavedSearchsForUser/" + thisTR.data("userid"))

            menu[0].style.top = mouseY(event) + "px";
            menu[0].style.left = mouseX(event) + "px";

            menu.show("fast");

            return false;
        });
    });

    $(".dcm").click(function ()
    {
        $(this).hide("fast");
        $("#rmenu").hide("fast");
    });
});

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