$PSScriptRoot = split-path -parent $MyInvocation.MyCommand.Definition

function ChangeExt($path, $ext) {
    Join-Path "$($([System.IO.Path]::GetDirectoryName($path)))" "$([System.IO.Path]::GetFileNameWithoutExtension($path)).$ext"
}

$LEX_FILENAME = "wordcount.lex"

# The tool
$TOOLS_DIR = Join-Path $PSScriptRoot ".." | Join-Path -ChildPath "tools"
$CSLEX_DIR = Join-Path $TOOLS_DIR "JohnGough"
$GPLEX = Join-Path $CSLEX_DIR "gplex.exe"

# The tool outputs this file
$LEX_OUTFILE = Join-Path $PSScriptRoot "$([System.IO.Path]::GetFileNameWithoutExtension($LEX_FILENAME)).cs"

$OUT_DIR = Join-Path $PSScriptRoot "Lexers"
$INPUT_FILE = Join-Path $PSScriptRoot "lexfiles" | Join-Path -ChildPath $LEX_FILENAME


& $GPLEX /summary $INPUT_FILE

# The Brad Merril version is hardcoded to produce the C# Lexer file in the working directory (pwd)
# with a filename where only the extension of the lexer specification file (*.lex)  is changed to '*.cs'
New-Item -ItemType Directory $OUT_DIR -ErrorAction SilentlyContinue | out-null
Copy-Item $LEX_OUTFILE $OUT_DIR
Remove-Item $LEX_OUTFILE
