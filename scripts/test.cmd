set pytest=.\.venv\Scripts\python -m pytest
for /f "delims=" %%a in ('.\.venv\Scripts\python -m find_libpython') do set PYTHONNET_PYDLL=%%a

dotnet test --runtime any-x64 --logger "console;verbosity=detailed" src/embed_tests/
%pytest% --runtime coreclr
%pytest% --runtime netfx
