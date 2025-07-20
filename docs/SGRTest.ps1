# Define ANSI escape function
function Set-SGR {
    param([string]$code)
    return "$([char]27)[$code" + "m"
}

# Reset formatting
$reset = Set-SGR "0"

# SGR styles to test
$styles = @{
    "Bold"            = "1"
    "Faint"           = "2"
    "Italic"          = "3"
    "Underline"       = "4"
    "NewSglUnderline" = "4:1"
    "NewDblUnderline" = "4:2"
    "NewUndercurl"    = "4:3"
    "Inverse"         = "7"
    "CrossedOut"      = "9"
    "DoubleUnderline" = "21"
}

# Foreground and background base colors (0–7)
$colorNames = @("Black","Red","Green","Yellow","Blue","Magenta","Cyan","White")

Write-Host "`nTesting ANSI SGR Attributes with Colors:`n"

foreach ($styleName in $styles.Keys) {
    $styleCode = $styles[$styleName]
    Write-Host "`n--- $styleName (SGR $styleCode) ---`n"

    for ($fg = 0; $fg -le 7; $fg++) {
        for ($bg = 0; $bg -le 7; $bg++) {
            $sgr = "$styleCode;3$fg;4$bg"
            $seq = Set-SGR $sgr
            $text = "{FG=$($colorNames[$fg]), BG=$($colorNames[$bg])}"
            Write-Host "$seq$text$reset " -NoNewline
        }
        Write-Host
    }
}

Write-Host "`nAll combinations displayed. Terminal should reset formatting automatically." -ForegroundColor Green
