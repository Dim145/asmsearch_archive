$(document).ready(function()
{
    $('#ba').on('hidden.bs.modal', function ()
    {
        var frame = $("#frame");
        frame.data("link", frame.attr("src"));
        frame.attr("src", "");

    });

    $("#btnBa").click(function ()
    {
        var frame = $("#frame");

        if (frame.data("link") != undefined && frame.data("link") != "")
        {
            frame.attr("src", frame.data("link"));
            frame.data("link", "");
        }
    });

    $('#modal')
        .on('hide', function ()
        {
            console.log('hide');
        })
        .on('hidden', function ()
        {
            console.log('hidden');
        })
        .on('show', function ()
        {
            console.log('show');
        })
        .on('shown', function ()
        {
            console.log('shown')
        });

    $("tr").each(function ()
    {
        var onclickstr = $(this).children().last().attr('onclick');

        if (onclickstr != undefined)
        {
            var index = onclickstr.indexOf('action", "') + 10;

            if (index <= 9)
                index = onclickstr.indexOf('"') + 1;
            else
                return;

            if (index > 0)
            {
                var lastIndex = onclickstr.indexOf('"', index);
                var url = onclickstr.substring(index, lastIndex);

                var a = document.createElement('a');
                a.href = url;

                var cpt = 0;
                $.each($(this).children(), function ()
                {
                    if (cpt++ > 0)
                    {
                        $(this).mouseover(function ()
                        {
                            $(this).append(a);

                            $(a).focus();
                        });

                        $(this).mouseleave(function ()
                        {
                            $(a).remove();
                        });
                    }
                });
            }
        }
        
    });
});