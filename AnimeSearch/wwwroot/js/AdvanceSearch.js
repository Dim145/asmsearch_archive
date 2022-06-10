$(document).ready(() =>
{
    let form = $("form");

    $("#val").click(() =>
    {
        $(".filter").each((index, elt) =>
        {
            let value = $(elt).data("select");

            if (value != 0)
                form.append($("<input>").attr("type", "hidden").attr("name", value == 1 ? "with_genres" : "without_genres").attr("value", elt.innerText));
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