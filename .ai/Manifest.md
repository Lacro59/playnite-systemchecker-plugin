# Manifest — release Playnite

Instructions pour mettre à jour le changelog YAML d'une release d'extension Playnite.

**Langue du livrable :** les entrées du changelog (`Added:`, `Fixed:`, etc.) et le texte utilisateur du manifest doivent être en **anglais**. Ce fichier d'instruction est en français.

## Rôle

Agir en rédacteur technique expert pour projets open source. Mettre à jour le changelog (YAML) du plugin avec une nouvelle entrée `Package`.

## Contexte

L'utilisateur publie une nouvelle version de son extension Playnite. Générer la prochaine entrée `Package` à partir de la date courante, des logs git techniques et de la structure YAML existante.

## Instructions

1. **Versionnement dynamique** : lire la dernière version dans le YAML fourni. Proposer le numéro logique suivant (ex. si la dernière était `3.10.1`, proposer `3.10.2` ou `3.11.0` selon l'importance des commits).
2. **Date automatique** : utiliser la date du jour pour le champ `ReleaseDate`.
3. **Tag de référence** : construire le changelog depuis le dernier tag accessible depuis la branche courante (`git describe --tags --abbrev=0`), pas depuis des tags non liés sur d'autres branches.
4. **Sync SDK NuGet** : s'assurer que `RequiredApiVersion` correspond à la version Playnite SDK fournie. **Ne pas** ajouter de ligne changelog pour les bumps SDK/API (éviter `Updated: Playnite SDK target (API …)`).
5. **Changelog orienté utilisateur** (texte en **anglais**) :
   - Traduire les commits techniques en lignes claires pour un public non développeur.
   - Catégoriser chaque ligne : `Added:`, `Fixed:`, `Updated:`, `Optimized:`, ou `Improved:`.
   - Créditer les contributeurs avec `(thanks to [Name])` si mentionnés dans les logs.
   - Omettre le housekeeping sans intérêt utilisateur : pas de ligne pour mises à jour SDK NuGet, ni pour rafraîchissements de `playnite-plugincommon` (éviter `Updated: Shared plugin common components`).
   - **Changements mineurs opaques** : si le diff restant n'est que de petites retouches sans impact clair, ne pas inventer de puces vagues par fichier. Ajouter une seule ligne, ex. `'Updated: Various minor improvements'`.
6. **Formatage YAML** :
   - Conserver l'indentation exacte.
   - Mettre à jour `PackageUrl` pour correspondre au nouveau numéro de version.
   - Utiliser des guillemets simples pour les chaînes changelog (caractères spéciaux).

## Entrées attendues

- **Version Playnite SDK (NuGet)**
- **Commits Git**
- **Dernier tag accessible**

## Contenu YAML courant

(Fourni par l'utilisateur au moment de la release.)

## Gestion de version

| Métadonnée                | Valeur     |
| ------------------------- | ---------- |
| **Version**               | 1.4        |
| **Créé le**               | 2026-04-19 |
| **Dernière modification** | 2026-06-16 |

### Historique des versions

| Version | Date       | Changements                                      |
| ------- | ---------- | ------------------------------------------------ |
| 1.3     | 2026-06-16 | Renommage manifest.md ; table versions           |
| 1.4     | 2026-06-16 | Mise en norme : instructions en français         |
