$(document).ready(() =>
{
    var urlSiteBase = $("#url").val();

    $("#tester").click((event) =>
    {
        event.preventDefault();

        var form = $("form").first();
        var obj = {};
        form.serializeArray().map((e) => obj[e.name] = e.value)

        if (obj["spostValues"] != undefined && obj["spostValues"].length > 0)
            obj["PostValues"] = JSON.parse(obj["spostValues"]);

        obj["url"] = urlSiteBase;

        $("#loading").show("fast");
        $.post("/api/TestSiteSearch", obj, function (res)
        {
            $("#loading").hide("fast", function()
            {
                $(this).attr("style", "display: none!important;"); // appeler à la fin pour override le d-flex

                $("#messageTitle").text("Reponse");

                let body = $("#messageBody");

                body.text("");
                body.append($("<p>").text("La recherche " + res.search + " à donnée " + res.nb_result + " résultat(s)."));
                body.append($("<p>").text("-1 signifie qu'il y as une erreur, 0 = pas de résultat ou résultat inateignable"));
                body.append($("<p>").text("Le contenue HTML de la page de résultat se trouve dans la console si le nombre de résultat est <= 0."));
                body.append($("<p>").text("Cliquez ici pour ouvrir la page de résultat dans un autre onglet.").attr("onClick", res.js).attr("style", "cursor: pointer;"));

                if(res.nb_result <= 0) // pour eviter les logs inutile, mais res.pageHTMl est vide donc ça ne change rien
                    console.log(res.pageHTML);

                $("#dialogMessage").modal("show");
            });

        }).fail((error) =>
        {
            console.log(error.responseText);

            $("#loading").hide("fast", function ()
            {
                $(this).attr("style", "display: none!important;"); // appeler à la fin pour override le d-flex

                $("#messageTitle").text("Erreur");
                $("#messageBody").text(error.responseText);
                $("#dialogMessage").modal("show");
            });
        });
    });

    $("#valdier").click((e) =>
    {
        e.preventDefault();

        var form = $("form").first();
        var obj = {};
        form.serializeArray().map((e) => obj[e.name] = e.value)

        if (obj["spostValues"] != undefined && obj["spostValues"].length > 0)
            obj["PostValues"] = JSON.parse(obj["spostValues"]);

        obj["url"] = urlSiteBase;

        $.post("/adminapi/majsite", obj, (response) =>
        {
            console.log(response);
            SwalFire({
                title: "Modification enregistrées",
                icon: "success",
                confirmButtonText: 'Cool !'
            });

            if (obj["urlChange"] != urlSiteBase)
                urlSiteBase = obj["urlChange"];

        }).fail((error) =>
        {
            SwalFire({
                icon: "error",
                confirmButtonText: 'Super...',
                title: error.responseText
            })
        });
    });
});