set pytest=.\.venv\Scripts\python -m pytest

dotnet test --runtime any-x64 --logger "console;verbosity=detailed" src/embed_tests/
%pytest% --runtime coreclr
%pytest% --runtime netfx
