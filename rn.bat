@echo off
if "%~1"=="" (
    echo Need first parameter: module name
    goto exit
)
if "%~2"=="" (
    echo Need second parameter: ModuleID
    goto exit
)
ren "Assets\ModuleNameModule.cs" "%~2Module.cs"
ren "Assets\ModuleNameModule.prefab" "%~2Module.prefab"
ren "Manual\Module Name.html" "%~1.html"
ren "Manual\img\Component\Module Name.svg" "%~1.svg"
ren "Assets\ModuleName.unity" "%~2.unity"

:exit
