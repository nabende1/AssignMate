$path = 'Data/Migrations/20260811093653_InitialCreate.cs'
$text = Get-Content -Raw -Path $path
$text = [regex]::Replace($text, 'table\.Column<([^>]+)>\(\s*,\s*', 'table.Column<$1>(')
$text = [regex]::Replace($text, '\(\s*,\s*maxLength:', '(maxLength:')
$text = [regex]::Replace($text, '\(\s*,\s*nullable:', '(nullable:')
$text = [regex]::Replace($text, '\(\s*,\s*type:', '(type:')
$text = [regex]::Replace($text, '\(\s*,\s*nullable:\s*false\)\s*,\s*', '(nullable: false), ')
Set-Content -Path $path -Value $text
Write-Host "Repaired $path"