$(document).ready(function ()
{
    var baseUrl = $("#baseurl").attr("href");

    $(".citation").each(function ()
    {
        let numero = $(this).attr("id");

        $(this).on("contextmenu", () =>
        {
            $(".dcm").show("fast");

            let menu = $("#rmenu");
            let isValidate = $(this).data("validated");

            $("#formid").val(numero);
            $("#cval").text(isValidate ? "Invalider" : "Valider");
            $("#valstate").val(!isValidate);

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