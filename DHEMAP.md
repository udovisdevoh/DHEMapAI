# Spécification du Format DHEMap

**Version : 1.0**

DHEMap est un format de fichier basé sur JSON, conçu pour représenter de manière lisible et structurée une carte (map) pour les jeux utilisant le moteur de Doom (Doom, Doom 2, Heretic, Hexen). Il vise à être une représentation logique de la carte, facile à éditer et à parser, avant une éventuelle compilation vers un format binaire comme `.WAD`.

## Concepts de Base

Une carte dans le moteur de Doom est définie par plusieurs composants interdépendants :

-   **Vertices**: Des points 2D (x, y) qui sont les "coins" de la carte.
-   **Linedefs**: Des lignes qui connectent deux `vertices`. Elles forment les murs et définissent les limites des zones.
-   **Sidedefs**: Les faces d'un `linedef`. Chaque `linedef` a au moins une face avant (`front`), et une face arrière (`back`) s'il est transparent ou traversable. C'est ici que les textures des murs sont définies.
-   **Sectors**: Des polygones fermés délimités par un ensemble de `linedefs`. Chaque secteur a sa propre hauteur de sol et de plafond, ses textures, et son niveau de luminosité.
-   **Things**: Des objets placés sur la carte, comme le joueur, les monstres, les armes, les bonus, etc.

## Structure du Fichier

Le fichier DHEMap est un objet JSON unique contenant les clés de haut niveau suivantes :

```json
{
  "format": "DHEMap",
  "version": "1.0",
  "mapInfo": { ... },
  "vertices": [ ... ],
  "linedefs": [ ... ],
  "sidedefs": [ ... ],
  "sectors": [ ... ],
  "things": [ ... ],
  "scripts": { ... }
}
```

### L'objet `mapInfo`

Contient les métadonnées de la carte.

| Clé          | Type   | Description                                                                          |
|--------------|--------|--------------------------------------------------------------------------------------|
| `game`       | Chaîne | Jeu cible : `"doom"`, `"doom2"`, `"heretic"`, ou `"hexen"`.                           |
| `episode`    | Entier | Numéro de l'épisode (pour Doom 1 & Heretic).                                         |
| `map`        | Entier | Numéro de la carte (ex: 1 pour E1M1, 7 pour MAP07).                                  |
| `name`       | Chaîne | Nom de la carte affiché en jeu.                                                      |
| `skyTexture` | Chaîne | Nom de la texture de ciel à utiliser (ex: `"SKY1"`).                                 |
| `music`      | Chaîne | Nom du "lump" de musique (ex: `"D_E1M1"`).                                           |

### Le tableau `vertices`

Chaque objet du tableau représente un sommet 2D.

| Clé  | Type   | Description                      |
|------|--------|----------------------------------|
| `id` | Entier | Identifiant unique du vertex.    |
| `x`  | Nombre | Coordonnée sur l'axe X.          |
| `y`  | Nombre | Coordonnée sur l'axe Y.          |

### Le tableau `linedefs`

Chaque objet représente une ligne de mur.

| Clé            | Type                | Description                                                                                                   |
|----------------|---------------------|---------------------------------------------------------------------------------------------------------------|
| `id`           | Entier              | Identifiant unique du linedef.                                                                                |
| `startVertex`  | Entier              | ID du `vertex` de départ.                                                                                     |
| `endVertex`    | Entier              | ID du `vertex` de fin.                                                                                        |
| `flags`        | Tableau de Chaînes  | Propriétés comme `"impassable"`, `"twoSided"`, `"blockMonsters"`, `"playerUse"`.                              |
| `action`       | Objet               | Action pour Doom/Heretic : `{ "special": Entier, "tag": Entier }`.                                            |
| `hexenArgs`    | Tableau d'entiers   | Pour Hexen, les 5 arguments de l'action.                                                                      |
| `frontSidedef` | Entier              | ID du `sidedef` avant.                                                                                        |
| `backSidedef`  | Entier ou `null`    | ID du `sidedef` arrière. `null` si la ligne n'a qu'un seul côté.                                               |

### Le tableau `sidedefs`

Définit l'apparence d'un côté de `linedef`.

| Clé             | Type   | Description                                                                            |
|-----------------|--------|----------------------------------------------------------------------------------------|
| `id`            | Entier | Identifiant unique du sidedef.                                                         |
| `offsetX`       | Nombre | Décalage horizontal de la texture.                                                     |
| `offsetY`       | Nombre | Décalage vertical de la texture.                                                       |
| `textureTop`    | Chaîne | Texture pour la partie supérieure du mur. `"-"` si absente.                            |
| `textureMiddle` | Chaîne | Texture principale du mur. `"-"` si absente.                                           |
| `textureBottom` | Chaîne | Texture pour la partie inférieure du mur. `"-"` si absente.                            |
| `sector`        | Entier | ID du `sector` auquel ce sidedef appartient (le secteur "devant" cette surface).         |

### Le tableau `sectors`

Définit une zone polygonale de la carte.

| Clé              | Type   | Description                                                                        |
|------------------|--------|------------------------------------------------------------------------------------|
| `id`             | Entier | Identifiant unique du secteur.                                                     |
| `floorHeight`    | Nombre | Hauteur du sol.                                                                    |
| `ceilingHeight`  | Nombre | Hauteur du plafond.                                                                |
| `floorTexture`   | Chaîne | Nom de la texture du sol (un "flat").                                              |
| `ceilingTexture` | Chaîne | Nom de la texture du plafond. Peut être `F_SKY1` pour un ciel.                     |
| `lightLevel`     | Entier | Niveau de luminosité (0-255).                                                      |
| `special`        | Entier | Type d'effet spécial du secteur (ex: 9 pour une porte secrète qui s'ouvre vite).   |
| `tag`            | Entier | Identifiant utilisé par un `linedef` pour affecter ce secteur (ex: pour une porte).|

### Le tableau `things`

Représente tous les objets interactifs ou décoratifs.

| Clé         | Type               | Description                                                                          |
|-------------|--------------------|--------------------------------------------------------------------------------------|
| `id`        | Entier             | Identifiant unique de l'objet.                                                       |
| `x`, `y`    | Nombres            | Coordonnées de l'objet.                                                              |
| `angle`     | Nombre             | Angle de l'objet en degrés (0-359).                                                  |
| `type`      | Entier             | Numéro de type de l'objet (diffère entre les jeux).                                  |
| `flags`     | Tableau de Chaînes | Propriétés comme `"skillEasy"`, `"skillNormal"`, `"skillHard"`, `"ambush"`.          |
| `hexenArgs` | Tableau d'entiers  | Pour Hexen, les 5 arguments spécifiques à l'objet.                                   |

### L'objet `scripts` (Optionnel)

Principalement pour Hexen, contient le code ACS (Action Code Script).

| Clé      | Type               | Description                                            |
|----------|--------------------|--------------------------------------------------------|
| `type`   | Chaîne             | Langage de script (ex: `"ACS"`).                       |
| `source` | Tableau de Chaînes | Le code source du script, une ligne par élément du tableau. |

## Exemple de Fichier

```json
{
  "format": "DHEMap",
  "version": "1.0",
  "mapInfo": {
    "game": "doom",
    "episode": 1,
    "map": 1,
    "name": "Hangar d'Entrée",
    "skyTexture": "SKY1",
    "music": "D_E1M1"
  },
  "vertices": [
    { "id": 0, "x": 0, "y": 0 },
    { "id": 1, "x": 256, "y": 0 },
    { "id": 2, "x": 256, "y": 256 },
    { "id": 3, "x": 0, "y": 256 }
  ],
  "linedefs": [
    { "id": 0, "startVertex": 0, "endVertex": 1, "flags": ["impassable"], "frontSidedef": 0 },
    { "id": 1, "startVertex": 1, "endVertex": 2, "flags": ["impassable"], "frontSidedef": 1 },
    { "id": 2, "startVertex": 2, "endVertex": 3, "flags": ["impassable"], "frontSidedef": 2 },
    { "id": 3, "startVertex": 3, "endVertex": 0, "flags": ["impassable"], "frontSidedef": 3 }
  ],
  "sidedefs": [
    { "id": 0, "offsetX": 0, "offsetY": 0, "textureMiddle": "STARTAN2", "sector": 0 },
    { "id": 1, "offsetX": 0, "offsetY": 0, "textureMiddle": "STARTAN2", "sector": 0 },
    { "id": 2, "offsetX": 0, "offsetY": 0, "textureMiddle": "STARTAN2", "sector": 0 },
    { "id": 3, "offsetX": 0, "offsetY": 0, "textureMiddle": "STARTAN2", "sector": 0 }
  ],
  "sectors": [
    {
      "id": 0,
      "floorHeight": 0,
      "ceilingHeight": 128,
      "floorTexture": "FLOOR4_8",
      "ceilingTexture": "CEIL3_1",
      "lightLevel": 160,
      "special": 0,
      "tag": 0
    }
  ],
  "things": [
    { "id": 0, "x": 128, "y": 128, "angle": 90, "type": 1, "flags": ["skillEasy", "skillNormal", "skillHard"] }
  ]
}
```
