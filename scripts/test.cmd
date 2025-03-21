@echo off

set index_url=https://%ADS_USER_NAME%:%ADS_USER_PASSWORD%@art-bobcat.autodesk.com/artifactory/api/pypi/autodesk-pypi-virtual/simple

call .\.venv\Scripts\activate.bat

set PYTHONHOME=
set PYTHONNET_PYDLL=.\.venv\Scripts\python

pip --no-cache-dir install --index-url=%index_url% pytest numpy

dotnet test --runtime any-x64 --logger "console;verbosity=detailed" src/embed_tests/
pytest --runtime coreclr
pytest --runtime netfx

deactivate
