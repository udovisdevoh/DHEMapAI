## [Documentation du format DGraph](DGenesis/readme.md)

## Documentation du format DGraph
[DGRAPH.md](DGRAPH.md)

## Documentation du format DHEMap
[DHEMAP.md](DHEMAP.md)

## Source combiner (to combine source code into single files)

combiner_source.ps1

### To combine C# files (using the default):

.\combiner_source.ps1 -Path "C:\Projects\MySolution"

### To combine Python files:

.\combiner_source.ps1 -Path "C:\Projects\MyPythonApp" -FileFormat "*.py"

### To combine text files:

.\combiner_source.ps1 -Path "C:\MyDocuments" -FileFormat "*.txt"
