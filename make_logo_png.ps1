$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Gera logo.png (fundo transparente, aparada) a partir de logobase.jpg na mesma pasta deste script.
$base = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$srcPath = Join-Path $base "logobase.jpg"
$dstPath = Join-Path $base "logo.png"

if (-not (Test-Path $srcPath)) { throw "Nao encontrei a imagem de origem: $srcPath  (coloque o arquivo 'logobase.jpg' nesta pasta)" }

$src = New-Object System.Drawing.Bitmap($srcPath)
$w = $src.Width; $h = $src.Height
"Origem: $w x $h"

# Copia p/ 32bpp ARGB
$argb = New-Object System.Drawing.Bitmap($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($argb); $g.DrawImage($src,0,0,$w,$h); $g.Dispose(); $src.Dispose()

$rect = New-Object System.Drawing.Rectangle(0,0,$w,$h)
$data = $argb.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::ReadWrite,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$stride = $data.Stride
$bytes = New-Object byte[] ($stride*$h)
[System.Runtime.InteropServices.Marshal]::Copy($data.Scan0,$bytes,0,$bytes.Length)

# Banda suave: lum>=240 transparente; lum<=225 opaco; linear no meio
$minX=$w; $minY=$h; $maxX=-1; $maxY=-1
for($y=0;$y -lt $h;$y++){
  $row=$y*$stride
  for($x=0;$x -lt $w;$x++){
    $i=$row+$x*4
    $b=$bytes[$i]; $gg=$bytes[$i+1]; $r=$bytes[$i+2]
    $lum = 0.299*$r + 0.587*$gg + 0.114*$b
    if($lum -ge 240){ $a=0 }
    elseif($lum -le 225){ $a=255 }
    else { $a=[int](255*(240-$lum)/15) }
    $bytes[$i+3]=[byte]$a
    if($a -gt 16){ if($x -lt $minX){$minX=$x}; if($x -gt $maxX){$maxX=$x}; if($y -lt $minY){$minY=$y}; if($y -gt $maxY){$maxY=$y} }
  }
}
[System.Runtime.InteropServices.Marshal]::Copy($bytes,0,$data.Scan0,$bytes.Length)
$argb.UnlockBits($data)

if($maxX -lt 0){ throw "Nao encontrei conteudo (a imagem parece toda clara). Verifique a logobase.jpg." }

# Recorta no bounding box do conteudo + 12px de respiro
$pad=12
$minX=[Math]::Max(0,$minX-$pad); $minY=[Math]::Max(0,$minY-$pad)
$maxX=[Math]::Min($w-1,$maxX+$pad); $maxY=[Math]::Min($h-1,$maxY+$pad)
$cw=$maxX-$minX+1; $ch=$maxY-$minY+1
"Conteudo: ${cw} x ${ch}  (bbox $minX,$minY -> $maxX,$maxY)"
$crop = New-Object System.Drawing.Bitmap($cw,$ch,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g2=[System.Drawing.Graphics]::FromImage($crop)
$g2.DrawImage($argb,(New-Object System.Drawing.Rectangle(0,0,$cw,$ch)),(New-Object System.Drawing.Rectangle($minX,$minY,$cw,$ch)),[System.Drawing.GraphicsUnit]::Pixel)
$g2.Dispose(); $argb.Dispose()
$crop.Save($dstPath,[System.Drawing.Imaging.ImageFormat]::Png)
$crop.Dispose()
"Salvo: $dstPath  ({0:N0} bytes)" -f (Get-Item $dstPath).Length
