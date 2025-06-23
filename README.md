## [Documentation du format DGenesis](DGenesis/readme.md)

## [Documentation du format DGraph](DGRAPH.md)

## [Documentation du format DPolyGraph](DPolyGraph.md)

## [Documentation du format DShape](DSHAPE.md)

## [Documentation du format DHEMap](DHEMAP.md)

## Source combiner (to combine source code into single files)

```
powershell -File combiner_sources.ps1
```

### To combine C# files (using the default):

```
powershell -File .\combiner_sources.ps1 -Path "C:\Projects\MySolution"
```

### To combine Python files:

```
powershell -File .\combiner_sources.ps1 -Path "C:\Projects\MyPythonApp" -FileFormat "*.py"
```

### To combine text files:

```
powershell -File .\combiner_sources.ps1 -Path "C:\MyDocuments" -FileFormat "*.txt"
```
