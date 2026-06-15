@echo off
rem ============================================================
rem  Gera o logo.png (fundo transparente, aparado) para o laudo,
rem  a partir do arquivo "logobase.jpg" nesta mesma pasta.
rem  Basta dar duplo-clique. O resultado e "logo.png".
rem ============================================================
echo Gerando logo.png a partir de logobase.jpg ...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0make_logo_png.ps1"
echo.
if errorlevel 1 (
  echo *** Ocorreu um erro. Verifique se "logobase.jpg" existe nesta pasta. ***
) else (
  echo Concluido. O arquivo "logo.png" foi atualizado.
)
echo.
pause
