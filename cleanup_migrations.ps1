$paths = @(
    'Data/Migrations/20260811093653_InitialCreate.cs',
    'Data/Migrations/ApplicationDbContextModelSnapshot.cs',
    'Data/Migrations/20260811093653_InitialCreate.Designer.cs'
)
$patterns = @(
    [regex] 'type:\s*"TEXT"',
    [regex] 'type:\s*"INTEGER"',
    [regex] '\.Annotation\("Sqlite:Autoincrement", true\)',
    [regex] '\.HasColumnType\("TEXT"\)',
    [regex] '\.HasColumnType\("INTEGER"\)'
)
foreach ($path in $paths) {
    Write-Host "Processing $path"
    $text = Get-Content -Raw -Path $path
    foreach ($pattern in $patterns) {
        $text = $pattern.Replace($text, "")
    }
    Set-Content -Path $path -Value $text
}
