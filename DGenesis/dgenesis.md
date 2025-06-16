# Spécification du Format D-Genesis

**Version : 1.0**

## 1. Introduction

**D-Genesis** est un format JSON de haut niveau servant de directive de conception pour la génération procédurale de cartes de type Doom. Plutôt que de décrire la géométrie de la carte, il spécifie les **contraintes créatives et thématiques** sous-jacentes : le style visuel, l'ambiance, la population, la taille et la complexité.

L'objectif de ce format est de fournir à un système d'IA ou à un générateur procédural un "dossier de conception" (`design brief`) structuré, clair et sans ambiguïté.

Le concept central est celui des **"Tokens Thématiques"** et de leurs **"Règles d'Adjacence"**, qui fonctionnent de manière similaire à une chaîne de Markov pour définir des probabilités contextuelles d'apparition des textures, des sols et des objets.

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