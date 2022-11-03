$("#swalDon").click(() =>
{
    let donsNb = $("#swalDon").data("props");
    const TABDONS = [{ name: "Eur (PayPal)", url: "/ressources/images/paypal.svg", value: 1 }, { name: "BTC (OpenNode)", url: "/ressources/images/opennode.svg", value: 2 }]

    SwalFire({
        iconHtml: `<img width='80' id='logo' src='${TABDONS.filter(d => d.value <= donsNb)[0].url}' />`,
        title: "Faire un don",
        confirmButtonText: "Valider",
        denyButtonText: "Annuler",
        showDenyButton: true,
        input: 'number',
        inputPlaceholder: 'Montant en EUR',
        inputAttributes: {
            id: "inswalput",
            step: 0.1
        },
        preConfirm: () => {
            let value = document.getElementById('inswalput').value;

            if (value && value >= 1) {
                return value;
            }
            else {
                Swal.showValidationMessage('Le montant doit être supérieur ou égal à 1.')
            }
        },
        html: `<select class="swal2-select" id="typeDon">${options()}</select>`
    }).then((res) =>
    {
        if (res.value)
        {
            let type = $("#typeDon").val();
            let url = "/api/donate?type=" + type + "&amount=" + res.value;

            if (type == 0) window.location = url;
            else window.open(url);
        }
    });

    $("#typeDon").change(() =>
    {
        let val = $("#typeDon").val();

        $("#logo").attr("src", TABDONS[val].url)
    });

    function options()
    {
        let options = '';

        for (let i = TABDONS.length-1; i >= 0; i--)
        {
            console.log(i)
            if (TABDONS[i].value <= donsNb)
            {
                donsNb -= TABDONS[i].value;

                options += `<option value="${i}" ${i == 0 ? "selected" : ""}>${TABDONS[i].name}</option>`
            }
        }

        return options;
    }
});