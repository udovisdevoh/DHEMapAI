# Format DShape

Le format `DShape` est un format JSON simple conçu pour décrire la géométrie 2D d'un polygone. Il est principalement utilisé pour définir la forme de base d'un secteur dans un contexte de génération de cartes pour des jeux de type Doom.

## Structure Générale

Voici un exemple de la structure d'un fichier `DShape` :

```json
{
  "format": "DShape",
  "version": "1.0",
  "name": "canonical_example_room",
  "description": "Un polygone simple...",
  "vertices": [
    { "x": 0, "y": 0 },
    { "x": 64, "y": -10 },
    ...
  ]
}
```

## Description des Champs

- `format` **[Chaîne de caractères]**: Une chaîne fixe qui identifie le format. Doit être `"DShape"`.

- `version` **[Chaîne de caractères]**: La version du format `DShape` utilisée (par exemple, `"1.0"`).

- `name` **[Chaîne de caractères]**: Un nom ou identifiant unique et lisible pour le polygone.

- `description` **[Chaîne de caractères]**: Un texte libre décrivant l'intention ou l'apparence de la forme.

- `vertices` **[Tableau d'Objets]**: La liste ordonnée des sommets qui définissent le périmètre du polygone.
    - Chaque objet dans le tableau représente un sommet et doit contenir deux clés : `x` et `y`.
    - Les valeurs de `x` et `y` sont des nombres (entiers ou décimaux) représentant les coordonnées du point dans un plan 2D.
    - **IMPORTANT**: L'ordre des sommets est crucial. Ils doivent être listés de manière séquentielle (horaire ou anti-horaire) pour tracer le contour du polygone. Une ligne implicite relie le dernier sommet au premier pour fermer la forme.

