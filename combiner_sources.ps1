# Déclare la prise en charge des paramètres avancés.
[CmdletBinding()]
param (
    # Définit le paramètre -Path. Il n'est pas obligatoire.
    # S'il est absent, sa valeur par défaut sera le répertoire courant.
    [Parameter(Mandatory=$false, ValueFromPipeline=$true)]
    [string]$Path = (Get-Location).Path
)

# Construit le chemin complet pour le fichier de sortie.
$outputFile = Join-Path -Path $Path -ChildPath "combined_source_code.cs"

# Affiche le répertoire qui sera analysé.
Write-Host "Analyse du répertoire : $Path"

# Supprime le fichier de sortie s'il existe déjà pour garantir un résultat propre.
if (Test-Path $outputFile) {
    Remove-Item $outputFile
}

# Récupère tous les fichiers .cs de manière récursive dans le chemin spécifié.
# Le bloc try/catch gère les erreurs si le chemin n'est pas valide.
try {
    $csFiles = Get-ChildItem -Path $Path -Filter *.cs -Recurse -ErrorAction Stop

    # Boucle sur chaque fichier .cs trouvé.
    foreach ($file in $csFiles) {
        # Affiche le fichier en cours de traitement.
        Write-Host "Ajout de : $($file.FullName)"

        # Crée la ligne de commentaire avec le chemin complet du fichier.
        $header = "// $($file.FullName)"

        # Ajoute d'abord la ligne de commentaire, puis le contenu complet du fichier
        # au fichier de sortie. -Raw lit le fichier en une seule chaîne, ce qui est plus efficace.
        Add-Content -Path $outputFile -Value $header
        Add-Content -Path $outputFile -Value (Get-Content -Path $file.FullName -Raw)
    }

    Write-Host "`n✅ Terminé ! Le fichier combiné est ici : $outputFile"
}
catch {
    # Affiche une erreur si le chemin n'existe pas ou n'est pas accessible.
    Write-Error "❌ Une erreur est survenue. Vérifiez que le chemin '$Path' est valide."
}