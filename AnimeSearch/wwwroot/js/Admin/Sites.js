$(document).ready(function ()
{
    $(".site").each(function ()
    {
        var lig = $(this);

        var error = lig.hasClass("bg-danger");
        var non_val = lig.hasClass("bg-warning");
        
        lig.on("contextmenu", function ()
        {
            $(".dcm").show("fast");

            let menu = $("#rmenu");
            var url = $(this).attr("id");

            $("#formid").val(url);
            $("#modifid").val(url);
            $("#cval").text(non_val ? "Validé" : error ? "Corrigé" : "Erreur (404)");
            $("#valstate").val(non_val || error ? 1 : 2);

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