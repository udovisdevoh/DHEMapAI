# Documentation du Format `.dgraph.json`

*Ce document spécifie la structure d'un fichier `.dgraph.json`, utilisé pour représenter un graphe de niveau planaire. Ce format est conçu pour être simple, lisible et facilement parsable.*

---

## Structure Racine

L'objet racine du fichier JSON contient deux propriétés principales : `nodes` et `edges`.

| Clé | Type | Description |
| :--- | :--- | :--- |
| `nodes` | `Array<Node>` | Une liste de tous les objets Nœud qui composent le graphe. |
| `edges` | `Array<Edge>` | Une liste de toutes les arêtes (connexions) entre les nœuds. |

---

## L'Objet `Node`

Chaque objet `Node` représente un lieu ou une "salle" dans le graphe du niveau.

| Propriété | Type | Statut | Description |
| :--- | :--- | :--- | :--- |
| `id` | `Integer` | **Requis** | Un identifiant numérique unique pour le nœud. |
| `type` | `String` | **Requis** | Le rôle fonctionnel du nœud. Voir la table des types de nœuds ci-dessous. |
| `position`| `Object` | **Requis** | Un objet contenant les coordonnées `x` et `y` pour la représentation visuelle planaire du graphe. |
| `unlocks` | `Array<Integer>` | *Optionnel* | Une liste des `id` de nœuds de type `locked` que ce nœud déverrouille. Présent uniquement sur les nœuds qui agissent comme un déclencheur (clé, interrupteur). |

### Types de Nœuds (`type`)

La propriété `type` d'un nœud définit son comportement dans la logique du jeu.

| Valeur | Description |
| :--- | :--- |
| `start` | Le point d'entrée unique du graphe. Il doit y en avoir exactement un par graphe. |
| `exit` | Un point de sortie du graphe. Il peut y en avoir un ou plusieurs. |
| `standard`| Un nœud de passage normal, sans fonction spéciale intrinsèque. Peut devenir un déclencheur en ayant une propriété `unlocks`. |
| `locked` | Un nœud inaccessible tant qu'il n'a pas été déverrouillé par un autre nœud. Ce nœud est la "porte" et non la "clé". |

---

## L'Objet `Edge`

Chaque objet `Edge` représente une connexion simple et bidirectionnelle entre deux nœuds.

| Propriété | Type | Statut | Description |
| :--- | :--- | :--- | :--- |
| `source` | `Integer` | **Requis** | L'`id` du premier nœud de la connexion. |
| `target` | `Integer` | **Requis** | L'`id` du second nœud de la connexion. |

---

## Exemple Canonique Complet

Voici un exemple complet d'un fichier `.dgraph.json` valide qui illustre les concepts décrits ci-dessus.

```json
{
  "nodes": [
    {
      "id": 0,
      "type": "start",
      "position": {
        "x": 50,
        "y": 200
      }
    },
    {
      "id": 1,
      "type": "standard",
      "position": {
        "x": 150,
        "y": 200
      }
    },
    {
      "id": 2,
      "type": "standard",
      "unlocks": [4],
      "position": {
        "x": 150,
        "y": 100
      }
    },
    {
      "id": 3,
      "type": "standard",
      "position": {
        "x": 250,
        "y": 200
      }
    },
    {
      "id": 4,
      "type": "locked",
      "position": {
        "x": 350,
        "y": 200
      }
    },
    {
      "id": 5,
      "type": "exit",
      "position": {
        "x": 450,
        "y": 200
      }
    },
    {
      "id": 6,
      "type": "standard",
      "position": {
        "x": 250,
        "y": 300
      }
    }
  ],
  "edges": [
    {
      "source": 0,
      "target": 1
    },
    {
      "source": 1,
      "target": 2
    },
    {
      "source": 1,
      "target": 3
    },
    {
      "source": 3,
      "target": 4
    },
    {
      "source": 4,
      "target": 5
    },
    {
      "source": 3,
      "target": 6
    }
  ]
}
