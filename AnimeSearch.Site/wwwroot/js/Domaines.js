$(document).ready(() =>
{
    $("tr").each(function (index)
    {
        if (index > 0)
        {
            let url = this.firstElementChild.firstElementChild.href;
            let td = this.children[1];

            $.get("testURL?url=" + encodeURIComponent(url), (response) =>
            {
                td.className = "text-success";
                td.textContent = "Fonctionnel";
            }).fail((error) =>
            {
                td.className = "text-danger";
                td.textContent = "Innateignable";
            });
        }
    });
});