$path = 'Data/Migrations/20260811093653_InitialCreate.cs'
$text = Get-Content -Raw -Path $path
$text = [regex]::Replace($text, 'table\.Column<([^>]+)>\(\s*,\s*nullable:', 'table.Column<$1>(nullable:')
$text = [regex]::Replace($text, 'table\.Column<([^>]+)>\(\s*,\s*nullable:\s*false\)\s*,\s*', 'table.Column<$1>(nullable: false)')
Set-Content -Path $path -Value $text
Write-Host "Repaired $path"
