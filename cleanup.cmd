@echo off
echo Searching for and deleting 'bin' and 'obj' directories...
echo Current Directory: %cd%
echo ---------------------------------------------------

:: Loop through all subdirectories looking for 'bin'
for /d /r %%i in (bin) do (
    if exist "%%i" (
        echo Deleting: %%i
        rmdir /s /q "%%i"
    )
)

:: Loop through all subdirectories looking for 'obj'
for /d /r %%i in (obj) do (
    if exist "%%i" (
        echo Deleting: %%i
        rmdir /s /q "%%i"
    )
)

echo ---------------------------------------------------
echo Cleanup complete!
pause
