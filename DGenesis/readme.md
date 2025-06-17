# Spécification du Format D-Genesis
**Version : 1.0**

---

## 1. Introduction

**D-Genesis** est un format JSON de haut niveau servant de directive de conception pour la génération procédurale de cartes de type Doom. Plutôt que de décrire la géométrie de la carte, il spécifie les **contraintes créatives et thématiques** sous-jacentes : le style visuel, l'ambiance, la population, la taille et la complexité.

L'objectif de ce format est de fournir à un système d'IA ou à un générateur procédural un "dossier de conception" (`design brief`) structuré, clair et sans ambiguïté.

Le concept central est celui des **"Tokens Thématiques"** et de leurs **"Règles d'Adjacence"**, qui fonctionnent de manière similaire à une chaîne de Markov pour définir des probabilités contextuelles d'apparition des textures, des sols et des objets.

---

## 2. Structure du Fichier

Le fichier D-Genesis est un objet JSON unique.
```json
{
  "format": "D-Genesis",
  "version": "1.0",
  "mapInfo": { ... },
  "generationParams": { ... },
  "thematicTokens": [ ... ]
}
```

### 2.1. `format` et `version` (Requis)
- `format`: Chaîne. Doit être `"D-Genesis"`.
- `version`: Chaîne. Doit être `"1.0"`.

### 2.2. L'objet `mapInfo` (Requis)
Contient les métadonnées de base de la carte à générer.

| Clé           | Type   | Description                                           | Exemple              |
|---------------|--------|-------------------------------------------------------|----------------------|
| `game`        | Chaîne | Jeu cible. **Enum**: `"doom"`, `"doom2"`, `"heretic"`, `"hexen"`. | `"doom2"`            |
| `mapLumpName` | Chaîne | Nom du marqueur de la carte (ex: "E1M1", "MAP01").   | `"MAP01"`            |
| `name`        | Chaîne | Nom de la carte qui sera affiché en jeu.                | `"The Iron Furnace"` |
| `music`       | Chaîne | Nom du *lump* de musique (ex: `"D_RUNNIN"`).           | `"D_SHAWN"`          |

### 2.3. L'objet `generationParams` (Requis)
Définit les contraintes structurelles et de gameplay de haut niveau avec des valeurs objectives.

| Clé                         | Type             | Description                                                                                             | Exemple     |
|-----------------------------|------------------|---------------------------------------------------------------------------------------------------------|-------------|
| `roomCount`                 | Entier           | Nombre approximatif de pièces (salles) principales à générer.                                          | `15`        |
| `secretRoomPercentage`      | Nombre           | Probabilité (0.0 à 1.0) qu'une pièce soit un secret.                                                    | `0.15`      |
| `avgConnectivity`           | Nombre           | Nombre moyen de connexions par pièce. <2 est linéaire, >2.5 est très connecté.                           | `2.8`       |
| `avgFloorHeightDelta`       | Entier           | Différence de hauteur de sol moyenne (en unités Doom) entre deux pièces connectées.                     | `48`        |
| `avgHeadroom`               | Entier           | Épaisseur verticale moyenne d'un secteur (distance entre son sol et son plafond).                        | `128`       |
| `totalVerticalSpan`         | Entier           | Différence de hauteur maximale autorisée entre le plancher le plus bas et le plafond le plus haut.     | `1024`      |
| `verticalTransitionProfile` | Tableau d'objets | Définit les probabilités des types de connexions verticales. Voir ci-dessous.                            | `[...]`     |

#### L'objet `verticalTransitionProfile`
Chaque objet du tableau `verticalTransitionProfile` contient :
- `type` (Chaîne): Le type de transition. **Enum**: `"level"` (plain-pied), `"step"` (escaliers), `"overlook"` (fenêtre/balcon), `"lift"` (ascenseur).
- `weight` (Entier): La probabilité relative de ce type de transition.

### 2.4. Le tableau `thematicTokens` (Requis)
C'est le cœur du format. Il définit l'ensemble des "pinceaux" que le générateur peut utiliser, ainsi que leurs interactions. Chaque objet du tableau est un **Token**.

#### L'objet Token

| Clé                | Type                 | Description                                                                                                                                                    |
|--------------------|----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `name`             | Chaîne               | Le nom descriptif et lisible de l'asset. Pour les 'things', ce nom doit correspondre à celui associé au `typeId` dans les listes de référence du jeu (ex: 'Shotgun Guy'). |
| `type`             | Chaîne               | Le type de token. **Enum**: `"wall"`, `"flat"`, `"object"`, `"connection_action"`.                                                                            |
| `typeId`           | Entier \| `null`     | Si le `type` est `"object"`, ceci est l'identifiant numérique canonique du 'thing'. C'est cette valeur qui doit être utilisée par le générateur. Sinon, `null`.       |
| `actionInfo`       | Objet \| `null`      | Si le `type` est `"connection_action"`, cet objet contient les détails de l'action : `special` (Entier) et `properties` (Objet). Sinon, `null`.                   |
| `baseWeight`       | Entier               | La probabilité de base (non-contextuelle) de ce token d'être choisi parmi tous les tokens du même `type`. Des valeurs plus élevées sont plus probables.        |
| `adjacencyRules`   | Tableau d'objets     | Un tableau de règles qui modifient le poids du token en fonction de ses voisins. Voir la section 2.4.1.                                                          |

#### 2.4.1. L'objet AdjacencyRule
Une règle d'adjacence définit comment la probabilité d'un token est affectée par la présence d'un autre token à proximité. Une règle doit contenir `modifier` et une seule des deux propriétés de ciblage : `adjacentTo` ou `adjacentToTypeId`.

| Clé                | Type   | Description                                                                                                                                      |
|--------------------|--------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| `modifier`         | Nombre | Le multiplicateur à appliquer au `baseWeight` du token courant si la condition est remplie. > 1 augmente la probabilité, < 1 la diminue.       |
| `adjacentTo`       | Chaîne | Cible un autre token par son `name`. Utile pour les éléments architecturaux (murs, sols).                                                        |
| `adjacentToTypeId` | Entier | Cible un token de type `object` par son `typeId`. Méthode robuste pour cibler les 'things'.                                                      |

---

## 3. Fichier d'Exemple Canonique

Voici un exemple complet et valide qui illustre comment ces concepts s'assemblent pour définir un thème "Forge Industrielle Infernale".

```json
{
    "format": "D-Genesis",
    "version": "1.0",
    "mapInfo": {
        "game": "doom2",
        "mapLumpName": "MAP01",
        "name": "The Abstract Forge",
        "music": "D_SHAWN"
    },
    "generationParams": {
        "roomCount": 15,
        "secretRoomPercentage": 0.20,
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
                "name": "DOORBLU",
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
        ]
    },
    "thematicTokens": [
        {
            "name": "Main Wall",
            "type": "wall",
            "paletteConcept": "wall_primary",
            "baseWeight": 100,
            "adjacencyRules": []
        },
        {
            "name": "Accent Wall",
            "type": "wall",
            "paletteConcept": "wall_accent",
            "baseWeight": 15,
            "adjacencyRules": []
        },
        {
            "name": "Support Wall",
            "type": "wall",
            "paletteConcept": "wall_support",
            "baseWeight": 25,
            "adjacencyRules": [
                {
                    "adjacentTo": "Main Wall",
                    "modifier": 2.0
                }
            ]
        },
        {
            "name": "Secret Wall",
            "type": "wall",
            "paletteConcept": "wall_secret_indicator",
            "baseWeight": 5,
            "adjacencyRules": []
        },
        {
            "name": "Panel Wall",
            "type": "wall",
            "paletteConcept": "wall_panel",
            "baseWeight": 10,
            "adjacencyRules": []
        },
        {
            "name": "Door Frame",
            "type": "wall",
            "paletteConcept": "door_frame",
            "baseWeight": 20,
            "adjacencyRules": []
        },
        {
            "name": "Utility Switch Wall",
            "type": "wall",
            "paletteConcept": "switch_utility",
            "baseWeight": 5,
            "adjacencyRules": [
                {
                    "adjacentTo": "Panel Wall",
                    "modifier": 4.0
                }
            ]
        },
        {
            "name": "Exit Switch Wall",
            "type": "wall",
            "paletteConcept": "switch_exit",
            "baseWeight": 1,
            "adjacencyRules": []
        },
        {
            "name": "Main Floor",
            "type": "flat",
            "paletteConcept": "floor_primary",
            "baseWeight": 100,
            "adjacencyRules": []
        },
        {
            "name": "Accent Floor",
            "type": "flat",
            "paletteConcept": "floor_accent",
            "baseWeight": 20,
            "adjacencyRules": []
        },
        {
            "name": "Low Damage Floor",
            "type": "flat",
            "paletteConcept": "floor_damage_low",
            "baseWeight": 15,
            "adjacencyRules": []
        },
        {
            "name": "High Damage Floor",
            "type": "flat",
            "paletteConcept": "floor_damage_high",
            "baseWeight": 5,
            "adjacencyRules": []
        },
        {
            "name": "Main Ceiling",
            "type": "flat",
            "paletteConcept": "ceiling_primary",
            "baseWeight": 100,
            "adjacencyRules": []
        },
        {
            "name": "Light Fixture Ceiling",
            "type": "flat",
            "paletteConcept": "ceiling_light_source",
            "baseWeight": 25,
            "adjacencyRules": []
        },
        {
            "name": "Platform Top",
            "type": "flat",
            "paletteConcept": "platform_surface",
            "baseWeight": 10,
            "adjacencyRules": []
        },
        {
            "name": "Standard Door",
            "type": "connection_action",
            "paletteConcept": "door_regular",
            "actionInfo": {
                "special": 1,
                "properties": null
            },
            "baseWeight": 100,
            "adjacencyRules": []
        },
        {
            "name": "Locked Door",
            "type": "connection_action",
            "paletteConcept": "door_locked",
            "actionInfo": {
                "special": 26,
                "properties": { "keyItemName": "Blue Keycard" }
            },
            "baseWeight": 10,
            "adjacencyRules": []
        },
        {
            "name": "Exit Door",
            "type": "connection_action",
            "paletteConcept": "door_exit",
            "actionInfo": {
                "special": 11,
                "properties": null
            },
            "baseWeight": 1,
            "adjacencyRules": []
        },
        {
            "name": "Imp",
            "type": "object",
            "typeId": 3001,
            "baseWeight": 100,
            "adjacencyRules": []
        },
        {
            "name": "Demon",
            "type": "object",
            "typeId": 3002,
            "baseWeight": 50,
            "adjacencyRules": [
                {
                    "adjacentToTypeId": 3001,
                    "modifier": 2.0
                }
            ]
        }
    ]
}