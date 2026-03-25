# Migrer depuis MyGet

Ce guide explique comment migrer tous vos packages NuGet d'un feed MyGet vers BaGetter.

La stratégie consiste à lister chaque version disponible sur MyGet, télécharger le `.nupkg`
correspondant, puis le pousser vers BaGetter via l'API NuGet standard.

---

## Pré-requis

| Outil | Vérification |
|-------|-------------|
| [`nuget.exe`](https://www.nuget.org/downloads) dans le PATH | `nuget help` |
| BaGetter démarré et accessible | `curl http://localhost:5000/v3/index.json` |
| URL de votre feed MyGet (format v2) | Disponible dans *MyGet > Feed Settings* |
| Clé API MyGet | *MyGet > Feed Settings > Access Tokens* |
| Clé API BaGetter | Définie dans `appsettings.json → ApiKey` |

---

## Étape 1 — Configurer BaGetter

Avant la migration, assurez-vous que BaGetter est configuré avec la bonne base de données
et que les options suivantes sont définies dans `appsettings.json` :

```json
{
  "ApiKey": "VOTRE_CLE_BAGETTER",
  "AllowPackageOverwrites": false,
  "PackageDeletionBehavior": "Unlist",
  "MaxPackageSizeGiB": 8,
  "Database": {
    "Type": "PostgreSql",
    "ConnectionString": "Host=localhost;Database=bagetter;Username=bagetter;Password=SECRET"
  },
  "Storage": {
    "Type": "FileSystem",
    "Path": "C:\\bagetter\\packages"
  }
}
```

> [!TIP]
> **Retention policies** — Désactivez les politiques de rétention (`Retention` dans la config) pendant la migration
> pour ne supprimer aucune version automatiquement.

Au premier démarrage, BaGetter applique automatiquement les migrations de base de données.

---

## Étape 2 — Lancer le script de migration

Le script PowerShell `scripts/migration/migrate-myget.ps1` automatise les étapes de
téléchargement et de push.

### Utilisation de base

```powershell
.\scripts\migration\migrate-myget.ps1 `
    -MyGetFeedUrl   "https://www.myget.org/F/MON-FEED/api/v2" `
    -MyGetApiKey    "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" `
    -BaGetterUrl    "http://localhost:5000/v3/index.json" `
    -BaGetterApiKey "MA_CLE_BAGETTER"
```

Remplacez :

- `MON-FEED` par le nom de votre feed MyGet
- `xxxxxxxx-...` par votre clé API MyGet
- `MA_CLE_BAGETTER` par la valeur de `ApiKey` dans votre `appsettings.json`

### Ce que fait le script

1. Liste toutes les versions (y compris pré-releases) via `nuget list -AllVersions -PreRelease`
2. Pour chaque version, télécharge le `.nupkg` depuis MyGet
3. Pousse le `.nupkg` vers BaGetter avec `-SkipDuplicate` (idempotent : les doublons sont ignorés)
4. Écrit les succès dans `%TEMP%\myget-migration\success.log`
5. Écrit les échecs dans `%TEMP%\myget-migration\errors.log`

### Reprendre après une interruption

Si le script est interrompu ou si des packages ont échoué, relancez-le avec `-ReplayErrors` :

```powershell
.\scripts\migration\migrate-myget.ps1 `
    -MyGetFeedUrl   "https://www.myget.org/F/MON-FEED/api/v2" `
    -MyGetApiKey    "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" `
    -BaGetterUrl    "http://localhost:5000/v3/index.json" `
    -BaGetterApiKey "MA_CLE_BAGETTER" `
    -ReplayErrors
```

Le flag `-ReplayErrors` lit le fichier `errors.log` généré lors de la précédente exécution
et tente uniquement les packages qui avaient échoué.

### Dossier temporaire personnalisé

Par défaut, les `.nupkg` sont téléchargés dans `%TEMP%\myget-migration`.
Pour utiliser un autre emplacement (ex. disque plus rapide) :

```powershell
.\scripts\migration\migrate-myget.ps1 `
    -MyGetFeedUrl   "https://www.myget.org/F/MON-FEED/api/v2" `
    -MyGetApiKey    "..." `
    -BaGetterUrl    "http://localhost:5000/v3/index.json" `
    -BaGetterApiKey "..." `
    -TempDir        "D:\migration-temp"
```

---

## Étape 3 — Vérifier la migration

### Compter les packages importés

```http
GET http://localhost:5000/v3/search/query?q=&take=0
```

La réponse JSON contient `totalHits` : le nombre total de versions indexées.

### Vérifier un package spécifique

```http
GET http://localhost:5000/v3/registration/NomDuPackage/index.json
```

### Vérifier les logs du script

```powershell
# Nombre de succès
(Get-Content "$env:TEMP\myget-migration\success.log").Count

# Packages en erreur
Get-Content "$env:TEMP\myget-migration\errors.log"
```

---

## Étape 4 — Reconfigurer vos clients NuGet

Une fois la migration validée, mettez à jour votre `nuget.config`
(Visual Studio, `dotnet restore`, CI/CD) pour pointer vers BaGetter :

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="BaGetter" value="http://localhost:5000/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <BaGetter>
      <add key="Username" value="unused" />
      <add key="ClearTextPassword" value="MA_CLE_BAGETTER" />
    </BaGetter>
  </packageSourceCredentials>
</configuration>
```

> [!NOTE]
> Si votre BaGetter n'a pas de clé API configurée (lecture publique), vous pouvez omettre
> la section `packageSourceCredentials`.

---

## Limitations connues

- **Statistiques de téléchargements** : les compteurs de downloads MyGet ne sont pas migrés
  (MyGet ne les expose pas via l'API NuGet).
- **Packages supprimés (unlisted)** : `nuget list` ne retourne pas les packages délistés sur MyGet.
  Si vous avez besoin de les migrer, exportez-les manuellement depuis l'interface MyGet.
- **Symboles** : ce guide couvre uniquement les feeds NuGet (`.nupkg`).
  Pour les feeds de symboles (`.snupkg`), consultez la [documentation Docker — Symbol server](../installation/docker.md#symbol-server).
