$(document).ready(function ()
{
    /** */
    var taskSeries = $.get("");
    var taskFilms = taskSeries;

    var propositions = $("#propositions");
    var urlBase = $("#urlbase").attr("href");

    $("#searchBar").keypress(function ()
    {
        if (event.keyCode == 40 || event.keyCode == 38 || event.keyCode == 13) // arrowdown
        {
            if (tab_index >= 0 && $("#propositions").css('display') == 'block' && $("#propositions")[0].childNodes.length > 0)
                event.preventDefault();
        }
    });

    $("#searchBar").keyup(function (event)
    {
        if (event.keyCode == 40 || event.keyCode == 38 || event.keyCode == 13) // arrowdown
        {
            event.preventDefault();
            keyPress(event);

            if (event.keyCode == 13)
                $("#" + tab_index).trigger("click");
        }
    });

    $("#searchBar").on('input', function (event)
    {
        if (taskSeries != null)
            taskSeries.abort();

        if (taskFilms != null)
            taskFilms.abort();

        if (this.value.length > 2) 
        {
            var search = this.value;
            var index = search.indexOf("=>");

            if (index > 0)
                search = search.substring(index + 2);

            if (search.length <= 2)
            {
                propositions.hide("fast");
                return;
            }

            taskSeries = $.get("https://api.tvmaze.com/search/shows?q=" + search, function (response)
            {
                propositions.empty();

                var cpt = 0;
                $.each(response, function (_, value)
                {
                    var v = $("<a>").text(value.show.name).addClass(["propositions-item", "row"]).attr("id", cpt++);

                    v.click(function ()
                    {
                        $("#searchBar").val(this.innerText);
                        propositions.empty();
                        propositions.hide("fast");
                        $("#searchBar").focus();
                        tab_index = -1;
                        resetLiClass();
                    });

                    propositions.append(v);
                });

                if (cpt > 0)
                    propositions.show("fast");

                taskSeries = null;

            });
            taskFilms = $.get(urlBase + "api/films/" + search, function (response)
            {
                var cpt = propositions[0].childElementCount;

                $.each(response, function (key, value)
                {
                    var v = $("<a>").text(value.name).addClass(["propositions-item", "row"]).attr("id", cpt++);

                    v.click(function ()
                    {
                        $("#searchBar").val(this.innerText);
                        propositions.empty();
                        propositions.hide("fast");
                        $("#searchBar").focus();
                        tab_index = -1;
                        resetLiClass();
                    });

                    propositions.append(v);
                });

                if (cpt > 0)
                    propositions.show("fast");

                taskFilms = null;
            });
        }
        else
        {
            propositions.empty();
            propositions.hide("fast");
        }

        if (propositions[0].childNodes.length < 1)
        {
            tab_index = -1;
            propositions.hide("fast");
        }
    });

    $("#searchBar").focusin(function ()
    {
        propositions.show("fast");
    });

    $(document).click(function (event)
    {
        if (!$(event.target).hasClass("propositions-item") && event.target.id != "searchBar")
        {
            tab_index = -1;
            propositions.hide("fast");
            resetLiClass();
        }
    });

    $("#btnOK").click(function ()
    {
        var str = "";

        $("tr").each(function ()
        {
            str += this.id + "|";
        });

        eraseCookie("languageOrder");
        createCookie("languageOrder", str);
    });
    
    $('.search_icon').on("contextmenu", function (e)
    {
        $(".dcm").show("fast");

        var menu = $("#rmenu");

        menu[0].style.top = mouseY(event) + "px";
        menu[0].style.left = mouseX(event) + "px";

        return false;
    });

    $(".dcm").click(function ()
    {
        $(this).hide("fast");
        $("#rmenu").hide("fast");
    });

    $("#valCit").click(function ()
    {
        var citation = { contenue: $("#contenue").val(), authorName: $("#auteur").val() };

        var sweetParams = {
            title: "Titre test",
            confirmButtonText: "Ok"
        };

        $.post(urlBase + "api/citation", citation, function (response)
        {
            $("#contenue").val("");
            $("#auteur").val("");

            sweetParams.title = response;
            sweetParams.icon = "success";

            SwalFire(sweetParams);
        }).fail(function (error)
        {
            sweetParams.title = error.responseText;
            sweetParams.icon = "error";

            SwalFire(sweetParams);
        });
    });

    $("#dform").submit(function ()
    {
        let loading = $("#loading");

        loading.show("fast");
        loading.focus();
    });

    /*window.Swal.fire(
    {
        title: "Titre test",
        html: "texte",
        icon: "success",
        showDenyButton: true,
        confirmButtonText: "Oui, ce type est moche !",
        denyButtonText: "J'ai changé d'avis",
        preConfirm: (e) => console.log("confirm", e), // appel juste avant fermeture si valider
        preDeny: (e) => console.log("deny", e), // appel juste avant fermeture si refus (deny mais pas cancel)
        didClose: (e) => console.log("close", e), // appel à la fermeture dans tous les cas
        didDestroy: (e) => console.log("destroy", e) // appel en dernier dans tous les cas
    });*/
});


var allowDrop = function (ev)
{
    ev.preventDefault();
}

var drag = function (ev)
{
    ev.dataTransfer.setData("text", ev.target.id);
}

var drop = function (ev)
{
    ev.preventDefault();

    var TRStart = document.getElementById(ev.dataTransfer.getData("text"));
    var TRDrop = document.getElementById(ev.currentTarget.id);

    swapNodes(TRStart, TRDrop);
}

function swapNodes(a, b)
{
    var pa1 = a.parentNode, pa2 = b.parentNode, sib = b.nextSibling;
    if (sib === a) sib = sib.nextSibling;
    pa1.replaceChild(b, a);
    if (sib) pa2.insertBefore(a, sib);
    else pa2.appendChild(a);
    return true;
}



var controle = false;
var tab_index = -1;

function keyPress(event)
{
    var count = $("#propositions").children().length;

    if (event.keyCode == 40 && tab_index == count - 1)
    {
        tab_index = -1;
    }


    if (event.keyCode == 38 && tab_index == 0)
    {
        tab_index = count;
    }

    tab_index = tab_index + (event.keyCode == 40 ? 1 : event.keyCode == 38 ? -1 : 0);

    var elt = $("#" + tab_index);
    var props = $("#propositions");

    elt.addClass("element_liste_hover");

    var posElt = elt.position().top + elt.height();
    var posDivBas = props.position().top + props.height();

    if (posElt > posDivBas)
    {
        props.scrollTop(props.scrollTop() + (elt.height() * 2));
    }

    if (posElt < props.position().top)
    {
        props.scrollTop(props.scrollTop() - (elt.height() * 2));
    }

    if (tab_index == 0)
    {
        props.scrollTop(0);
    }

    if (tab_index == count - 1)
    {
        props.scrollTop(9999);
    }

    resetLiClass();
}

function resetLiClass()
{
    var y = 0;
    while (y < $("#propositions")[0].childNodes.length)
    {
        if (y != tab_index)
        {
            $("#" + y).attr("class", "propositions-item row");
        }
        y = y + 1;
    }
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