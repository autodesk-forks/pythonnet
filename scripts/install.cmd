set index_url=https://%ADS_USER_NAME%:%ADS_USER_PASSWORD%@art-bobcat.autodesk.com/artifactory/api/pypi/autodesk-pypi-virtual/simple

set python=.\python\tools\python
set pip=%python% -m pip
set vPython=.\.venv\Scripts\python
set vPip=%vPython% -m pip

set PYTHONHOME=

nuget install python -Version 3.8 -ExcludeVersion -OutputDirectory .
%pip% --no-cache-dir install --index-url=%index_url% virtualenv
%python% -m virtualenv .venv

call .\.venv\Scripts\activate.bat

pip --no-cache-dir install --index-url=%index_url% --upgrade -r requirements.txt
pip --no-cache-dir install -v .

deactivate
