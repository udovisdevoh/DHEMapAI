# Documentation du Format DPolyGraph

Le format **DPolyGraph** est une spécification JSON conçue pour décrire la structure géométrique et logique d'un niveau de jeu. Il fait le pont entre un graphe de progression de haut niveau (comme le format `DGraph`) et une carte de jeu pleinement détaillée.

Son objectif principal est de définir la forme polygonale de chaque secteur (pièce) et les relations de progression entre eux (départ, sortie, serrures), sans se préoccuper des détails visuels ou physiques comme les textures, les hauteurs ou le placement des objets.

## Structure Globale

Un fichier `.dpolygraph.json` est un objet JSON contenant les propriétés de premier niveau suivantes :

| Propriété | Type | Description |
| :--- | :--- | :--- |
| `"format"` | `String` | Le nom du format, doit être `"DPolyGraph"`. |
| `"version"` | `String` | La version de la spécification, par exemple `"1.2"`. |
| `"sectors"` | `Array` d'Objets | La liste de tous les secteurs qui composent le niveau. |

## L'Objet `sector`

Chaque élément dans le tableau `sectors` est un objet qui représente une zone polygonale du niveau. Il est défini par les propriétés suivantes :

| Propriété | Type | Description |
| :--- | :--- | :--- |
| `"id"` | `Number` | Un identifiant entier unique pour le secteur. Il correspond généralement à l'ID du nœud dans le `DGraph` d'origine. |
| `"type"` | `String` | Le rôle logique du secteur. Valeurs possibles : `"start"`, `"exit"`, `"locked"`, `"standard"`. |
| `"polygon"` | `Array` d'Objets | Une liste ordonnée de sommets qui définissent le contour du secteur. Chaque sommet est un objet avec des propriétés `x` et `y`. Les sommets doivent être listés dans un ordre séquentiel (horaire ou antihoraire) pour former un polygone simple et fermé. |
| `"unlocksSector"`| `Number` (Optionnel) | Présent uniquement si ce secteur agit comme une "clé" ou un interrupteur. La valeur est l' `id` de l'unique secteur de type `"locked"` qu'il déverrouille. |
| `"unlockedBySector"`| `Number` (Optionnel) | Présent uniquement si le `type` du secteur est `"locked"`. La valeur est l' `id` de l'unique secteur qui déverrouille celui-ci. |

> **Note sur les relations de déverrouillage :** La relation entre un secteur "clé" et une "serrure" est toujours une paire un-pour-un, explicitement définie dans les deux sens via `unlocksSector` et `unlockedBySector`.

## Exemple Canonique

Voici l'exemple d'un fichier `exemple.dpolygraph.json` complet, représentant un niveau simple avec cinq secteurs, dont une paire clé/serrure.

```json
{
  "format": "DPolyGraph",
  "version": "1.2",

  "sectors": [
    {
      "id": 0,
      "type": "start",
      "polygon": [
        { "x": 0, "y": 100 },
        { "x": 100, "y": 100 },
        { "x": 100, "y": 0 },
        { "x": 0, "y": 0 }
      ]
    },
    {
      "id": 1,
      "type": "standard",
      "polygon": [
        { "x": 100, "y": 100 },
        { "x": 200, "y": 100 },
        { "x": 200, "y": 0 },
        { "x": 100, "y": 0 }
      ]
    },
    {
      "id": 2,
      "type": "standard",
      "unlocksSector": 3,
      "polygon": [
        { "x": 200, "y": 100 },
        { "x": 300, "y": 100 },
        { "x": 300, "y": 0 },
        { "x": 200, "y": 0 }
      ]
    },
    {
      "id": 3,
      "type": "locked",
      "unlockedBySector": 2,
      "polygon": [
        { "x": 300, "y": 100 },
        { "x": 400, "y": 100 },
        { "x": 400, "y": 0 },
        { "x": 300, "y": 0 }
      ]
    },
    {
      "id": 4,
      "type": "exit",
      "polygon": [
        { "x": 400, "y": 100 },
        { "x": 500, "y": 100 },
        { "x": 500, "y": 0 },
        { "x": 400, "y": 0 }
      ]
    }
  ]
}
