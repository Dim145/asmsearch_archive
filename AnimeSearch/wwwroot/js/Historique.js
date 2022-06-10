$(document).ready(function ()
{
    var baseUrl = $("#baseurl").attr("href");

    $(".tr-search").each(function ()
    {
        var td = $(this).children()[0];

        $(this).click(function ()
        {
            location.replace(baseUrl + "?q=" + td.innerText);
        });

        var a = document.createElement('a');
        a.href = baseUrl + '?q=' + $(this).children()[0].innerText;
        //a.innerText = "test";
        //a.style = "display: none;";

        $(this).mouseover(function ()
        {
            $(".text-center").first().append(a);

            $(a).focus();
        });

        $(this).mouseleave(function ()
        {
            $(a).remove();
        });
    });
});