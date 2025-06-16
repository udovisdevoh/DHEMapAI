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
    "name": "The Iron Furnace",
    "music": "D_RUNNIN"
  },
  "generationParams": {
    "roomCount": 12,
    "secretRoomPercentage": 0.15,
    "avgConnectivity": 2.8,
    "avgFloorHeightDelta": 48,
    "avgHeadroom": 128,
    "totalVerticalSpan": 1024,
    "verticalTransitionProfile": [
      { "type": "level", "weight": 40 },
      { "type": "step", "weight": 35 },
      { "type": "overlook", "weight": 20 },
      { "type": "lift", "weight": 5 }
    ]
  },
  "thematicTokens": [
    {
      "name": "METAL5", "type": "flat", "typeId": null, "actionInfo": null,
      "baseWeight": 100, "adjacencyRules": []
    },
    {
      "name": "RROCK05", "type": "flat", "typeId": null, "actionInfo": null,
      "baseWeight": 80,
      "adjacencyRules": [ { "adjacentTo": "LAVA1", "modifier": 3 } ]
    },
    {
      "name": "LAVA1", "type": "flat", "typeId": null, "actionInfo": null,
      "baseWeight": 25,
      "adjacencyRules": [
        { "adjacentTo": "RROCK05", "modifier": 4 },
        { "adjacentTo": "METAL5", "modifier": 0.5 }
      ]
    },
    {
      "name": "METAL1", "type": "wall", "typeId": null, "actionInfo": null,
      "baseWeight": 200, "adjacencyRules": []
    },
    {
      "name": "SUPPORT3", "type": "wall", "typeId": null, "actionInfo": null,
      "baseWeight": 70,
      "adjacencyRules": [ { "adjacentTo": "METAL1", "modifier": 2 } ]
    },
    {
      "name": "Imp", "type": "object", "typeId": 3001, "actionInfo": null,
      "baseWeight": 100, "adjacencyRules": []
    },
    {
      "name": "Shotgun Guy", "type": "object", "typeId": 9, "actionInfo": null,
      "baseWeight": 70, "adjacencyRules": []
    },
    {
      "name": "Demon", "type": "object", "typeId": 3002, "actionInfo": null,
      "baseWeight": 50,
      "adjacencyRules": [ { "adjacentToTypeId": 3001, "modifier": 2 } ]
    },
    {
      "name": "Exploding Barrel", "type": "object", "typeId": 2035, "actionInfo": null,
      "baseWeight": 30,
      "adjacencyRules": [
        { "adjacentToTypeId": 3001, "modifier": 1.5 },
        { "adjacentToTypeId": 9, "modifier": 2 }
      ]
    },
    {
      "name": "Standard Door", "type": "connection_action", "typeId": null,
      "actionInfo": { "special": 1, "properties": null },
      "baseWeight": 100, "adjacencyRules": []
    }
  ]
}
