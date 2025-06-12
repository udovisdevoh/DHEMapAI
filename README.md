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

# Spécification du Format D-Graph

**Version : 1.3**

D-Graph est un format JSON de haut niveau pour la conception de cartes de jeux basés sur le moteur de Doom. Il modélise une carte comme un **graphe**, où les **pièces (`rooms`) sont les nœuds** et les **connexions (`connections`) sont les arêtes**.

La version 1.3 affine le concept de **Palette Thématique (`themePalette`)** pour créer une distinction claire entre la **fonction** d'une surface et son **style** visuel, et standardise les identifiants de concepts en anglais.

## Philosophie

-   **Abstraction Géométrique :** Décrire l'intention ("une grande pièce octogonale") plutôt que les coordonnées des sommets.
-   **Focus sur la Topologie :** Spécifier quelles pièces sont connectées et comment.
-   **Fonction vs. Style :** Le **concept** dans la palette (la clé, ex: `wall_primary`) définit la **fonction** de la surface. La **liste des textures** associées à cette clé définit le **style** visuel (pierre, métal, bois, etc.).

## Structure du Fichier

```json
{
  "format": "D-Graph",
  "version": "1.3",
  "mapInfo": { ... },
  "themePalette": { ... },
  "rooms": [ ... ],
  "connections": [ ... ]
}
```

### L'objet `themePalette`

Définit l'ensemble des textures et "flats" à utiliser, groupés par concept fonctionnel. Chaque concept est une clé dont la valeur est un tableau d'objets texturés pondérés.

-   `name`: Le nom réel de la texture ou du flat (ex: `"STARG1"`).
-   `weight`: Un poids numérique qui représente la probabilité relative d'utilisation de cette texture pour ce concept.

#### Concepts de Palette Suggérés

**Murs (Walls)**
| Concept | Description |
|---|---|
| `wall_primary` | Texture principale pour les murs intérieurs/extérieurs. |
| `wall_accent` | Texture pour un mur d'accentuation, une section différente. |
| `wall_support` | Pour les piliers, contreforts, et autres structures de support. |
| `wall_secret_indicator` | Texture pour un mur secret, souvent subtilement différente. |
| `wall_panel` | Pour les panneaux de contrôle, les écrans, les terminaux. |

**Sols et Plafonds (Floors & Ceilings)**
| Concept | Description |
|---|---|
| `floor_primary` | Flat principal pour les sols. |
| `floor_accent` | Flat pour une zone de sol différente (ex: un tapis, une estrade). |
| `floor_damage_low` | Sol causant des dégâts légers (ex: Nukage, 10% de dégâts). |
| `floor_damage_high` | Sol causant des dégâts élevés (ex: Lave, 20% de dégâts). |
| `floor_heal` | Sol qui rend de la vie (rare, spécifique à certains mods/jeux). |
| `ceiling_primary` | Flat principal pour les plafonds. |
| `ceiling_light_source`| Flat pour les luminaires encastrés dans le plafond. |
| `ceiling_crusher` | Texture pour un plafond broyeur. |
| `platform_surface` | Texture de la surface d'une plateforme mobile ou d'un ascenseur. |

**Portes et Passages (Doors & Passages)**
| Concept | Description |
|---|---|
| `door_regular` | Texture pour une porte standard. |
| `door_locked` | Pour les portes nécessitant une clé. |
| `door_exit` | Pour la porte finale qui termine le niveau. |
| `door_frame` | Texture pour le cadre ou le mécanisme autour d'une porte. |
| `stair_riser` | Texture pour la contremarche d'un escalier. |
| `window_grate` | Texture pour une fenêtre, une grille ou des barreaux. |

**Interrupteurs et Mécanismes (Switches & Mechanisms)**
| Concept | Description |
|---|---|
| `switch_utility` | Interrupteur standard pour activer portes, ascenseurs, etc. |
| `switch_exit` | Interrupteur qui termine le niveau. |
| `switch_panel_wall` | Texture du mur sur lequel un interrupteur est généralement placé. |

**Mécaniques Spéciales (Special Mechanics)**
| Concept | Description |
|---|---|
| `teleporter_pad` | Flat pour la surface d'un téléporteur. |

### Le tableau `rooms` (Mis à Jour)

Les `properties` des pièces font maintenant référence aux concepts en anglais de la `themePalette`.

| Clé            | Type   | Description                                                        |
|----------------|--------|--------------------------------------------------------------------|
| `wallTexture`  | Chaîne | Référence à un concept de mur (ex: `"wall_primary"`).              |
| `floorFlat`    | Chaîne | Référence à un concept de sol (ex: `"floor_primary"`).             |
| `ceilingFlat`  | Chaîne | Référence à un concept de plafond (ex: `"ceiling_primary"`).       |
| ...autres propriétés... |

*(Les autres sections de la documentation : `mapInfo`, `contents`, `features`, `connections` restent les mêmes.)*

Exemple de fichier:

```json
{
  "format": "D-Graph",
  "version": "1.3",
  "mapInfo": {
    "game": "doom",
    "episode": 1,
    "map": 1,
    "name": "La Base Thématique",
    "music": "D_E1M1"
  },
  "themePalette": {
    "wall_primary": [
      { "name": "STARG1", "weight": 70 },
      { "name": "STARG2", "weight": 20 },
      { "name": "STARGR1", "weight": 10 }
    ],
    "wall_panel": [
      { "name": "COMPTALL", "weight": 100 }
    ],
    "wall_secret_indicator": [
      { "name": "SUPPORT3", "weight": 100 }
    ],
    "floor_primary": [
      { "name": "FLOOR4_8", "weight": 90 },
      { "name": "FLAT19", "weight": 10 }
    ],
    "floor_damage_low": [
      { "name": "NUKAGE1", "weight": 75 },
      { "name": "NUKAGE2", "weight": 25 }
    ],
    "ceiling_primary": [
      { "name": "CEIL3_1", "weight": 100 }
    ],
    "ceiling_light_source": [
      { "name": "LIGHT3", "weight": 100 }
    ],
    "door_regular": [
      { "name": "DOOR3", "weight": 100 }
    ],
    "door_frame": [
      { "name": "DOORTRAK", "weight": 100 }
    ],
    "switch_exit": [
      { "name": "SW1EXIT", "weight": 100 }
    ],
    "switch_utility": [
      { "name": "SW1STAR", "weight": 100 }
    ]
  },
  "rooms": [
    {
      "id": "start_room",
      "parentRoom": null,
      "shapeHint": { "vertices": 4, "description": "Une petite pièce rectangulaire." },
      "properties": {
        "floor": "normal", "ceiling": "low", "lightLevel": "normal",
        "wallTexture": "wall_primary",
        "floorFlat": "floor_primary",
        "ceilingFlat": "ceiling_primary"
      },
      "contents": { "items": [ { "type": "Player1Start", "count": 1 } ] },
      "features": []
    },
    {
      "id": "sky_chamber",
      "parentRoom": null,
      "shapeHint": { "vertices": 8, "description": "Une grande cour octogonale." },
      "properties": {
        "floor": "normal", "ceiling": "sky", "lightLevel": "bright",
        "wallTexture": "wall_primary",
        "floorFlat": "floor_primary",
        "ceilingFlat": "sky"
      },
      "contents": { "monsters": [ { "type": "Imp", "count": 3 } ] },
      "features": [
        {
          "type": "ExitSwitch", "count": 1,
          "properties": { "texture": "SW1EXIT" }
        }
      ]
    }
  ],
  "connections": [
    {
      "fromRoom": "start_room",
      "toRoom": "sky_chamber",
      "type": "door",
      "properties": { "texture": "DOOR3" }
    }
  ]
}```
