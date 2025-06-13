# Spécification du Format D-Graph

**Version : 1.5**

D-Graph est un format JSON de haut niveau pour la conception de cartes de jeux basés sur le moteur de Doom. Il modélise une carte comme un **graphe**, où les **pièces (`rooms`) sont les nœuds** et les **connexions (`connections`) sont les arêtes**.

La version 1.5 introduit le **système de tags**, permettant aux `features` (comme les interrupteurs) d'affecter des `rooms` spécifiques (comme des portes ou des plateformes).

## Philosophie

-   **Abstraction Géométrique :** Décrire l'intention ("une grande pièce octogonale") plutôt que les coordonnées des sommets.
-   **Focus sur la Topologie :** Spécifier quelles pièces sont connectées et comment.
-   **Fonction vs. Style :** Le **concept** dans la palette (la clé, ex: `wall_primary`) définit la **fonction** de la surface, tandis que la **liste des textures** associées définit le **style** visuel.

## Structure du Fichier

```json
{
  "format": "D-Graph",
  "version": "1.5",
  "mapInfo": { ... },
  "themePalette": { ... },
  "rooms": [ ... ],
  "connections": [ ... ]
}
```

### 1. OBJET RACINE
- `format`: String, REQUIS. Doit être "D-Graph".
- `version`: String, REQUIS. Doit être "1.5".
- `mapInfo`: Object, REQUIS. Voir section 1.1.
- `themePalette`: Object, REQUIS. Voir section 1.2.
- `rooms`: Array<Object>, REQUIS. Voir section 1.3.
- `connections`: Array<Object>, REQUIS. Voir section 1.4.

### 1.1. Spécification de `mapInfo`
- `game`: String, REQUIS. Enum: "doom", "doom2", "heretic", "hexen".
- `map`: Integer, REQUIS. Numéro de la carte.
- `name`: String, REQUIS. Nom de la carte.
- `music`: String, REQUIS. Nom du lump de musique (ex: "D_RUNNIN").

### 1.2. Spécification de `themePalette`
Définit le vocabulaire visuel. Chaque clé est un concept fonctionnel, et la valeur est un tableau de textures pondérées `{ "name": String, "weight": Integer }`.
**Concepts de Palette (liste exhaustive et fixe) :**
- **Murs :** `wall_primary`, `wall_accent`, `wall_support`, `wall_secret_indicator`, `wall_panel`.
- **Sols/Plafonds :** `floor_primary`, `floor_accent`, `floor_damage_low`, `floor_damage_high`, `ceiling_primary`, `ceiling_light_source`, `ceiling_crusher`, `platform_surface`.
- **Portes/Passages :** `door_regular`, `door_locked`, `door_exit`, `door_frame`, `stair_riser`, `window_grate`.
- **Interrupteurs :** `switch_utility`, `switch_exit`, `switch_panel_wall`.
- **Mécaniques :** `teleporter_pad`.

### 1.3. Spécification de `room`
Décrit une zone ou un secteur de la carte.
- `id`: String, REQUIS. Identifiant unique (ex: "salle_de_controle").
- `parentRoom`: String | null, REQUIS. ID de la pièce parente si imbriquée, sinon `null`.
- `shapeHint`: Object, REQUIS. Contient : `{ "vertices": Integer, "description": String }`.
- `properties`: Object, REQUIS. Contient :
  - `floor`, `ceiling`: String, REQUIS. Enum: "very_low", "low", "normal", "high", "very_high", "sky".
  - `lightLevel`: String, REQUIS. Enum: "very_dark", "dark", "normal", "bright", "flickering", "strobe".
  - `wallTexture`, `floorFlat`, `ceilingFlat`: String, REQUIS. Doivent correspondre à une clé de la `themePalette`.
  - `tag`: Integer | null, OPTIONNEL. Identifiant numérique unique pour que cette pièce soit ciblée par une action.
- `contents`: Object, REQUIS. Contient les tableaux `monsters`, `items`, `decorations`. Chaque élément est un objet `{ "name": String, "typeId": Integer, "count": Integer }`. Le `typeId` est le numéro de "thing" du jeu.
- `features`: Array<Object>, REQUIS. Décrit les actions sur les murs. Chaque élément est un objet `{ "name": String, "actionId": Integer, "count": Integer, "properties": Object }`. L'`actionId` est le numéro de "spécial" du linedef.
  - Les `properties` d'une feature peuvent contenir un `targetTag`: Integer, OPTIONNEL, qui spécifie le `tag` de la pièce à affecter.

### 1.4. Spécification de `connection`
Décrit un lien entre deux pièces.
- `fromRoom`, `toRoom`: String, REQUIS. IDs des pièces connectées.
- `type`: String, REQUIS. Enum: "opening", "door", "locked_door", "secret_door", "window", "teleporter".
- `properties`: Object, REQUIS. Détails (ex: `{ "texture": "DOORBLU", "key": "blue" }`).

### 1.5. Exemple Canonique de Structure v1.5
Cet exemple montre une `salle_de_controle` avec un interrupteur (`feature`) qui cible le `tag: 1`. La pièce `plateforme_mobile` possède ce `tag: 1` et sera donc affectée par l'interrupteur.

```json
{
  "format": "D-Graph",
  "version": "1.5",
  "mapInfo": { "game": "doom2", "map": 1, "name": "Le Complexe Interactif", "music": "D_SHAWN" },
  "themePalette": {
    "wall_primary": [{ "name": "STARG1", "weight": 100 }],
    "wall_panel": [{ "name": "COMPTALL", "weight": 100 }],
    "floor_primary": [{ "name": "FLAT19", "weight": 100 }],
    "ceiling_primary": [{ "name": "CEIL1_3", "weight": 100 }],
    "platform_surface": [{ "name": "PLAT1", "weight": 100 }],
    "switch_utility": [{ "name": "SW1COMM", "weight": 100 }]
  },
  "rooms": [
    {
      "id": "start_room", "parentRoom": null,
      "shapeHint": { "vertices": 4, "description": "Pièce de départ." },
      "properties": { "floor": "normal", "ceiling": "normal", "lightLevel": "normal", "wallTexture": "wall_primary", "floorFlat": "floor_primary", "ceilingFlat": "ceiling_primary", "tag": null },
      "contents": { "items": [{ "name": "Player1Start", "typeId": 1, "count": 1 }] },
      "features": []
    },
    {
      "id": "salle_de_controle", "parentRoom": null,
      "shapeHint": { "vertices": 4, "description": "Salle de contrôle avec un interrupteur." },
      "properties": { "floor": "high", "ceiling": "normal", "lightLevel": "bright", "wallTexture": "wall_panel", "floorFlat": "floor_primary", "ceilingFlat": "ceiling_primary", "tag": null },
      "contents": { "monsters": [{ "name": "Zombieman", "typeId": 3004, "count": 2 }] },
      "features": [
        { 
          "name": "LiftSwitch", "actionId": 18, "count": 1,
          "properties": { "texture": "SW1COMM", "targetTag": 1 } 
        }
      ]
    },
    {
      "id": "plateforme_mobile", "parentRoom": null,
      "shapeHint": { "vertices": 4, "description": "Plateforme servant d'ascenseur." },
      "properties": {
        "floor": "low", "ceiling": "normal", "lightLevel": "normal",
        "wallTexture": "wall_primary", "floorFlat": "platform_surface", "ceilingFlat": "ceiling_primary",
        "tag": 1
      },
      "contents": {},
      "features": []
    }
  ],
  "connections": [
    { "fromRoom": "start_room", "toRoom": "plateforme_mobile", "type": "opening", "properties": {} },
    { "fromRoom": "start_room", "toRoom": "salle_de_controle", "type": "opening", "properties": {"description": "Un petit escalier monte vers la salle de contrôle."} }
  ]
}
```