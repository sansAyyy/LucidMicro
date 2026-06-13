param(
    [Parameter(Mandatory = $true)]
    [string]$ServiceName,

    [Parameter(Mandatory = $true)]
    [string]$FeatureName,

    [Parameter(Mandatory = $true)]
    [string]$EntityName,

    [string]$Route,

    [string]$TableName,

    [int]$NameMaxLength = 128,

    [switch]$DryRun,

    [switch]$Force
)

$ErrorActionPreference = 'Stop'

function Convert-ToCamelCase {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value.Length -eq 1) {
        return $Value.ToLowerInvariant()
    }

    return $Value.Substring(0, 1).ToLowerInvariant() + $Value.Substring(1)
}

function Convert-ToKebabCase {
    param([Parameter(Mandatory = $true)][string]$Value)

    return [regex]::Replace($Value, '([a-z0-9])([A-Z])', '$1-$2').ToLowerInvariant()
}

function Convert-ToSnakeCase {
    param([Parameter(Mandatory = $true)][string]$Value)

    return [regex]::Replace($Value, '([a-z0-9])([A-Z])', '$1_$2').ToLowerInvariant()
}

function Add-UniqueLineBefore {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content,

        [Parameter(Mandatory = $true)]
        [string]$Line,

        [Parameter(Mandatory = $true)]
        [string]$BeforePattern
    )

    if ($Content.Contains($Line)) {
        return $Content
    }

    $match = [regex]::Match($Content, $BeforePattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    if (-not $match.Success) {
        throw "Could not find insertion point matching '$BeforePattern'."
    }

    return $Content.Insert($match.Index, $Line + [Environment]::NewLine)
}

function Add-UniqueScopedRegistration {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content,

        [Parameter(Mandatory = $true)]
        [string]$RegistrationLine
    )

    if ($Content.Contains($RegistrationLine)) {
        return $Content
    }

    $anchorPattern = '^\s*services\.AddValidatorsFromAssemblyContaining<.*;\s*$'
    $anchor = [regex]::Match($Content, $anchorPattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    if ($anchor.Success) {
        return $Content.Insert($anchor.Index, '        ' + $RegistrationLine + [Environment]::NewLine)
    }

    $returnPattern = '^\s*return services;\s*$'
    $returnStatement = [regex]::Match($Content, $returnPattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    if (-not $returnStatement.Success) {
        throw "Could not find 'return services;' in dependency injection file."
    }

    return $Content.Insert($returnStatement.Index, '        ' + $RegistrationLine + [Environment]::NewLine + [Environment]::NewLine)
}

function Register-ApplicationService {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ApplicationDependencyInjectionFile,

        [Parameter(Mandatory = $true)]
        [string]$ServiceName,

        [Parameter(Mandatory = $true)]
        [string]$FeatureName
    )

    if (-not (Test-Path $ApplicationDependencyInjectionFile)) {
        throw "Dependency injection file '$ApplicationDependencyInjectionFile' was not found."
    }

    $abstractionsUsing = "using LucidMicro.Services.$ServiceName.Application.Features.$FeatureName.Abstractions;"
    $servicesUsing = "using LucidMicro.Services.$ServiceName.Application.Features.$FeatureName.Services;"
    $registrationLine = "services.AddScoped<I$($FeatureName)ApplicationService, $($FeatureName)ApplicationService>();"

    $content = Get-Content -LiteralPath $ApplicationDependencyInjectionFile -Raw
    $content = Add-UniqueLineBefore -Content $content -Line $abstractionsUsing -BeforePattern '^using '
    $content = Add-UniqueLineBefore -Content $content -Line $servicesUsing -BeforePattern '^using '
    $content = Add-UniqueScopedRegistration -Content $content -RegistrationLine $registrationLine

    Set-Content -LiteralPath $ApplicationDependencyInjectionFile -Value $content -NoNewline
    Write-Output "Registered I$($FeatureName)ApplicationService in $ApplicationDependencyInjectionFile"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$templateRoot = Join-Path $repoRoot 'templates\crud-module-template\backend'
$serviceRoot = Join-Path $repoRoot "backend\src\Services\$ServiceName"

if (-not (Test-Path $templateRoot)) {
    throw "Template directory '$templateRoot' was not found."
}

if (-not (Test-Path $serviceRoot)) {
    throw "Service directory '$serviceRoot' was not found."
}

if ([string]::IsNullOrWhiteSpace($Route)) {
    $Route = 'api/' + (Convert-ToKebabCase $FeatureName)
}

if ([string]::IsNullOrWhiteSpace($TableName)) {
    $TableName = Convert-ToSnakeCase $FeatureName
}

$featureNameCamel = Convert-ToCamelCase $FeatureName
$entityNameCamel = Convert-ToCamelCase $EntityName
$entityNamePluralCamel = Convert-ToCamelCase $FeatureName

$replacements = [ordered]@{
    '__ServiceName__' = $ServiceName
    '__FeatureName__' = $FeatureName
    '__featureNameCamel__' = $featureNameCamel
    '__EntityName__' = $EntityName
    '__entityNameCamel__' = $entityNameCamel
    '__entityNamePluralCamel__' = $entityNamePluralCamel
    '__Route__' = $Route
    '__TableName__' = $TableName
    '__NameMaxLength__' = [string]$NameMaxLength
}

$layerProjects = @{
    'Api' = "LucidMicro.Services.$ServiceName.Api"
    'Application' = "LucidMicro.Services.$ServiceName.Application"
    'Domain' = "LucidMicro.Services.$ServiceName.Domain"
    'Infrastructure' = "LucidMicro.Services.$ServiceName.Infrastructure"
}

$generationPlan = @()

Get-ChildItem -Path $templateRoot -Recurse -File -Filter '*.tpl' | ForEach-Object {
    $relativePath = [System.IO.Path]::GetRelativePath($templateRoot, $_.FullName)
    $segments = $relativePath -split '[\\/]'
    $layer = $segments[0]

    if (-not $layerProjects.ContainsKey($layer)) {
        throw "Unknown template layer '$layer' in '$relativePath'."
    }

    $projectName = $layerProjects[$layer]
    $targetRelativePath = ($segments | Select-Object -Skip 1) -join [System.IO.Path]::DirectorySeparatorChar
    $targetRelativePath = $targetRelativePath -replace '\.tpl$', ''

    foreach ($key in $replacements.Keys) {
        $targetRelativePath = $targetRelativePath.Replace($key, $replacements[$key])
    }

    $targetPath = Join-Path (Join-Path $serviceRoot $projectName) $targetRelativePath

    $generationPlan += [pscustomobject]@{
        TemplatePath = $_.FullName
        TargetPath = $targetPath
        Exists = Test-Path $targetPath
    }
}

$applicationDependencyInjectionFile = Join-Path $serviceRoot "LucidMicro.Services.$ServiceName.Application\DependencyInjection\ServiceCollectionExtensions.cs"
$applicationServiceAlreadyRegistered = $false
if (Test-Path $applicationDependencyInjectionFile) {
    $applicationServiceAlreadyRegistered = (Get-Content -LiteralPath $applicationDependencyInjectionFile -Raw).Contains("I$($FeatureName)ApplicationService")
}

if ($DryRun) {
    Write-Output "Dry run for CRUD module '$FeatureName' in service '$ServiceName'."
    foreach ($item in $generationPlan) {
        $action = if ($item.Exists -and $Force) { 'overwrite' } elseif ($item.Exists) { 'exists' } else { 'create' }
        Write-Output "[$action] $($item.TargetPath)"
    }

    $registrationAction = if ($applicationServiceAlreadyRegistered) { 'exists' } else { 'update' }
    Write-Output "[$registrationAction] $applicationDependencyInjectionFile"
    return
}

foreach ($item in $generationPlan) {
    if ($item.Exists -and -not $Force) {
        throw "Target file '$($item.TargetPath)' already exists. Use -Force to overwrite."
    }

    $content = Get-Content -LiteralPath $item.TemplatePath -Raw
    foreach ($key in $replacements.Keys) {
        $content = $content.Replace($key, $replacements[$key])
    }

    $targetDirectory = Split-Path -Parent $item.TargetPath
    New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    Set-Content -LiteralPath $item.TargetPath -Value $content -NoNewline
    Write-Output "Generated $($item.TargetPath)"
}

Register-ApplicationService `
    -ApplicationDependencyInjectionFile $applicationDependencyInjectionFile `
    -ServiceName $ServiceName `
    -FeatureName $FeatureName

Write-Output "CRUD module '$FeatureName' generated for service '$ServiceName'."
