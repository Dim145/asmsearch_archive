$("#delete").click( e =>
{
    SwalFire({
        icon: "warning",
        input: "password",
        title: "Supprimer votre compte est définitif. Il n'est pas possible de le récupérer.",
        text: 'Seules les recherches sont gardées afin de ne pas perdre les valeurs des tops de la page "Données". Mais elles sont anonymisées.',
        confirmButtonText: "Valider",
        denyButtonText: "Annuler",
        showDenyButton: true,
        preConfirm: (res) => {
            if (res.length <= 0)
                Swal.showValidationMessage('Veuillez saisir votre mot de passe.')
            else
                return res;
        }
    }).then(res =>
    {
        if (res.isConfirmed)
        {
            $.post("/api/account/datas/delete", { mdp: res.value }, () =>
            {
                window.location = window.location.protocol + "//" + window.location.host;
            }).fail(e => SwalFire({ icon: "error", title: "Erreur", text: e.responseText }));
        }
    });
});