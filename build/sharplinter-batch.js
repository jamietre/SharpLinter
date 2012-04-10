config={
    inputroot: "../source/sharplinterexe/bin/debug",
    outputroot: "../dist",
    verbose: true,
    "$project": "$root/..",
    "$redist": "$root/../source/sharplinter/redist",
    groups: [
    {
        input: "$project/README.md,$redist/msvcp100.dll,$redist/msvcr100.dll,sharplinter.exe,jtc.sharplinter.dll,$redist/noesis.javascript.dll,$redist/ecmascript.net.modified.dll,$redist/yahoo.yui.compressor.dll",
        output: { 
            target:"sharplinter.zip",
            action: "zip"
            }
    },
    {
        input: "sharplinter.exe,jtc.sharplinter.dll",
        outputroot: "/applications/jslint",
        output: 
        { 
            target:"/applications/jslint",
            action: "copy"
        }

    }
 
    ]
};