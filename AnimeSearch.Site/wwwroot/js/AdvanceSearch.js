$(document).ready(() =>
{
    let form = $("form");

    $("#val").click(() =>
    {
        $(".filter").each((index, elt) =>
        {
            let value = $(elt).data("select");

            if (value != 0)
                form.append($("<input>").attr("type", "hidden").attr("name", value == 1 ? "withGenres" : "withoutGenres").attr("value", elt.innerText));
        });
    });
    
    $("#prev").click(() =>
    {
        const page = $("[name='page']");
        
        page.val(page.val() - 1);

        $("#val").click();
    });

    $("#next").click(() =>
    {
        const page = $("[name='page']");

        page.val(parseInt(page.val()) + 1);

        $("#val").click();
    });
    
    $("#SearchIn").change(function()
    {
        const value = this.value;
        
        $(".filter").each((index, elt) =>
        {
            const genre = $(elt);
            const type = genre.data("type");
            
            if(value == "All" || type == "All") 
            {
                genre.show();
            }
            else
            {
                if(value == type) 
                {
                    genre.show();
                }
                else 
                {
                    genre.hide();
                    genre.data("select", 0);
                    
                    genre.removeClass("btn-success");
                    genre.removeClass("btn-danger");
                    
                    if(!genre.hasClass("btn-dark"))
                        genre.addClass("btn-dark");
                }
            }
        });
    });

    $(".filter").each((index, elt) =>
    {
        elt.onclick = () =>
        {
            let element = $(elt);
            let value = element.data("select");

            element.toggleClass(value == 0 ? "btn-dark" : value == 1 ? "btn-success" : "btn-danger");
            element.data("select", value == 0 ? 1 : value == -1 ? 0 : -1);

            value = element.data("select");
            element.toggleClass(value == 0 ? "btn-dark" : value == 1 ? "btn-success" : "btn-danger");

            if (form.attr("method") == "get")
                form.attr("method", "post");
        }
    });

    $("#reset").click(() =>
    {
        $(".filter").each((index, elt) =>
        {
            let element = $(elt);
            let value = element.data("select");

            element.toggleClass(value == 0 ? "btn-dark" : value == 1 ? "btn-success" : "btn-danger");
            element.data("select", 0);

            value = element.data("select");
            element.toggleClass("btn-dark");
        });
    });

    $(".box").each((_, elt) =>
    {
        let box = $(elt);

        let btn = box.children().last();

        if (box.children().first()[0].scrollHeight > box.children().first()[0].offsetHeight)
        {
            btn.click((e) =>
            {
                box.toggleClass("open");

                e.preventDefault();
            });
        }
        else
        {
            btn.remove();
        }
    });
});