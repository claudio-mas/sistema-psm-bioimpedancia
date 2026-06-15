@echo off
rem ============================================================
rem  Recompila (Release) e republica o suplemento no Google Drive.
rem  Use sempre que alterar o codigo do painel/suplemento.
rem  Depois, reinstale nos PCs (duplo clique no SistemaPSM.AddIn.vsto).
rem ============================================================
setlocal
set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
set "PROJ=%~dp0SistemaPSM.AddIn.csproj"
set "STAGE=%~dp0_Publish\"
set "GDIR=G:\Meu Drive\Sistema PSM\Suplemento\"

echo Feche o Excel antes de continuar.
pause

if exist "%STAGE%" rmdir /s /q "%STAGE%"

rem Versao crescente baseada em data/hora (AutoIncrement nao funciona via linha de comando).
rem Formato: 1.0.<(AA*1000)+dia-do-ano>.<HHMM>  -> sempre maior que a anterior.
for /f %%v in ('powershell -NoProfile -Command "$d=Get-Date; '1.0.{0}.{1}' -f (([int]$d.ToString('yy'))*1000 + $d.DayOfYear), $d.ToString('HHmm')"') do set "APPVER=%%v"
echo Publicando versao %APPVER% ...

"%MSBUILD%" "%PROJ%" /t:Publish /p:Configuration=Release /p:Platform=AnyCPU ^
  /p:VisualStudioVersion=17.0 /p:PublishDir="%STAGE%" /p:PublishUrl="%GDIR%" ^
  /p:ApplicationVersion=%APPVER% /p:UpdateEnabled=false /p:Install=true ^
  /p:BootstrapperEnabled=false /v:minimal /nologo
if errorlevel 1 ( echo. & echo *** ERRO na publicacao *** & pause & exit /b 1 )

echo.
echo Copiando para o Google Drive: %GDIR%
xcopy "%STAGE%*" "%GDIR%" /e /y /i >nul

echo.
echo Concluido. Nos PCs, reinstale com duplo clique em:
echo   %GDIR%SistemaPSM.AddIn.vsto
pause
