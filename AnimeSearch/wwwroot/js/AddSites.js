var tabSearch = ["one piece", "naruto", "fairy tail", "bleach", "lucifer",  "black clover", "black mirror", "nanatsu no taizai", "attaque des titans"];
var indexSearch = 0;

$(document).ready(function ()
{
    var elements = [$("#baseURL"), $("#searchURL"), $("#iconURL"), $("#siteTitle"), $("#type"), $("#cheminElt"), $("#idDoc")];
    var urlBase = $("#urlbase").attr("href");

    elements[0].focusout(function (e)
    {
        var baseURL = $(this);
        var url = $("#httpType").val();

        url += baseURL.val();

        $.get(urlBase + "testURL?url=" + encodeURIComponent(url), function ()
        {
            baseURL[0].setCustomValidity("");
            addVal();

        }).fail(function ()
        {
            baseURL[0].setCustomValidity("adresse inateignable");
            elements[1][0].setCustomValidity("adresse inateignable");
            addVal();
            $("#btnDiv").hide("fast");
        });
    });

    elements[0].on("paste", httpClipBoradSupp);

    elements[1].focusout(function ()
    {
        var value = $(this).val();

        var p = $("#POST-values").children().length;

        if ((value == "" || value == undefined) && p <= 0)
        {
            addVal();
            $("#btnDiv").hide("fast");
        }
        else
        {
            var searchURL = this;
            var url = $("#httpType").val();

            url += elements[0].val() + "/" + value;

            $.get(urlBase + "testURL?url=" + encodeURIComponent(url), function ()
            {
                searchURL.setCustomValidity("");
                addVal();

                changeDialogSite(url);
                $("#btnDiv").show("fast");

            }).fail(function ()
            {
                searchURL.setCustomValidity("adresse inateignable");
                addVal();
                $("#btnDiv").hide("fast");
            });
        }
    });

    elements[2].focusout(function ()
    {
        var searchURL = this;
        var url = $("#httpTypeIcon").val() + $(this).val();

        $.get(urlBase + "testURL?url=" + encodeURIComponent(url), function ()
        {
            $("#imageIcon").attr("src", url);
            searchURL.setCustomValidity("");
            addVal();

        }).fail(function ()
        {
            $("#imageIcon").attr("src", "");
            searchURL.setCustomValidity("adresse inateignable");
            addVal();
        });

        addVal();
    });

    elements[2].on("paste", httpClipBoradSupp);

    elements[3].focusout(function ()
    {
        addVal();
    });

    elements[4].focusout(function ()
    {
        addVal();
    });

    elements[5].focusout(function ()
    {
        addVal();
    });

    elements[6].focusout(function ()
    {
        addVal();
    });

    $('#dialogIframe').on('hidden.bs.modal', function (e)
    {
        addVal();
    });

    $('#dialogPOST').on('hidden.bs.modal', function (e)
    {
        addVal();

        elements[1].focus();
        $(this).focus(); // trigger the element[1] focusout event.
    });

    $("#site").click(function (event)
    {
        $("#btnFermer").click();

        var target = $(event.target);
        var path = "";

        event.preventDefault();

        while (target[0] != undefined && target[0].id != "site" && target[0].localName != "body")
        {
            if (target[0].localName != undefined)
            {
                var tmp = "/" + target[0].localName;
                var classString = target.attr('class');

                if (classString != undefined && classString != "" && classString.indexOf(" ") <= 0 && classString.indexOf("-") <= 0)
                    tmp += "[@class='" + classString + "']";

                path = tmp + path;
            }

            target = target.parent();
        }

        elements[5].val(path.substr(1));
        elements[6].val("");
    });

    $("#research").click(function ()
    {
        var url = $("#httpType").val();

        url += elements[0].val() + "/" + elements[1].val();

        changeDialogSite(url);
    });

    $("#addPOST").click(function ()
    {
        var tr = $("<tr>");
        var tbody = $("#POST-values");

        var nb = tbody.children().length;

        tr.append($("<td>").append($("<input>").addClass("w-100").attr("id", "name" + nb)));
        tr.append($("<td>").append($("<input>").addClass("w-100").attr("id", "val" + nb)));

        tbody.append(tr);
    });

    /**
     * 
     * @param {JQuery.TriggeredEvent} event
     */
    function httpClipBoradSupp (event)
    {
        var pastedData = event.originalEvent.clipboardData.getData('text') + "";

        if (pastedData.startsWith("http"))
        {
            $(this).val(pastedData.substr(pastedData.indexOf("//") + 2));
            event.preventDefault();
        }
    }

    /**
     * 
     * @param {number} replace
     */
    function getPostValues(replace = -1)
    {
        var postValues = {};

        var postLength = $("#POST-values").children().length;

        for (var cpt = 0; cpt < postLength; cpt++)
        {
            var name = $("#name" + cpt).val();
            var value = $("#val" + cpt).val();

            if (name != "" && name != undefined && value != undefined && value != "")
            {
                if (value == "?")
                    value = replace != -1 ? tabSearch[replace] : null;

                postValues[name] = value;
            }
        }

        return postValues;
    }

    /**
     * 
     * @param {string} url
     */
    function changeDialogSite(url)
    {
        var tmp = indexSearch++;

        var postValues = getPostValues(tmp);

        $.post(urlBase + "GetHTMLSite?url=" + encodeURIComponent(url + (url.endsWith("=") ? tabSearch[tmp] : "")), postValues, function (response)
        {
            $("#site").text("");
            $("#site").append(response);
            $("#dataSearch").text(tabSearch[tmp]);
        }).fail(function ()
        {
            $("#messageTitle").text("Erreur");
            $("#messageBody").text("Erreur lors du chargement du site saisi.");

            $("#dialogMessage").modal("show");
        });

        if (indexSearch >= tabSearch.length)
            indexSearch = 0;
    }

    function addVal ()
    {
        var isValid = true;

        $.each(elements, function (_key, data)
        {
            if (!data[0].validity.valid)
                isValid = false;
        });

        if (isValid)
        {
            if ($("#valider")[0] == undefined)
            {
                var btn = $("<button>").attr("id", "valider").addClass(["btn", "btn-dark", "btn-outline-light"]).text("Valider");
                $("#valid-Zone").append(btn);

                btn.click(validerForm);
            }
        }
        else
        {
            $("#valider").remove();
        }
    }

    function validerForm()
    {
        var site = {
            url: $("#httpType").val() + elements[0].val(), urlSearch: elements[1].val(),
            urlIcon: $("#httpTypeIcon").val() + elements[2].val(), title: elements[3].val(),
            typeSite: elements[4].val(), cheminBaliseA: elements[5].val(),
            idBase: elements[6].val(), postValues: getPostValues(-1), is_inter: $("#lang").is(":checked")

        };

        $.post(urlBase + "api/addSite", site, function (response)
        {
            $("#messageTitle").text("Succès");
            $("#messageBody").text(response);

            $("#dialogMessage").modal("show");
        }).fail(function (error)
        {
            $("#messageTitle").text("Erreur");
            $("#messageBody").text(error.responseText);

            $("#dialogMessage").modal("show");
        });
    }
});