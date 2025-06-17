# Spécification du Format D-Genesis
**Version : 1.0**

---

## 1. Introduction

**D-Genesis** est un format JSON de haut niveau servant de directive de conception pour la génération procédurale de cartes de type Doom. Plutôt que de décrire la géométrie de la carte, il spécifie les **contraintes créatives et thématiques** sous-jacentes.

Le format sépare la définition d'un thème général (`themePalette`) d'un système de règles contextuelles plus fin (`thematicTokens`) qui définit les relations entre les assets spécifiques.

Par la suite, les **"Tokens Thématiques"** et de leurs **"Règles d'Adjacence"**, qui fonctionnent de manière similaire à une chaîne de Markov pour définir des probabilités contextuelles d'apparition des textures, des sols et des objets.

---

## 2. Structure du Fichier

Le fichier D-Genesis est un objet JSON unique.

```json
{
  "format": "D-Genesis",
  "version": "1.0",
  "mapInfo": { ... },
  "generationParams": { ... },
  "themePalette": { ... },
  "thematicTokens": [ ... ]
}
```

### 2.1. `format` et `version` (Requis)
- `format`: Chaîne de caractères. Doit être `"D-Genesis"`.
- `version`: Chaîne de caractères. Doit être `"1.0"`.

### 2.2. L'objet `mapInfo` (Requis)
Contient les métadonnées de base de la carte à générer.

| Clé           | Type   | Description                                                     | Exemple              |
|---------------|--------|-----------------------------------------------------------------|----------------------|
| `game`        | Chaîne | Jeu cible. **Enum**: `"doom"`, `"doom2"`, `"heretic"`, `"hexen"`. | `"doom2"`            |
| `mapLumpName` | Chaîne | Nom du marqueur de la carte (ex: "E1M1", "MAP01").              | `"MAP01"`            |
| `name`        | Chaîne | Nom de la carte qui sera affiché en jeu.                        | `"The Iron Furnace"` |
| `music`       | Chaîne | Nom du *lump* de musique (ex: `"D_RUNNIN"`).                    | `"D_SHAWN"`          |

### 2.3. L'objet `generationParams` (Requis)
Définit les contraintes structurelles et de gameplay de haut niveau.

| Clé                         | Type             | Description                                                                     | Exemple |
|-----------------------------|------------------|---------------------------------------------------------------------------------|---------|
| `roomCount`                 | Entier           | Nombre approximatif de pièces principales à générer.                            | `15`    |
| `avgConnectivity`           | Nombre           | Nombre moyen de connexions par pièce. <2 est linéaire, >2.5 est très connecté.   | `2.8`   |
| `avgFloorHeightDelta`       | Entier           | Différence de hauteur de sol moyenne entre deux pièces connectées.              | `48`    |
| `avgHeadroom`               | Entier           | Épaisseur verticale moyenne d'un secteur.                                       | `128`   |
| `totalVerticalSpan`         | Entier           | Différence de hauteur maximale autorisée pour toute la carte.                   | `1024`  |
| `verticalTransitionProfile` | Tableau d'objets | Définit les probabilités des types de connexions verticales.                    | `[...]` |

#### L'objet `verticalTransitionProfile` (Requis)
Chaque objet du tableau `verticalTransitionProfile` contient :
- `type` (Chaîne): Le type de transition. **Enum**: `"level"` (plain-pied), `"step"` (escaliers), `"overlook"` (fenêtre/balcon), `"lift"` (ascenseur).
- `weight` (Entier): La probabilité relative de ce type de transition.

### 2.4. L'objet `themePalette` (Requis)
Cet objet définit une palette de concepts thématiques de haut niveau. Il sera utilisé par un générateur pour une sélection aléatoire pondérée d'assets de base, ou comme référence pour des algorithmes plus simples, fonctionnant indépendamment du système `thematicTokens`.

Chaque clé de cet objet est un **concept** (ex: `"wall_primary"`), et sa valeur est un tableau d'objets de textures pondérées (`{ "name": String, "weight": Integer }`).

#### Concepts de Palette Canoniques :
-   **Murs :** `wall_primary`, `wall_accent`, `wall_support`, `wall_secret_indicator`, `wall_panel`, `door_frame`.
-   **Sols/Plafonds :** `floor_primary`, `floor_accent`, `floor_damage_low`, `floor_damage_high`, `ceiling_primary`, `ceiling_light_source`, `platform_surface`.
-   **Apparences de Connexion/Action :** `door_regular`, `door_locked`, `door_exit`, `switch_utility`, `switch_exit`.
-   **Indicateurs de Clé :** `door_indicator_blue`, `door_indicator_red`, `door_indicator_yellow`.

### 2.5. Le tableau `thematicTokens` (Requis)
C'est le cœur du format. Il définit une liste d'assets spécifiques (textures, objets) et leurs règles d'adjacence contextuelles. Ce système permet un contrôle fin sur les relations entre les éléments de la carte.

#### L'objet Token
| Clé                | Type                 | Description                                                                                                                              |
|--------------------|----------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| `name`             | Chaîne               | Le nom de l'asset (texture, flat, ou nom de 'thing'). Doit être un nom valide pour le jeu cible.                                          |
| `type`             | Chaîne               | Le type de token. **Enum**: `"wall"`, `"flat"`, `"object"`, `"connection_action"`.                                                        |
| `typeId`           | Entier \| `null`     | Si le `type` est `"object"`, ceci est l'identifiant numérique canonique du 'thing'.                                                          |
| `actionInfo`       | Objet \| `null`      | Si le `type` est `"connection_action"`, cet objet contient les détails de l'action : `special` (Entier) et `properties` (Objet).         |                                                    |
| `adjacencyRules`   | Tableau d'objets     | Un tableau de règles qui modifient le poids du token en fonction de ses voisins.                                                         |

#### 2.5.1. L'objet AdjacencyRule
Définit comment la probabilité d'un token est affectée par la présence d'un autre token. Une règle doit contenir `modifier` et soit `adjacentTo`, soit `adjacentToTypeId`.

| Clé                | Type   | Description                                                                                             |
|--------------------|--------|---------------------------------------------------------------------------------------------------------|
| `modifier`         | Nombre | Le multiplicateur à appliquer à la probabilité de base. > 1 augmente la probabilité, < 1 la diminue.             |
| `adjacentTo`       | Chaîne | Cible un autre token par son `name` (utile pour murs/sols).                                             |
| `adjacentToTypeId` | Entier | Cible un token de type `object` par son `typeId` (utile pour 'things').                                  |

### 2.6. L'objet `sectorBehaviorPalette` (Requis)

Cet objet définit un ensemble de comportements de secteur qui peuvent être appliqués sur la carte. Il permet de contrôler la probabilité relative des différents effets spéciaux (lumière, dégâts, etc.) en leur associant un poids. Ce système peut être utilisé par un générateur pour attribuer des comportements de manière globale.

Chaque entrée dans la `sectorBehaviorPalette` est une clé représentant un nom de concept (ex: `"flickering_light"`) dont la valeur est un objet décrivant le comportement :

| Clé             | Type   | Description                                                                 |
|-----------------|--------|-----------------------------------------------------------------------------|
| `description`   | Chaîne | Une brève description lisible de l'effet pour clarifier son intention.      |
| `sectorSpecial` | Entier | L'identifiant numérique du "special" de secteur, basé sur le jeu cible.     |
| `weight`        | Entier | La probabilité relative de cet effet d'être choisi pour un secteur donné. |

### 2.7. L'objet `featurePalette` (Optionnel)

Cet objet définit un "budget" de probabilités pour les mécaniques de jeu et les structures complexes. Le générateur peut utiliser les poids (`weight`) relatifs de cette palette pour déterminer la fréquence et le type des "features" à implémenter sur la carte. Ce système remplace le paramètre `secretRoomPercentage`.

Chaque entrée dans la `featurePalette` est une clé représentant le concept, et une valeur qui est un objet décrivant la feature :

| Clé           | Type   | Description                                                           |
|---------------|--------|-----------------------------------------------------------------------|
| `description` | Chaîne | Une brève description lisible du concept pour clarifier son intention. |
| `weight`      | Entier | La probabilité relative de cette feature d'apparaître sur la carte.    |

#### Concepts de Features Reconnus
La liste des clés reconnues pour la `featurePalette` est la suivante :
- `door`
- `key_door`
- `switch_door`
- `secret_door`
- `secret_switch`
- `secret_exit`
- `crushing_ceiling`
- `elevator`
- `teleporter`

---

## 3. Fichier d'Exemple Canonique
Voici un exemple complet et valide qui illustre comment ces concepts s'assemblent.

```json
{
    "format": "D-Genesis",
    "version": "1.0",
    "mapInfo": {
        "game": "doom2",
        "mapLumpName": "MAP01",
        "name": "The Iron Furnace",
        "music": "D_SHAWN"
    },
    "generationParams": {
        "roomCount": 15,
        "avgConnectivity": 2.8,
        "avgFloorHeightDelta": 48,
        "avgHeadroom": 128,
        "totalVerticalSpan": 1024,
        "verticalTransitionProfile": [
            {
                "type": "level",
                "weight": 40
            },
            {
                "type": "step",
                "weight": 35
            },
            {
                "type": "overlook",
                "weight": 20
            },
            {
                "type": "lift",
                "weight": 5
            }
        ]
    },
    "sectorBehaviorPalette": {
        "normal": {
            "description": "Comportement par défaut, aucun effet.",
            "sectorSpecial": 0,
            "weight": 100
        },
        "flickering_light": {
            "description": "Lumière qui scintille de manière aléatoire.",
            "sectorSpecial": 17,
            "weight": 15
        },
        "pulsing_light_slow": {
            "description": "Lumière qui pulse lentement.",
            "sectorSpecial": 8,
            "weight": 10
        },
        "secret_area": {
            "description": "Marque le secteur comme un secret trouvé lors de l'entrée.",
            "sectorSpecial": 9,
            "weight": 5
        },
        "damage_floor_5_percent": {
            "description": "Inflige 5% de dégâts par seconde.",
            "sectorSpecial": 7,
            "weight": 10
        },
        "damage_floor_20_percent": {
            "description": "Inflige 20% de dégâts par seconde.",
            "sectorSpecial": 16,
            "weight": 5
        },
        "exit_trigger": {
            "description": "Met fin au niveau lorsque le joueur meurt dans ce secteur.",
            "sectorSpecial": 11,
            "weight": 1
        }
    },
    "featurePalette": {
        "door": {
            "description": "Une porte standard non verrouillée, actionnée par le joueur.",
            "weight": 100
        },
        "key_door": {
            "description": "Une progression basée sur une clé et une porte verrouillée.",
            "weight": 70
        },
        "switch_door": {
            "description": "Une porte qui s'ouvre en activant un interrupteur distant.",
            "weight": 40
        },
        "secret_door": {
            "description": "Un mur secret qui s'ouvre pour révéler une zone ou un objet.",
            "weight": 25
        },
        "secret_switch": {
            "description": "Un interrupteur caché qui active un secret (pont, porte, etc.).",
            "weight": 15
        },
        "secret_exit": {
            "description": "Une sortie de niveau cachée et optionnelle.",
            "weight": 5
        },
        "crushing_ceiling": {
            "description": "Un piège de plafond qui s'abaisse pour écraser.",
            "weight": 10
        },
        "elevator": {
            "description": "Un ascenseur pour la progression verticale.",
            "weight": 35
        },
        "teleporter": {
            "description": "Une paire de téléporteurs reliant deux points de la carte.",
            "weight": 20
        }
    },
    "themePalette": {
        "wall_primary": [
            {
                "name": "METAL1",
                "weight": 70
            },
            {
                "name": "STARG1",
                "weight": 30
            }
        ],
        "wall_accent": [
            {
                "name": "SKIN3",
                "weight": 100
            }
        ],
        "wall_support": [
            {
                "name": "SUPPORT3",
                "weight": 100
            }
        ],
        "wall_secret_indicator": [
            {
                "name": "BRICK11",
                "weight": 100
            }
        ],
        "wall_panel": [
            {
                "name": "COMPTALL",
                "weight": 100
            }
        ],
        "floor_primary": [
            {
                "name": "TECH01",
                "weight": 80
            },
            {
                "name": "FLAT5_4",
                "weight": 20
            }
        ],
        "floor_accent": [
            {
                "name": "FLAT23",
                "weight": 100
            }
        ],
        "floor_damage_low": [
            {
                "name": "NUKAGE1",
                "weight": 100
            }
        ],
        "floor_damage_high": [
            {
                "name": "LAVA1",
                "weight": 100
            }
        ],
        "ceiling_primary": [
            {
                "name": "CEIL1_1",
                "weight": 100
            }
        ],
        "ceiling_light_source": [
            {
                "name": "TLITE6_1",
                "weight": 100
            }
        ],
        "platform_surface": [
            {
                "name": "PLAT1",
                "weight": 100
            }
        ],
        "door_regular": [
            {
                "name": "BIGDOOR2",
                "weight": 100
            }
        ],
        "door_locked": [
            {
                "name": "BIGDOOR2",
                "weight": 100
            }
        ],
        "door_exit": [
            {
                "name": "EXITDOOR",
                "weight": 100
            }
        ],
        "door_frame": [
            {
                "name": "DOORTRAK",
                "weight": 100
            }
        ],
        "switch_utility": [
            {
                "name": "SW1STAR",
                "weight": 100
            }
        ],
        "switch_exit": [
            {
                "name": "SW1EXIT",
                "weight": 100
            }
        ],
        "door_indicator_blue": [
            {
                "name": "LITEBLU",
                "weight": 100
            }
        ],
        "door_indicator_red": [
            {
                "name": "LITERED",
                "weight": 100
            }
        ],
        "door_indicator_yellow": [
            {
                "name": "LITE5",
                "weight": 100
            }
        ]
    },
    "thematicTokens": [
        {
            "name": "METAL1",
            "type": "wall",
            "adjacencyRules": []
        },
        {
            "name": "SUPPORT3",
            "type": "wall",
            "adjacencyRules": [
                {
                    "adjacentTo": "METAL1",
                    "modifier": 3.0
                }
            ]
        },
        {
            "name": "SKIN3",
            "type": "wall",
            "adjacencyRules": [
                {
                    "adjacentTo": "LAVA1",
                    "modifier": 5.0
                },
                {
                    "adjacentTo": "METAL1",
                    "modifier": 0.2
                }
            ]
        },
        {
            "name": "TECH01",
            "type": "flat",
            "adjacencyRules": []
        },
        {
            "name": "LAVA1",
            "type": "flat",
            "adjacencyRules": []
        },
        {
            "name": "Imp",
            "type": "object",
            "typeId": 3001,
            "adjacencyRules": []
        },
        {
            "name": "Demon",
            "type": "object",
            "typeId": 3002,
            "adjacencyRules": [
                {
                    "adjacentToTypeId": 3001,
                    "modifier": 2.0
                }
            ]
        },
        {
            "name": "Exploding Barrel",
            "type": "object",
            "typeId": 2035,
            "adjacencyRules": [
                {
                    "adjacentToTypeId": 9,
                    "modifier": 2.0
                }
            ]
        }
    ]
}
