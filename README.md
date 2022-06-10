# ASM Search

Le but du projet est de simplifier la vie de tous les fans de séries/animées en simplifiant la recherche du site du streaming et/ou de téléchargement. <br />
Il suffit d'exécuter une recherche pour que le serveur recherche automatiquement sur un certain nombre d'autres sites la même recherche.
Le nombre de résultats trouver sur chaque site, la date de sortie, les genres et d'autres informations seront donnés par le site.

<p align="center">
    <img src="/AnimeSearch/wwwroot/ressources/images/full-logo.svg"  height="100" title="Logo" alt="Logo introuvable" />
</p>

### Branche React 
Cette branche est celle utilisant la librairie ReactJS.NET. Elle est une ancienne version n'étant plus mise à jours mais qui peut tous à fait être reprise pour l'améliorer ou transformer le site en SPA (Single-Page-Application).
    
## Pour commencer

Le site permet de rechercher à peu près tous ce qui existe en matière de vidéos:
- Animes
- Séries (TV etc...)
- Films*

Il est aussi possible d'accéder aux recherches les plus exécutées et aux différents sites de la base de données via la page "Données".

Attention, la recherche de films fonctionne, mais le site (API) qui renvoie les données ne renvoie pas des valeurs très exhaustives. 
Par exemple, si vous recherchez "fast & furious 8", le 5 peut être trouvé en premier. Une recherche multiple est plus sûre dans ce cas-là.

### Pré-requis

Ce qu'il est requis pour commencer avec votre projet...

- **ASP.NET 5** (5.0.31 ou >)
- **SQL Serveur** en tant que base de données
- Pour le développement, **Visual Studio 2019** est vivement recommandé
- Un serveur mail accessible via smtp comme **[Gmail](https://www.google.com/intl/fr/gmail/about/)** ou **[Mail.fr](https://mail.fr)**

### Installation

Les seules installations à effectuer sont celles des programmes/logiciels du dessus.

Mais il faut configurer certaines données dans le fichier appsettings.json:
- SQLServer: chaine de connexion à la base de données.
- themoviedb_api: clé d'accès à la base de données en ligne [The Movie DB](https://www.themoviedb.org/) qui est gratuite mais obligatoire.
- Les données du serveur mail. Dans celle-ci, il y as deux adresses à renseigner:
  - le "Mail" correspond au from et est l'adresse affilier au compte avec le mdp.
  - La "Destination" est l'adresse à laquelle envoyer les email. Elle peut être la même que la valeur de "Mail"
- Un id paypal correspondant à un boutons de **don** sur paypal. Il est possible de les configurer [ici](https://www.paypal.com/buttons/)

Enfin, si la base de données n'est pas encore configurée, il faut la mettre à jour. <br/>
Dans visual studio, il faut ouvrir le gestionnaire de packages (Affichage->Autres fenêtre->Console du gestionnaire de package) et éxécuté la commande ```Update-Database```
    
## Démarrage
L'application démarre grâce à IIS Express qui est inclus dans Visual Studio par défaut ou avec un "véritable" IIS. L'identification Windows doit être activée.    


## Fabriqué avec
* [Visual Qtudio 2019](https://visualstudio.microsoft.com/fr/) - IDE version Community spécialisé en C#
* [SQL Server](https://www.microsoft.com/fr-fr/sql-server/) - Base de données de Microsoft

## Auteurs
* **Dimitri Dubois** _alias_ [@Dim145](https://github.com/Dim145)

