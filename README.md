# ASM Search

Le but du projet est de simplifier la vie de tous les fans de séries/animées en simplifiant la recherche du site du streaming et/ou de téléchargement. <br />
Il suffit d'exécuter une recherche pour que le serveur recherche automatiquement sur un certain nombre d'autres sites la même recherche.
Le nombre de résultats trouver sur chaque site, la date de sortie, les genres et d'autres informations seront donnés par le site.

<p align="center">
    <img src="/AnimeSearch/wwwroot/ressources/images/full-logo.svg"  height="100" title="Logo" alt="Logo introuvable" />
</p>
    
### Branche Blazor
Reprise du coté client du site afin de l'améliorer. Utilisation de Blazor pour le rendu des pages, toujours avec un peu de JS.

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

- **ASP.NET 6** (6.0.100 ou >)
- **SQL Serveur** en tant que base de données
- Pour le développement, **Visual Studio 2022** est vivement recommandé
- Un serveur mail accessible via smtp comme **[Gmail](https://www.google.com/intl/fr/gmail/about/)** ou **[Mail.fr](https://mail.fr)**
- Un compte **[Discord](https://discord.com/)** développeur pour créer un bot.
- Un compte **[Télégram](https://web.telegram.org/)** pour configurer un bot..

### Installation

Les seules installations à effectuer sont celles des programmes/logiciels du dessus.

Mais il faut configurer certaines données dans le fichier appsettings.json:  

- SQLServer: chaine de connexion à la base de données.
- themoviedb_api: clé d'accès à la base de données en ligne [The Movie DB](https://www.themoviedb.org/) qui est gratuite mais obligatoire.
- Les données du serveur mail. Dans celle-ci, il y as deux adresses à renseigner:
  - le "Mail" correspond au from et est l'adresse affilier au compte avec le mdp.
  - La "Destination" est l'adresse à laquelle envoyer les email. Elle peut être la même que la valeur de "Mail"
- Un e-mail correspondant à un compte [Paypal](https://www.paypal.com/fr/home) sur lequel des éventuels **dons** seront crédités.
- Il faut créer la base de données si elle n'existe pas encore.

#### Les données suivantes sont optionnelles

- Créer une application puis un bot sur le site [développeur de Discord](https://discord.com/developers/applications):
    - Une fois une application créée, vous pouvez créer un bot dans l'onglet de droite correspondante.
    - Une fois le bot crée, il faut copier le "token" d'identification du bot dans le fichier "appsettings.json"
    - Libre à vous de customiser le bot après cela.
- Si besoin, créez-vous un compte Telegram pour ensuite créer un Bot:
    - Pour créer et configurer un bot, il faut communiquer avec [BotFather](https://t.me/botfather)
    - Lors de la création, BotFather vous donneras un token qu'il faudra copier dans "appsettings Json
    - La commande `/help` vous aideras pour les configurations annexes.
    
## Démarrage
L'application démarre grâce à IIS Express qui est inclus dans Visual Studio par défaut ou avec un "véritable" IIS.  
À chacun démarrage de l'application, les migrations sont effectuées automatiquement s'il y en à en attente.  
De plus, à chaque démarrage, des données sont insérées de base dans la BDD, si elles n'existent pas. Ces données se trouvent dans le fichier [FirstStartup](/AnimeSearch/FirstStartup.cs)

## Fabriqué avec
* [Visual Qtudio 2022](https://visualstudio.microsoft.com/fr/) - IDE version Community spécialisé en C#
* [SQL Server](https://www.microsoft.com/fr-fr/sql-server/) - Base de données de Microsoft

## Auteurs
* **Dimitri Dubois** _alias_ [@Dim145](https://github.com/Dim145)

