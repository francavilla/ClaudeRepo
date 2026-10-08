<#
.SYNOPSIS
    Crea un nuovo progetto a partire da DesktopAppTemplate.

.DESCRIPTION
    Copia il template in una nuova cartella (senza bin/obj), sostituisce il nome "DesktopAppTemplate"
    in nomi di file, cartelle e contenuti (namespace, riferimenti, XAML, .sln...) e assegna nuovi GUID ai progetti.
    Il template originale non viene modificato.

.PARAMETER NewName
    Nome del nuovo progetto, es. "GestioneOrdini" oppure "Acme.GestioneOrdini" (diventa namespace e nome degli assembly).

.PARAMETER Destination
    Cartella di destinazione. Predefinita: una cartella con il nuovo nome accanto al template.

.EXAMPLE
    .\New-Project.ps1 -NewName GestioneOrdini
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$NewName,

    [string]$Destination
)

$ErrorActionPreference = 'Stop'
$oldName = 'DesktopAppTemplate'
$source = $PSScriptRoot

if ($NewName -notmatch '^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$') {
    throw "Nome non valido: '$NewName'. Usare lettere, cifre e '_' (con '.' come separatore), senza iniziare con una cifra."
}
if (-not $Destination) { $Destination = Join-Path (Split-Path $source -Parent) $NewName }
if (Test-Path $Destination) { throw "La cartella '$Destination' esiste già." }

# 1. Copia dei file (esclusi bin, obj, .vs e questo script)
$excluded = @('bin', 'obj', '.vs')
New-Item -ItemType Directory -Path $Destination | Out-Null
$Destination = (Resolve-Path $Destination).Path

Get-ChildItem -Path $source -Recurse -File -Force | ForEach-Object {
    $relative = $_.FullName.Substring($source.Length).TrimStart('\', '/')
    $parts = $relative -split '[\\/]'
    if (($parts | Where-Object { $excluded -contains $_ }) -or $relative -eq 'New-Project.ps1') { return }

    $target = Join-Path $Destination $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
    Copy-Item -LiteralPath $_.FullName -Destination $target
}

# 2. Contenuti: sostituzione del nome e nuovi GUID dei progetti nella solution
$textExtensions = '.cs', '.csproj', '.props', '.sln', '.xaml', '.config', '.manifest', '.md', '.json', '.yml', '.ps1'
$utf8Bom = New-Object System.Text.UTF8Encoding($true)

Get-ChildItem -Path $Destination -Recurse -File -Force |
    Where-Object { $textExtensions -contains $_.Extension.ToLowerInvariant() } |
    ForEach-Object {
        $text = [System.IO.File]::ReadAllText($_.FullName)
        $updated = $text.Replace($oldName, $NewName)

        if ($_.Extension -eq '.sln') {
            # Solo i GUID dei progetti (terzo GUID di ogni riga "Project"): i GUID di tipo restano invariati.
            $projectGuids = [regex]::Matches($updated, 'Project\("\{[^}]+\}"\) = "[^"]+", "[^"]+", "\{([0-9A-Fa-f-]+)\}"') |
                ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
            foreach ($guid in $projectGuids) {
                $updated = [regex]::Replace($updated, [regex]::Escape($guid), [guid]::NewGuid().ToString().ToUpperInvariant(), 'IgnoreCase')
            }
        }

        if ($updated -ne $text) { [System.IO.File]::WriteAllText($_.FullName, $updated, $utf8Bom) }
    }

# 3. Nomi di file e cartelle (dai più profondi ai meno profondi)
Get-ChildItem -Path $Destination -Recurse -Force |
    Sort-Object { $_.FullName.Length } -Descending |
    Where-Object { $_.Name.Contains($oldName) } |
    ForEach-Object { Rename-Item -LiteralPath $_.FullName -NewName $_.Name.Replace($oldName, $NewName) }

Write-Host "Progetto '$NewName' creato in: $Destination"
Write-Host "Prossimi passi: aprire $NewName.sln, poi seguire la sezione 'Adattarlo a un nuovo progetto' del README."
