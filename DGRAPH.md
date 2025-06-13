# Sp�cification du Format D-Graph

**Version : 1.5**

D-Graph est un format JSON de haut niveau pour la conception de cartes de jeux bas�s sur le moteur de Doom. Il mod�lise une carte comme un **graphe**, o� les **pi�ces (`rooms`) sont les n�uds** et les **connexions (`connections`) sont les ar�tes**.

La version 1.5 introduit le **syst�me de tags**, permettant aux `features` (comme les interrupteurs) d'affecter des `rooms` sp�cifiques (comme des portes ou des plateformes).

## Philosophie

-   **Abstraction G�om�trique :** D�crire l'intention ("une grande pi�ce octogonale") plut�t que les coordonn�es des sommets.
-   **Focus sur la Topologie :** Sp�cifier quelles pi�ces sont connect�es et comment.
-   **Fonction vs. Style :** Le **concept** dans la palette (la cl�, ex: `wall_primary`) d�finit la **fonction** de la surface, tandis que la **liste des textures** associ�es d�finit le **style** visuel.

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
- `format`: String, REQUIS. Doit �tre "D-Graph".
- `version`: String, REQUIS. Doit �tre "1.5".
- `mapInfo`: Object, REQUIS. Voir section 1.1.
- `themePalette`: Object, REQUIS. Voir section 1.2.
- `rooms`: Array<Object>, REQUIS. Voir section 1.3.
- `connections`: Array<Object>, REQUIS. Voir section 1.4.

### 1.1. Sp�cification de `mapInfo`
- `game`: String, REQUIS. Enum: "doom", "doom2", "heretic", "hexen".
- `map`: Integer, REQUIS. Num�ro de la carte.
- `name`: String, REQUIS. Nom de la carte.
- `music`: String, REQUIS. Nom du lump de musique (ex: "D_RUNNIN").

### 1.2. Sp�cification de `themePalette`
D�finit le vocabulaire visuel. Chaque cl� est un concept fonctionnel, et la valeur est un tableau de textures pond�r�es `{ "name": String, "weight": Integer }`.
**Concepts de Palette (liste exhaustive et fixe) :**
- **Murs :** `wall_primary`, `wall_accent`, `wall_support`, `wall_secret_indicator`, `wall_panel`.
- **Sols/Plafonds :** `floor_primary`, `floor_accent`, `floor_damage_low`, `floor_damage_high`, `ceiling_primary`, `ceiling_light_source`, `ceiling_crusher`, `platform_surface`.
- **Portes/Passages :** `door_regular`, `door_locked`, `door_exit`, `door_frame`, `stair_riser`, `window_grate`.
- **Interrupteurs :** `switch_utility`, `switch_exit`, `switch_panel_wall`.
- **M�caniques :** `teleporter_pad`.

### 1.3. Sp�cification de `room`
D�crit une zone ou un secteur de la carte.
- `id`: String, REQUIS. Identifiant unique (ex: "salle_de_controle").
- `parentRoom`: String | null, REQUIS. ID de la pi�ce parente si imbriqu�e, sinon `null`.
- `shapeHint`: Object, REQUIS. Contient : `{ "vertices": Integer, "description": String }`.
- `properties`: Object, REQUIS. Contient :
  - `floor`, `ceiling`: String, REQUIS. Enum: "very_low", "low", "normal", "high", "very_high", "sky".
  - `lightLevel`: String, REQUIS. Enum: "very_dark", "dark", "normal", "bright", "flickering", "strobe".
  - `wallTexture`, `floorFlat`, `ceilingFlat`: String, REQUIS. Doivent correspondre � une cl� de la `themePalette`.
  - `tag`: Integer | null, OPTIONNEL. Identifiant num�rique unique pour que cette pi�ce soit cibl�e par une action.
- `contents`: Object, REQUIS. Contient les tableaux `monsters`, `items`, `decorations`. Chaque �l�ment est un objet `{ "name": String, "typeId": Integer, "count": Integer }`. Le `typeId` est le num�ro de "thing" du jeu.
- `features`: Array<Object>, REQUIS. D�crit les actions sur les murs. Chaque �l�ment est un objet `{ "name": String, "actionId": Integer, "count": Integer, "properties": Object }`. L'`actionId` est le num�ro de "sp�cial" du linedef.
  - Les `properties` d'une feature peuvent contenir un `targetTag`: Integer, OPTIONNEL, qui sp�cifie le `tag` de la pi�ce � affecter.

### 1.4. Sp�cification de `connection`
D�crit un lien entre deux pi�ces.
- `fromRoom`, `toRoom`: String, REQUIS. IDs des pi�ces connect�es.
- `type`: String, REQUIS. Enum: "opening", "door", "locked_door", "secret_door", "window", "teleporter".
- `properties`: Object, REQUIS. D�tails (ex: `{ "texture": "DOORBLU", "key": "blue" }`).

### 1.5. Exemple Canonique de Structure v1.5
Cet exemple montre une `salle_de_controle` avec un interrupteur (`feature`) qui cible le `tag: 1`. La pi�ce `plateforme_mobile` poss�de ce `tag: 1` et sera donc affect�e par l'interrupteur.

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
      "shapeHint": { "vertices": 4, "description": "Pi�ce de d�part." },
      "properties": { "floor": "normal", "ceiling": "normal", "lightLevel": "normal", "wallTexture": "wall_primary", "floorFlat": "floor_primary", "ceilingFlat": "ceiling_primary", "tag": null },
      "contents": { "items": [{ "name": "Player1Start", "typeId": 1, "count": 1 }] },
      "features": []
    },
    {
      "id": "salle_de_controle", "parentRoom": null,
      "shapeHint": { "vertices": 4, "description": "Salle de contr�le avec un interrupteur." },
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
    { "fromRoom": "start_room", "toRoom": "salle_de_controle", "type": "opening", "properties": {"description": "Un petit escalier monte vers la salle de contr�le."} }
  ]
}
```